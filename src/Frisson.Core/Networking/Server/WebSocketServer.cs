using System.Collections.Concurrent;

using FleckServer = global::Fleck.WebSocketServer;
using FleckSocket = global::Fleck.IWebSocketConnection;

namespace Frisson.Core.Networking.Server;

internal class WebSocketServer : IDisposable
{
    internal class ClientConnectedEventArgs : EventArgs
    {
        public Guid ClientId { get; init; }
        public Action? OnDisposing { get; init; }

        public ClientConnectedEventArgs(Guid clientId, Action? onDisposing)
        {
            ClientId = clientId;
            OnDisposing = onDisposing;
        }
    }

    internal class ClientDisconnectedEventArgs(Guid clientId) : EventArgs
    {
        public Guid ClientId { get; init; } = clientId;
    }

    internal class ClientMessageReceivedEventArgs(Guid clientId, string message) : EventArgs
    {
        public Guid ClientId { get; init; } = clientId;
        public string Message { get; init; } = message;
    }

    private const int _maxMessageLength = 1950;

    private FleckServer? _fleckServer;
    private readonly CancellationTokenSource _serverCts = new();
    private readonly ConcurrentDictionary<Guid, FleckSocket> _clients = new();

    public event EventHandler<ClientConnectedEventArgs>? ClientConnected;
    public event EventHandler<ClientDisconnectedEventArgs>? ClientDisconnected;
    public event EventHandler<ClientMessageReceivedEventArgs>? ClientMessageReceived;

    public WebSocketServer()
    {
    }

    public async Task RunAsync(int port)
    {
        _fleckServer = new FleckServer($"ws://0.0.0.0:{port}");

        _fleckServer.Start(socket =>
        {
            var id = Guid.NewGuid();

            socket.OnOpen = () =>
            {
                _clients.TryAdd(id, socket);
                LoggerService.Instance.Log($"[Server] Client connected: {id}");
                ClientConnected?.Invoke(this, new ClientConnectedEventArgs(id, () => socket.Close()));
            };

            socket.OnClose = () =>
            {
                _clients.TryRemove(id, out _);
                LoggerService.Instance.Log($"[Server] Client disconnected: {id}");
                ClientDisconnected?.Invoke(this, new ClientDisconnectedEventArgs(id));
            };

            socket.OnMessage = message =>
            {
                LoggerService.Instance.Log($"[Server] Received from {id}: {message}");
                ClientMessageReceived?.Invoke(this, new ClientMessageReceivedEventArgs(id, message));
            };
        });

        LoggerService.Instance.Log($"[Server] Started on port {port}");

        try
        {
            await Task.Delay(Timeout.Infinite, _serverCts.Token);
        }
        catch (OperationCanceledException)
        {
            LoggerService.Instance.Log("[Server] Stopped");
        }
    }

    /// <summary>
    /// Sends a message to a specific client.
    /// </summary>
    /// <param name="clientId">The target client ID.</param>
    /// <param name="message">The message to send.</param>
    /// <returns>True if the message was sent successfully.</returns>
    public Task<bool> SendAsync(Guid clientId, string message)
    {
        if (message.Length > _maxMessageLength)
        {
            LoggerService.Instance.Log($"[Server] Message too long ({message.Length} chars), max {_maxMessageLength}");
            return Task.FromResult(false);
        }

        if (!_clients.TryGetValue(clientId, out var socket))
            return Task.FromResult(false);

        if (!socket.IsAvailable)
            return Task.FromResult(false);

        try
        {
            socket.Send(message);
            LoggerService.Instance.Log($"[Server] Sent to {clientId}: {message}");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            LoggerService.Instance.Log($"[Server] Failed to send to {clientId}: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public void Dispose()
    {
        _serverCts.Cancel();

        foreach (var socket in _clients.Values)
        {
            socket.Close();
        }
        _clients.Clear();

        _serverCts.Dispose();
        _fleckServer?.Dispose();
    }
}