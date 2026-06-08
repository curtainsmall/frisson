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
    /// Parses a JSON string and dispatches to the appropriate Remote scheme subclass.
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
                "bind" => Remote.BindScheme.FromJson(root),
                "strength-set" => Remote.StrengthSetScheme.FromJson(root),
                "strength-step" => Remote.StrengthStepScheme.FromJson(root),
                "pulse" => Remote.PulseScheme.FromJson(root),
                "clear" => Remote.ClearScheme.FromJson(root),
                "error" => Remote.ErrorScheme.FromJson(root),
                "msg" => new Remote.MsgScheme(),
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
}
