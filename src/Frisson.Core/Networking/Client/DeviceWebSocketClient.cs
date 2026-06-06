namespace Frisson.Core.Networking.Client;

internal sealed class DeviceWebSocketClient : WebSocketClient
{
    public DeviceWebSocketClient(Guid id, Action? onDisposing) : base(id, onDisposing)
    {
    }
}
