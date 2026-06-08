using System.Text.Json;
using SchemeBase = Frisson.Core.Scheme.Scheme;

namespace Frisson.Core.Scheme.Remote;

/// <summary>
/// Remote bind reply/confirm scheme.
/// Format: { "type": "bind", "id": "..." }
/// No clientId/targetId fields — this is the Remote protocol format.
/// </summary>
public sealed class BindScheme : SchemeBase
{
    public override string Type => "bind";

    /// <summary>
    /// The Remote's assigned UUID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    public override string ToJson()
    {
        return JsonSerializer.Serialize(new
        {
            type = Type,
            id = Id
        });
    }

    /// <summary>
    /// Parses a Remote bind message from a JsonElement.
    /// Returns null if the "id" field is missing.
    /// </summary>
    public static BindScheme? FromJson(JsonElement root)
    {
        if (!root.TryGetProperty("id", out var idProp))
            return null;

        return new BindScheme
        {
            Id = idProp.GetString() ?? string.Empty
        };
    }

    /// <summary>
    /// Attempts to parse a raw JSON string as a Remote bind message.
    /// Returns null if it doesn't match the Remote bind format (missing "id" field).
    /// </summary>
    public static BindScheme? TryParse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp))
                return null;
            if (typeProp.GetString() != "bind")
                return null;

            return FromJson(root);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
