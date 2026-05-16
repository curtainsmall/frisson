using System.Text.Json;

namespace Frisson.Core.Networking.Client.Scheme;

internal class ClientProtocolScheme : ProtocolScheme
{
    public Guid? BindId { get; init; }

    public ClientProtocolScheme(JsonDocument jsonDoc)
    {
        var rootElement = jsonDoc.RootElement;
        BindId = ParseBindId(rootElement);
    }

    private static Guid? ParseBindId(JsonElement rootElement)
    {
        if (!rootElement.TryGetProperty("bind", out var bindElement))
            return null;

        if (bindElement.ValueKind != JsonValueKind.String)
            return null;

        if (!bindElement.TryGetGuid(out var bindId))
            return null;

        return bindId;
    }
}
