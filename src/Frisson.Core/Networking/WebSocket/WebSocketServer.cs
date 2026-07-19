using System.Collections.Concurrent;
using System.Text.Json;

using FleckServer = global::Fleck.WebSocketServer;
using FleckSocket = global::Fleck.IWebSocketConnection;

using Frisson.Core.Agent;
using Frisson.Core.Agent.Remote;
using Frisson.Core.Agent.Actuator;

namespace Frisson.Core.Networking.WebSocket;

internal class WebSocketServer : IDisposable
{
    private const int MaxMessageLength = 1950;

    private FleckServer? _server;
    private readonly ConcurrentDictionary<Guid, WebSocketClient> _clients = new();
    private readonly ConcurrentDictionary<Guid, PendingBind> _pendingBinds = new();
    private Guid? _actuatorClientId;

    public event Action<Agent.Agent>? AgentCreated;
    public event Action<Guid>? ClientDisconnected;
    public event Action<Guid, string>? RemoteBindingRequested;

    private record PendingBind(Guid ClientId, string SourceName, bool AlwaysReply, List<Scheme.Remote.UiItem>? Ui, WebSocketClient Client);

    public void Start(int port)
    {
        _server = new FleckServer($"ws://0.0.0.0:{port}");
        _server.Start(socket =>
        {
            var id = Guid.NewGuid();

            socket.OnOpen = () =>
            {
                var client = new WebSocketClient(id, socket);
                _clients.TryAdd(id, client);
                LoggerService.Instance.Log($"[Server] Client connected: {id}");
                client.Send(new Scheme.Actuator.BindScheme
                {
                    ClientId = id,
                    TargetId = Guid.Empty,
                    Message = "targetId"
                }.ToJson());
            };

            socket.OnClose = () => TryRemove(id);

            socket.OnMessage = msg =>
            {
                LoggerService.Instance.Log($"[Server] Received from {id}: {msg}");
                if (!_clients.TryGetValue(id, out var client)) return;

                if (!client.IsBound)
                {
                    // Device bind: auto-accept and reply immediately
                    if (msg.Contains("\"clientId\""))
                    {
                        var actuatorAgent = CreateActuator(id, msg, client);
                        if (actuatorAgent == null) { TryRemove(id); return; }
                        actuatorAgent.SendFunc = client.Send;
                        client.MessageHandler = json => actuatorAgent.HandleMessage(json);
                        client.IsBound = true;
                        AgentCreated?.Invoke(actuatorAgent);
                    }
                    // Remote bind: pend and wait for user trust confirmation
                    else if (msg.Contains("\"name\""))
                    {
                        var scheme = Scheme.Remote.BindScheme.TryParse(msg);
                        if (scheme == null) { TryRemove(id); return; }

                        // Validate UI declaration if present
                        if (scheme.Ui != null)
                        {
                            var uiError = Scheme.Remote.UiItem.Validate(scheme.Ui);
                            if (uiError != null)
                            {
                                client.Send(JsonSerializer.Serialize(new
                                {
                                    type = "error",
                                    message = "InvalidUI"
                                }));
                                TryRemove(id);
                                return;
                            }
                        }

                        _pendingBinds.TryAdd(id, new PendingBind(id, scheme.Name, scheme.AlwaysReply, scheme.Ui, client));
                        RemoteBindingRequested?.Invoke(id, scheme.Name);
                    }
                    else
                    {
                        TryRemove(id);
                    }
                }
                else
                {
                    client.OnMessage(msg);
                }
            };
        });

        LoggerService.Instance.Log($"[Server] Started on port {port}");
    }

    private Agent.Agent? CreateActuator(Guid id, string msg, WebSocketClient client)
    {
        var scheme = Scheme.Actuator.BindScheme.TryParseDeviceBind(msg);
        if (scheme == null) return null;

        // DG-LAB allows only one device at a time — disconnect existing actuator if any
        if (_actuatorClientId.HasValue)
            TryRemove(_actuatorClientId.Value);
        _actuatorClientId = id;

        client.Send(new Scheme.Actuator.BindScheme
        {
            ClientId = AppCore.DummyFrontendId,
            TargetId = id,
            Message = "200"
        }.ToJson());
        return new ActuatorAgent(id, () => TryRemove(id));
    }

    public void AcceptRemote(Guid clientId)
    {
        if (!_pendingBinds.TryRemove(clientId, out var pending)) return;
        if (!_clients.TryGetValue(clientId, out var client)) return;

        client.Send(new Scheme.Remote.BindScheme { Id = clientId, Name = pending.SourceName, AlwaysReply = pending.AlwaysReply }.ToJson());
        var agent = new RemoteAgent(clientId, pending.SourceName, () => TryRemove(clientId))
        {
            Ui = pending.Ui
        };
        agent.AlwaysReply = pending.AlwaysReply;
        agent.SendFunc = client.Send;
        client.MessageHandler = json => agent.HandleMessage(json);
        client.IsBound = true;
        AgentCreated?.Invoke(agent);
    }

    public void RejectRemote(Guid clientId)
    {
        if (!_pendingBinds.TryRemove(clientId, out var pending)) return;

        pending.Client.Send(JsonSerializer.Serialize(new
        {
            type = "error",
            message = "Connection rejected by user."
        }));

        TryRemove(clientId);
    }

    public void TryRemove(Guid id)
    {
        if (_clients.TryRemove(id, out var client))
        {
            if (_actuatorClientId == id) _actuatorClientId = null;
            client.Close();
            ClientDisconnected?.Invoke(id);
        }
    }

    public void Dispose()
    {
        foreach (var id in _clients.Keys.ToList())
            TryRemove(id);
        _server?.Dispose();
    }
}
