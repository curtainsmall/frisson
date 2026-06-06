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

    public Guid Id { get; init; }

    public WebSocketClient(Guid id, Action? onDisposing)
    {
        Id = id;
        OnDisposing = onDisposing;
    }

    public void Dispose()
    {
        OnDisposing?.Invoke();
    }
}
