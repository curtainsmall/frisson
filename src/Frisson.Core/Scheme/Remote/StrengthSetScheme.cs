using System.Text.Json;
using SchemeBase = Frisson.Core.Scheme.Scheme;

namespace Frisson.Core.Scheme.Remote;

/// <summary>
/// Remote strength-set control message.
/// Format: { "type": "strength-set", "channel": "A"|"B", "value": 0-200 }
/// </summary>
public sealed class StrengthSetScheme : SchemeBase
{
    public override string Type => "strength-set";

    /// <summary>
    /// Channel: "A" or "B".
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Target strength value (0-200).
    /// </summary>
    public int Value { get; set; }

    public override string ToJson()
    {
        return JsonSerializer.Serialize(new
        {
            type = Type,
            channel = Channel,
            value = Value
        });
    }

    /// <summary>
    /// Parses from a JsonElement. Returns null if required fields are missing.
    /// </summary>
    public static StrengthSetScheme? FromJson(JsonElement root)
    {
        if (!root.TryGetProperty("channel", out var channelProp) ||
            !root.TryGetProperty("value", out var valueProp))
            return null;

        return new StrengthSetScheme
        {
            Channel = channelProp.GetString() ?? string.Empty,
            Value = valueProp.GetInt32()
        };
    }
}
