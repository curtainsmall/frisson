namespace Frisson.Core.Networking.Client;

internal sealed class RemoteWebSocketClient : WebSocketClient
{
    public RemoteWebSocketClient(Guid id, Action? onDisposing) : base(id, onDisposing)
    {
    }
}
