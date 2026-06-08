namespace Frisson.Core.Networking.Client;

/// <summary>
/// Abstraction of an external Device (DG-LAB APP).
/// Type itself indicates Device-bound state (base WebSocketClient = unbound).
/// Only sends/receives DG-LAB protocol messages.
/// </summary>
internal sealed class DeviceWebSocketClient : WebSocketClient
{
    /// <summary>
    /// Copy constructor — upgrades an unbound WebSocketClient to a Device-bound client.
    /// </summary>
    public DeviceWebSocketClient(WebSocketClient existing) : base(existing.OnDisposing)
    {
        SendFunc = existing.SendFunc;
    }
}
