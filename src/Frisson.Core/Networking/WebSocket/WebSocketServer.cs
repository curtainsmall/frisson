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

    public event Action<Agent.Agent>? AgentCreated;
    public event Action<Guid>? ClientDisconnected;

    public void Start(int port)
    {
        _server = new FleckServer($"ws://0.0.0.0:{port}");
        _server.Start(socket =>
        {
            var wsId = Guid.NewGuid();

            socket.OnOpen = () =>
            {
                var client = new WebSocketClient(wsId, socket);
                _clients.TryAdd(wsId, client);
                LoggerService.Instance.Log($"[Server] Client connected: {wsId}");
                client.Send(new Scheme.Device.BindScheme
                {
                    ClientId = wsId,
                    TargetId = Guid.Empty,
                    Message = "targetId"
                }.ToJson());
            };

            socket.OnClose = () => TryRemove(wsId);

            socket.OnMessage = msg =>
            {
                LoggerService.Instance.Log($"[Server] Received from {wsId}: {msg}");
                if (!_clients.TryGetValue(wsId, out var client)) return;

                if (!client.IsBound)
                {
                    Agent.Agent? agent = msg switch
                    {
                        _ when msg.Contains("\"clientId\"") => CreateDevice(wsId, msg, client),
                        _ when msg.Contains("\"id\"") && msg.Contains("\"name\"") => CreateControlSource(wsId, msg, client),
                        _ => null
                    };

                    if (agent == null) { TryRemove(wsId); return; }
                    agent.SendFunc = client.Send;
                    client.MessageHandler = json => agent.HandleMessage(json);
                    client.IsBound = true;
                    AgentCreated?.Invoke(agent);
                }
                // Subsequent messages handled by client.MessageHandler via client.OnMessage
            };
        });

        LoggerService.Instance.Log($"[Server] Started on port {port}");
    }

    private Agent.Agent? CreateDevice(Guid wsId, string msg, WebSocketClient client)
    {
        var scheme = Scheme.Device.BindScheme.TryParseDeviceBind(msg);
        if (scheme == null) return null;
        client.Send(new Scheme.Device.BindScheme
        {
            ClientId = wsId,
            TargetId = wsId,
            Message = "200"
        }.ToJson());
        return new DeviceAgent(wsId, () => TryRemove(wsId));
    }

    private Agent.Agent? CreateControlSource(Guid wsId, string msg, WebSocketClient client)
    {
        var scheme = Scheme.Control.BindScheme.TryParse(msg);
        if (scheme == null) return null;
        client.Send(new Scheme.Control.BindScheme { Id = wsId, Name = scheme.Name }.ToJson());
        return new ControlSourceAgent(wsId, scheme.Name, () => TryRemove(wsId));
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
