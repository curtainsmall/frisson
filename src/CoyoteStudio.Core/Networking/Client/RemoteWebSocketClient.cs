using System.Text.Json;

using CoyoteStudio.Core.Networking.Client.Scheme;

namespace CoyoteStudio.Core.Networking.Client;

internal class RemoteWebSocketClient : WebSocketClient
{
    public RemoteWebSocketClient(Guid id, Action? onDisposing) : base(id, onDisposing)
    {
    }

    public override void Setup(string jsonString)
    {
        var jsonDoc = JsonDocument.Parse(jsonString);
        if (jsonDoc is null)
            return;

        var scheme = new RemoteProtocolScheme(jsonDoc);

    }
}
