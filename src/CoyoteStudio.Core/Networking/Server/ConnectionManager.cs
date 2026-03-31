using System.Collections.Concurrent;

using CoyoteStudio.Core.Networking.Client;

namespace CoyoteStudio.Core.Networking.Server;

internal class ConnectionManager
{
    private readonly ConcurrentDictionary<Guid, WebSocketClient> _connections = new();

    public void Register(Guid id, Action? onDisposing)
    {
        if (!_connections.TryAdd(id, new WebSocketClient(id, onDisposing)))
            throw new ArgumentException($"ID {id} already exists");
    }

    public void TryUnregister(Guid id)
    {
        _ = _connections.TryRemove(id, out _);
    }

    public bool TryGetClient(Guid id, out WebSocketClient? client)
    {
        return _connections.TryGetValue(id, out client);
    }

    public void SetupClient(ConnectionData data)
    {
        if (!TryGetClient(data.Id, out var client))
            return;
        client?.Setup(data);

    }

    public WebSocketClientKind? GetClientKind(Guid id)
    {
        return _connections.TryGetValue(id, out var client)
            ? client.GetType().Name switch
            {
                "DeviceClient" => WebSocketClientKind.Device,
                "RemoteClient" => WebSocketClientKind.Remote,
                "WebSocketClient" => WebSocketClientKind.Unknown,
                _ => throw new InvalidOperationException("Invalid implement for WebSocketClient class")
            }
            : null;
    }

}
