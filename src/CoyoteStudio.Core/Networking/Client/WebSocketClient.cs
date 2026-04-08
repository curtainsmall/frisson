using System.Text.Json;

using CoyoteStudio.Core.Networking.Client.Scheme;

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

    /// <summary>
    /// Event raised when a valid bind message is received with matching GUID.
    /// </summary>
    public event EventHandler? BindRequested;

    public WebSocketClient(Guid id, Action? onDisposing)
    {
        Id = id;
        OnDisposing = onDisposing;
    }

    public virtual void Setup(string jsonString)
    {
        using var jsonDoc = JsonDocument.Parse(jsonString);
        Setup(jsonDoc);
    }

    public virtual void Setup(JsonDocument jsonDoc)
    {
        var scheme = new ClientProtocolScheme(jsonDoc);

        // Check if this is a valid bind message with matching GUID
        if (scheme.BindId == Id)
        {
            OnBindRequested();
        }
    }

    protected virtual void OnBindRequested()
    {
        BindRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        OnDisposing?.Invoke();
    }
}
