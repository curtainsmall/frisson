using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

using CoyoteStudio.Core.Networking.Client;
using CoyoteStudio.Core.Networking.Client.Scheme;

namespace CoyoteStudio.Core.Networking.Server;

internal class WebSocketManager : IDisposable
{
    private readonly WebSocketServer _server = new();
    private readonly ConcurrentDictionary<Guid, WebSocketClient> _clients = new();

    public WebSocketManager()
    {
        AddEventHandlers();
    }

    private void OnClientConnected(object? _, WebSocketServer.ClientConnectedEventArgs e)
    {
        var client = Register(e.ClientId, e.OnDisposing);
        client.BindRequested += OnClientBindRequested;
    }

    private void OnClientBindRequested(object? sender, EventArgs e)
    {
        if (sender is WebSocketClient client && _clients.ContainsKey(client.Id))
        {
            UpliftToRemoteClient(client.Id, client);
        }
    }

    private void OnClientDisconnected(object? _, WebSocketServer.ClientDisconnectedEventArgs e)
    {
        TryUnregister(e.ClientId);
    }

    private void OnClientMessageReceived(object? _, WebSocketServer.ClientMessageReceivedEventArgs e)
    {
        if (TryGetClient(e.ClientId, out var client) && client is not null)
        {
            switch (client)
            {
                case DeviceWebSocketClient deviceClient:
                    deviceClient.Setup(e.Message);
                    break;
                case RemoteWebSocketClient remoteClient:
                    remoteClient.Setup(e.Message);
                    break;
                case WebSocketClient baseClient:
                    baseClient.Setup(e.Message);
                    break;
            }
        }
    }

    private void UpliftToRemoteClient(Guid clientId, WebSocketClient existingClient)
    {
        // Unsubscribe from old client's events
        existingClient.BindRequested -= OnClientBindRequested;

        // Create new RemoteWebSocketClient preserving the original OnDisposing action
        var remoteClient = new RemoteWebSocketClient(clientId, existingClient.OnDisposing);

        // Replace the client in the dictionary
        _clients[clientId] = remoteClient;
    }

    public async Task RunAsync(int port)
    {
        await _server.RunAsync(port);
    }

    /// <summary>
    /// Sends a message to a specific client.
    /// </summary>
    /// <param name="clientId">The target client ID.</param>
    /// <param name="message">The message to send.</param>
    /// <returns>True if the message was sent successfully.</returns>
    public async Task<bool> SendAsync(Guid clientId, string message)
    {
        return await _server.SendAsync(clientId, message);
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
            client.Dispose();
        }
    }

    private bool TryGetClient(Guid id, out WebSocketClient? client)
    {
        return _clients.TryGetValue(id, out client);
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
