using System.Text.Json;

using CoyoteStudio.Core.Networking.Client.Scheme;

namespace CoyoteStudio.Core.Networking.Client;

internal enum WebSocketClientKind
{
    Unknown = 0,
    Remote,
    Device
}

internal class BindRequestedEventArgs : EventArgs
{
    public JsonDocument JsonDocument { get; init; }

    public BindRequestedEventArgs(JsonDocument jsonDocument)
    {
        JsonDocument = jsonDocument;
    }
}

internal class WebSocketClient : IDisposable
{
    public Action? OnDisposing { get; init; }

    public Guid Id { get; init; }

    /// <summary>
    /// Event raised when a valid bind message is received with matching GUID.
    /// </summary>
    public event EventHandler<BindRequestedEventArgs>? BindRequested;

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
            OnBindRequested(jsonDoc);
        }
    }

    protected virtual void OnBindRequested(JsonDocument jsonDoc)
    {
        BindRequested?.Invoke(this, new BindRequestedEventArgs(jsonDoc));
    }

    public void Dispose()
    {
        OnDisposing?.Invoke();
    }
}
