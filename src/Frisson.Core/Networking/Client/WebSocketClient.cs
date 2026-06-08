using Frisson.Core.Scheme;

namespace Frisson.Core.Networking.Client;

public enum WebSocketClientKind
{
    Unknown = 0,
    Remote,
    Device
}

/// <summary>
/// Connection status of a client for UI display.
/// </summary>
public enum WebClientConnectionStatus
{
    Connected,
    Disconnected,
    Pending
}

internal class WebSocketClient : IDisposable
{
    public Action? OnDisposing { get; init; }

    /// <summary>
    /// Callback for sending a message over the WebSocket connection.
    /// Set by WebSocketManager during registration/upgrade.
    /// </summary>
    public Func<string, Task<bool>>? SendFunc { get; set; }

    public WebSocketClient(Action? onDisposing)
    {
        OnDisposing = onDisposing;
    }

    /// <summary>
    /// Sends a JSON string over the WebSocket connection.
    /// </summary>
    public async Task<bool> SendAsync(string json)
    {
        if (SendFunc == null)
            return false;
        return await SendFunc(json);
    }

    /// <summary>
    /// Parses an incoming JSON message into the appropriate Scheme subclass.
    /// </summary>
    public Scheme.Scheme? Receive(string json)
    {
        return Scheme.Scheme.Parse(json);
    }

    public void Dispose()
    {
        OnDisposing?.Invoke();
    }
}
