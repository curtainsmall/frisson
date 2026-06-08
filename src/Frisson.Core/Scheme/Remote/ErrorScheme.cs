using System.Text.Json;
using SchemeBase = Frisson.Core.Scheme.Scheme;

namespace Frisson.Core.Scheme.Remote;

/// <summary>
/// Remote error message.
/// Format: { "type": "error", "message": "description" }
/// Protocol errors for Remote use descriptive text (unlike Device which uses numeric codes).
/// </summary>
public sealed class ErrorScheme : SchemeBase
{
    public override string Type => "error";

    /// <summary>
    /// Human-readable error description.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    public override string ToJson()
    {
        return JsonSerializer.Serialize(new
        {
            type = Type,
            message = Message
        });
    }

    /// <summary>
    /// Parses from a JsonElement. Returns null if the "message" field is missing.
    /// </summary>
    public static ErrorScheme? FromJson(JsonElement root)
    {
        if (!root.TryGetProperty("message", out var messageProp))
            return null;

        return new ErrorScheme
        {
            Message = messageProp.GetString() ?? string.Empty
        };
    }
}
