using System.Collections.Concurrent;
using System.Text.Json;

using FleckServer = global::Fleck.WebSocketServer;
using FleckSocket = global::Fleck.IWebSocketConnection;

using Frisson.Core.Agent;
using Frisson.Core.Agent.Control;
using Frisson.Core.Agent.Device;

namespace Frisson.Core.Networking.WebSocket;

internal class WebSocketServer : IDisposable
{
    private const int MaxMessageLength = 1950;

    private FleckServer? _server;
    private readonly ConcurrentDictionary<Guid, WebSocketClient> _clients = new();
    private readonly ConcurrentDictionary<Guid, PendingBind> _pendingBinds = new();

    public event Action<Agent.Agent>? AgentCreated;
    public event Action<Guid>? ClientDisconnected;
    public event Action<Guid, string>? ControlSourceBindingRequested;

    private record PendingBind(Guid ClientId, string SourceName, WebSocketClient Client);

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
                client.Send(new Scheme.Device.BindScheme
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
                        var deviceAgent = CreateDevice(id, msg, client);
                        if (deviceAgent == null) { TryRemove(id); return; }
                        deviceAgent.SendFunc = client.Send;
                        client.MessageHandler = json => deviceAgent.HandleMessage(json);
                        client.IsBound = true;
                        AgentCreated?.Invoke(deviceAgent);
                    }
                    // Control Source bind: pend and wait for user trust confirmation
                    else if (msg.Contains("\"name\""))
                    {
                        var scheme = Scheme.Control.BindScheme.TryParse(msg);
                        if (scheme == null) { TryRemove(id); return; }
                        _pendingBinds.TryAdd(id, new PendingBind(id, scheme.Name, client));
                        ControlSourceBindingRequested?.Invoke(id, scheme.Name);
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

    private Agent.Agent? CreateDevice(Guid id, string msg, WebSocketClient client)
    {
        var scheme = Scheme.Device.BindScheme.TryParseDeviceBind(msg);
        if (scheme == null) return null;
        client.Send(new Scheme.Device.BindScheme
        {
            ClientId = AppCore.DummyFrontendId,
            TargetId = id,
            Message = "200"
        }.ToJson());
        return new DeviceAgent(id, () => TryRemove(id));
    }

    public void AcceptControlSource(Guid clientId)
    {
        if (!_pendingBinds.TryRemove(clientId, out var pending)) return;
        if (!_clients.TryGetValue(clientId, out var client)) return;

        client.Send(new Scheme.Control.BindScheme { Id = clientId, Name = pending.SourceName }.ToJson());
        var agent = new ControlSourceAgent(clientId, pending.SourceName, () => TryRemove(clientId));
        agent.SendFunc = client.Send;
        client.MessageHandler = json => agent.HandleMessage(json);
        client.IsBound = true;
        AgentCreated?.Invoke(agent);
    }

    public void RejectControlSource(Guid clientId)
    {
        _pendingBinds.TryRemove(clientId, out _);
        TryRemove(clientId);
    }

    public void TryRemove(Guid id)
    {
        if (_clients.TryRemove(id, out var client))
        {
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
