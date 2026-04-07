using System.Collections.Concurrent;
using System.Diagnostics;

using CoyoteStudio.Core.Networking.Client;

namespace CoyoteStudio.Core.Networking.Server;

internal class WebSocketManager : IDisposable
{
    private readonly WebSocketServer _server = new();
    private readonly ConcurrentDictionary<Guid, WebSocketClient> _clients = new();

    public WebSocketManager()
    {
        _server.ClientDisconnected += OnClientDisconnected;
    }

    private void OnClientConnected(object? _, WebSocketServer.ClientConnectedEventArgs e)
    {
        Register(e.ClientId, e.OnDisposing);
    }

    private void OnClientDisconnected(object? _, WebSocketServer.ClientDisconnectedEventArgs e)
    {
        TryUnregister(e.ClientId);
    }

    private void OnClientMessageReceived(object? _, WebSocketServer.ClientMessageReceivedEventArgs e)
    {
        if (TryGetClient(e.ClientId, out var client))
        {
            switch (client)
            {
                case DeviceWebSocketClient deviceClient:
                {
                    deviceClient.Setup(e.Message);
                    break;
                }
                case RemoteWebSocketClient remoteClient:
                {
                    remoteClient.Setup(e.Message);
                    break;
                }
                case WebSocketClient:
                {
                    break;
                }
                default:
                    throw new UnreachableException("Invalid client type");
            }
        }
    }

    public async Task StartupAsync(int port)
    {
        await _server.RunAsync(port);
    }

    public void Dispose()
    {
        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }
        _clients.Clear();

        _server.Dispose();
    }

    private void Register(Guid id, Action? onDisposing)
    {
        if (!_clients.TryAdd(id, new WebSocketClient(id, onDisposing)))
            throw new ArgumentException($"ID {id} already exists");
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
}
