using System.Collections.Concurrent;

using Frisson.Core.Networking.Client;

namespace Frisson.Core.Networking.Server;

public sealed class ClientConnectionEventArgs : EventArgs
{
    public Guid ClientId { get; }
    public WebSocketClientKind Kind { get; }
    public WebClientConnectionStatus Status { get; }

    public ClientConnectionEventArgs(Guid clientId, WebSocketClientKind kind, WebClientConnectionStatus status)
    {
        ClientId = clientId;
        Kind = kind;
        Status = status;
    }
}

internal class WebSocketManager : IDisposable
{
    private readonly WebSocketServer _server = new();
    private readonly ConcurrentDictionary<Guid, WebSocketClient> _clients = new();

    public WebSocketManager()
    {
        AddEventHandlers();
    }

    /// <summary>
    /// Raised when a client has connected.
    /// </summary>
    public event EventHandler<ClientConnectionEventArgs>? ClientConnected;

    /// <summary>
    /// Raised when a client has disconnected.
    /// </summary>
    public event EventHandler<ClientConnectionEventArgs>? ClientDisconnected;

    /// <summary>
    /// Gets a client by its ID.
    /// </summary>
    public WebSocketClient? GetClient(Guid clientId)
    {
        _clients.TryGetValue(clientId, out var client);
        return client;
    }

    /// <summary>
    /// Disconnects a specific client by ID.
    /// </summary>
    public void DisconnectClient(Guid clientId)
    {
        LoggerService.Instance.Log($"[Manager] Force disconnecting client {clientId}");
        TryUnregister(clientId);
    }

    /// <summary>
    /// Sends a message to a specific client.
    /// </summary>
    public async Task<bool> SendAsync(Guid clientId, string message)
    {
        return await _server.SendAsync(clientId, message);
    }

    public async Task RunAsync(int port)
    {
        await _server.RunAsync(port);
    }

    public void Dispose()
    {
        RemoveEventHandlers();

        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }
        _clients.Clear();

        _server.Dispose();
    }

    private void OnClientConnected(object? _, WebSocketServer.ClientConnectedEventArgs e)
    {
        Register(e.ClientId, e.OnDisposing);
        LoggerService.Instance.Log($"[Manager] Client connected: {e.ClientId}");
    }

    private void OnClientDisconnected(object? _, WebSocketServer.ClientDisconnectedEventArgs e)
    {
        TryUnregister(e.ClientId);
    }

    private void OnClientMessageReceived(object? _, WebSocketServer.ClientMessageReceivedEventArgs e)
    {
        LoggerService.Instance.Log($"[Manager] Message from {e.ClientId}: {e.Message}");
    }

    private WebSocketClient Register(Guid id, Action? onDisposing)
    {
        var client = new WebSocketClient(id, onDisposing);
        if (!_clients.TryAdd(id, client))
            throw new ArgumentException($"ID {id} already exists");
        return client;
    }

    private void TryUnregister(Guid id)
    {
        if (_clients.TryRemove(id, out var client))
        {
            ClientDisconnected?.Invoke(this, new ClientConnectionEventArgs(id, WebSocketClientKind.Unknown, WebClientConnectionStatus.Disconnected));
            client.Dispose();
        }
    }

    private void AddEventHandlers()
    {
        _server.ClientConnected += OnClientConnected;
        _server.ClientDisconnected += OnClientDisconnected;
        _server.ClientMessageReceived += OnClientMessageReceived;
    }

    private void RemoveEventHandlers()
    {
        _server.ClientConnected -= OnClientConnected;
        _server.ClientDisconnected -= OnClientDisconnected;
        _server.ClientMessageReceived -= OnClientMessageReceived;
    }
}
