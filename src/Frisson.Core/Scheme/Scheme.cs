using System.Text.Json;

namespace Frisson.Core.Scheme;

/// <summary>
/// Abstract base class for all protocol message schemes.
/// Each subclass represents a specific message type with its own JSON structure.
/// </summary>
public abstract class Scheme
{
    /// <summary>
    /// The "type" field value for this message scheme.
    /// </summary>
    public abstract string Type { get; }

    /// <summary>
    /// Parses a JSON string and dispatches to the appropriate scheme subclass.
    /// Returns null if the type is unknown or the JSON is invalid.
    /// </summary>
    public static Scheme? Parse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp))
                return null;

            var type = typeProp.GetString();
            return type switch
            {
                "bind" => TryParseBind(root),
                _ => null
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Serializes this scheme to a JSON string.
    /// </summary>
    public abstract string ToJson();

    /// <summary>
    /// Tries to parse a bind message — dispatches to Device or Control based on fields.
    /// Device bind has clientId/targetId; Control bind has id/name.
    /// </summary>
    private static Scheme? TryParseBind(JsonElement root)
    {
        if (root.TryGetProperty("clientId", out _))
            return Device.BindScheme.FromJson(root);
        return Control.BindScheme.FromJson(root);
    }
}
