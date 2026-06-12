using System.Text.Json;
using SchemeBase = Frisson.Core.Scheme.Scheme;

namespace Frisson.Core.Scheme.Control;

/// <summary>
/// Control bind reply/confirm scheme.
/// Format: { "type": "bind", "id": "...", "name": "..." }
/// </summary>
public sealed class BindScheme : SchemeBase
{
    public override string Type => "bind";

    /// <summary>
    /// The controller's assigned UUID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The controller's display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    public override string ToJson()
    {
        return JsonSerializer.Serialize(new
        {
            type = Type,
            id = Id.ToString(),
            name = Name
        });
    }

    /// <summary>
    /// Parses a Control bind message from a JsonElement.
    /// Returns null if the "id" field is missing.
    /// </summary>
    public static BindScheme? FromJson(JsonElement root)
    {
        if (!root.TryGetProperty("id", out var idProp))
            return null;

        var idStr = idProp.GetString() ?? string.Empty;
        if (!Guid.TryParse(idStr, out var id))
            return null;

        root.TryGetProperty("name", out var nameProp);

        return new BindScheme
        {
            Id = id,
            Name = nameProp.GetString() ?? string.Empty
        };
    }

    /// <summary>
    /// Attempts to parse a raw JSON string as a Control bind message.
    /// Returns null if it doesn't match the Control bind format.
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

            // Control bind has "id" and "name" but NOT "clientId"/"targetId"
            if (root.TryGetProperty("clientId", out _))
                return null;

            return FromJson(root);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
