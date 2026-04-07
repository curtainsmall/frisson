namespace CoyoteStudio.Core.Networking.Client;

internal enum WebSocketClientKind
{
    Unknown = 0,
    Remote,
    Device
}

internal class WebSocketClient : IDisposable
{
    internal record ChannelState
    {
        public int Strength { get; private set; }
    }

    public Action? OnDisposing { get; init; }

    public Guid Id { get; init; }
    public ClientData? Data { get; private set; }

    public WebSocketClient(Guid id, Action? onDisposing)
    {
        Id = id;
        OnDisposing = onDisposing;
    }

    public virtual void Setup(string jsonString)
    {
    }

    public void Dispose()
    {
        OnDisposing?.Invoke();
    }
}
