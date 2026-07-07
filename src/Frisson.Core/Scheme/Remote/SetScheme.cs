using System.Text.Json;
using SchemeBase = Frisson.Core.Scheme.Scheme;

namespace Frisson.Core.Scheme.Remote;

public sealed class SetScheme : SchemeBase
{
    public override string Type => "set";

    /// <summary>
    /// Channel identifier: "A" or "B" (case-insensitive).
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Absolute strength value (non-negative).
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

    public static SetScheme? FromJson(JsonElement root)
    {
        if (!root.TryGetProperty("type", out var typeProp) ||
            typeProp.GetString() != "set")
            return null;

        root.TryGetProperty("channel", out var channelProp);
        root.TryGetProperty("value", out var valueProp);

        return new SetScheme
        {
            Channel = channelProp.GetString() ?? string.Empty,
            Value = valueProp.ValueKind == JsonValueKind.Number ? valueProp.GetInt32() : 0
        };
    }
}
