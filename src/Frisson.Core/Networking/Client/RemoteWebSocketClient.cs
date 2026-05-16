using System.Text.Json;

using Frisson.Core.Networking.Client.Scheme;

namespace Frisson.Core.Networking.Client;

internal sealed class RemoteMessageReceivedEventArgs : EventArgs
{
    public RemoteProtocolScheme Scheme { get; }

    public RemoteMessageReceivedEventArgs(RemoteProtocolScheme scheme)
    {
        Scheme = scheme;
    }
}

internal class RemoteWebSocketClient : WebSocketClient
{
    public event EventHandler<RemoteMessageReceivedEventArgs>? RemoteMessageReceived;

    public RemoteWebSocketClient(Guid id, Action? onDisposing) : base(id, onDisposing)
    {
    }

    public override void Setup(string jsonString)
    {
        using var jsonDoc = JsonDocument.Parse(jsonString);
        Setup(jsonDoc);
    }

    public override void Setup(JsonDocument jsonDoc)
    {
        var scheme = new RemoteProtocolScheme(jsonDoc);
        RemoteMessageReceived?.Invoke(this, new RemoteMessageReceivedEventArgs(scheme));
    }
}
