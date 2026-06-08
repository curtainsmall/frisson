namespace Frisson.Core.Networking.Client;

/// <summary>
/// Abstraction of an external Remote program.
/// Type itself indicates Remote-bound state (base WebSocketClient = unbound).
/// Only sends/receives Remote protocol messages.
/// </summary>
internal sealed class RemoteWebSocketClient : WebSocketClient
{
    /// <summary>
    /// Copy constructor — upgrades an unbound WebSocketClient to a Remote-bound client.
    /// </summary>
    public RemoteWebSocketClient(WebSocketClient existing) : base(existing.OnDisposing)
    {
        SendFunc = existing.SendFunc;
    }
}
