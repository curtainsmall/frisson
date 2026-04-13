using System.Collections.Concurrent;
using System.Text.Json;

using CoyoteStudio.Core.Networking.Client;

namespace CoyoteStudio.Core.Networking.Server;

internal class WebSocketManager : IDisposable
{
    /// <summary>
    /// A placeholder UUID used as a dummy client ID in messages.
    /// </summary>
    public static readonly Guid DummyClientId = Guid.NewGuid();

    private readonly WebSocketServer _server = new();
    private readonly ConcurrentDictionary<Guid, WebSocketClient> _clients = new();

    public WebSocketManager()
    {
        AddEventHandlers();
    }

    /// <summary>
    /// Gets a client by its ID.
    /// </summary>
    /// <param name="clientId">The client GUID</param>
    /// <returns>The client instance, or null if not found</returns>
    public WebSocketClient? GetClient(Guid clientId)
    {
        _clients.TryGetValue(clientId, out var client);
        return client;
    }

    private async void OnClientConnected(object? _, WebSocketServer.ClientConnectedEventArgs e)
    {
        var client = Register(e.ClientId, e.OnDisposing);
        client.BindRequested += OnClientBindRequested;

        // Send bind message immediately upon connection
        var bindMessage = CreateBindMessage(client.Id);
        await SendAsync(client.Id, bindMessage);
    }

    private void OnClientBindRequested(object? sender, BindRequestedEventArgs e)
    {
        if (sender is WebSocketClient client && _clients.ContainsKey(client.Id))
        {
            // Determine client type based on message content
            // If the reply matches the bind message format (except message field), it's a Device
            if (IsDeviceBindReply(e.JsonDocument, client.Id))
            {
                UpliftToDeviceClient(client.Id, client);
            }
            else
            {
                UpliftToRemoteClient(client.Id, client);
            }
        }
    }
    
    private bool IsDeviceBindReply(JsonDocument jsonDoc, Guid expectedClientId)
    {
        var root = jsonDoc.RootElement;
        
        // Check if it has the expected fields for a device bind reply
        if (!root.TryGetProperty("type", out var typeElement) || typeElement.GetString() != "bind")
            return false;
        
        if (!root.TryGetProperty("clientId", out var clientIdElement))
            return false;
        
        // clientId should be the dummy client ID
        if (!clientIdElement.TryGetGuid(out var clientId) || clientId != DummyClientId)
            return false;
        
        if (!root.TryGetProperty("targetId", out var targetIdElement))
            return false;
        
        // targetId should match the client ID we sent the bind message to
        if (!targetIdElement.TryGetGuid(out var targetId) || targetId != expectedClientId)
            return false;
        
        if (!root.TryGetProperty("message", out _))
            return false;
        
        // message field can be anything, just need to exist
        return true;
    }

    private static string CreateBindMessage(Guid clientId)
    {
        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
            
        writer.WriteStartObject();
        writer.WriteString("type", "bind");
        writer.WriteString("clientId", DummyClientId.ToString());
        writer.WriteString("targetId", clientId.ToString());
        writer.WriteString("message", "targetId");
        writer.WriteEndObject();
        writer.Flush();
            
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
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
    
    private void UpliftToDeviceClient(Guid clientId, WebSocketClient existingClient)
    {
        // Unsubscribe from old client's events
        existingClient.BindRequested -= OnClientBindRequested;

        // Create new DeviceWebSocketClient preserving the original OnDisposing action
        var deviceClient = new DeviceWebSocketClient(clientId, existingClient.OnDisposing);

        // Replace the client in the dictionary
        _clients[clientId] = deviceClient;
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
