using System.Collections.Concurrent;

using FleckServer = global::Fleck.WebSocketServer;
using FleckSocket = global::Fleck.IWebSocketConnection;

namespace Frisson.Core.Networking.WebSocket;

internal record AgentCreationArgs(
    Guid Id,
    Func<string, Task<bool>> SendFunc,
    Action CloseAction,
    Action<Action<string>> SetMessageHandler);

internal class WebSocketServer : IDisposable
{
    private const int _maxMessageLength = 1950;

    private FleckServer? _server;
    private readonly ConcurrentDictionary<Guid, WebSocketClient> _clients = new();

    public event Action<AgentCreationArgs>? ClientConnected;
    public event Action<Guid>? ClientDisconnected;

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
                ClientConnected?.Invoke(new AgentCreationArgs(
                    id, client.Send, client.Close,
                    h => client.MessageHandler = h));
            };

            socket.OnClose = () =>
            {
                ClientDisconnected?.Invoke(id);
                _clients.TryRemove(id, out _);
                LoggerService.Instance.Log($"[Server] Client disconnected: {id}");
            };

            socket.OnMessage = message =>
            {
                LoggerService.Instance.Log($"[Server] Received from {id}: {message}");
                if (_clients.TryGetValue(id, out var client))
                    client.OnMessage(message);
            };
        });

        LoggerService.Instance.Log($"[Server] Started on port {port}");
    }

    public void Close(Guid id)
    {
        if (_clients.TryGetValue(id, out var client))
            client.Close();
    }

    public void Dispose()
    {
        foreach (var client in _clients.Values)
            client.Close();
        _clients.Clear();
        _server?.Dispose();
    }
}
