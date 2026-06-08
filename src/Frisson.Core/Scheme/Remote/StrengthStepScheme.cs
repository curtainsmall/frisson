using System.Text.Json;
using SchemeBase = Frisson.Core.Scheme.Scheme;

namespace Frisson.Core.Scheme.Remote;

/// <summary>
/// Remote strength-step control message.
/// Format: { "type": "strength-step", "channel": "A"|"B", "value": int }
/// value: positive = increase, negative = decrease.
/// </summary>
public sealed class StrengthStepScheme : SchemeBase
{
    public override string Type => "strength-step";

    /// <summary>
    /// Channel: "A" or "B".
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Relative step value (positive = increase, negative = decrease).
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
    public static StrengthStepScheme? FromJson(JsonElement root)
    {
        if (!root.TryGetProperty("channel", out var channelProp) ||
            !root.TryGetProperty("value", out var valueProp))
            return null;

        return new StrengthStepScheme
        {
            Channel = channelProp.GetString() ?? string.Empty,
            Value = valueProp.GetInt32()
        };
    }
}
