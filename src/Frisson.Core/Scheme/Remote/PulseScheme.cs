using System.Text.Json;
using SchemeBase = Frisson.Core.Scheme.Scheme;

namespace Frisson.Core.Scheme.Remote;

/// <summary>
/// Remote pulse control message.
/// Format: { "type": "pulse", "channel": "A"|"B", "value": ["hex1", "hex2", ...] }
/// Each entry is an 8-byte HEX string representing 100ms of waveform data.
/// </summary>
public sealed class PulseScheme : SchemeBase
{
    public override string Type => "pulse";

    /// <summary>
    /// Channel: "A" or "B".
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Array of 8-byte HEX waveform strings (max 100 entries).
    /// </summary>
    public List<string> Value { get; set; } = [];

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
    public static PulseScheme? FromJson(JsonElement root)
    {
        if (!root.TryGetProperty("channel", out var channelProp) ||
            !root.TryGetProperty("value", out var valueProp))
            return null;

        if (valueProp.ValueKind != JsonValueKind.Array)
            return null;

        var values = new List<string>();
        foreach (var item in valueProp.EnumerateArray())
        {
            var str = item.GetString();
            if (str != null)
                values.Add(str);
        }

        return new PulseScheme
        {
            Channel = channelProp.GetString() ?? string.Empty,
            Value = values
        };
    }
}
