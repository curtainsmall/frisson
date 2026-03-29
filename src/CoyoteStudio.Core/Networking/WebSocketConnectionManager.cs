using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

using CoyoteStudio.Core.Network;

namespace CoyoteStudio.Core.Networking;

internal class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<Guid, WebSocketClient> _connections = new();

    public void Register(Guid id)
    {
        _connections.TryAdd(id, new WebSocketClient(id));
    }

    public void Unregister(Guid id)
    {
        _connections.TryRemove(id, out _);
    }

    public bool TryGetClient(Guid id, out WebSocketClient? client)
    {
        return _connections.TryGetValue(id, out client);
    }

}
