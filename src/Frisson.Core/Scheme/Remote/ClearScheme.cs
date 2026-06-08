using System.Text.Json;
using SchemeBase = Frisson.Core.Scheme.Scheme;

namespace Frisson.Core.Scheme.Remote;

/// <summary>
/// Remote clear control message.
/// Format: { "type": "clear", "channel": "A"|"B" }
/// </summary>
public sealed class ClearScheme : SchemeBase
{
    public override string Type => "clear";

    /// <summary>
    /// Channel: "A" or "B".
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    public override string ToJson()
    {
        return JsonSerializer.Serialize(new
        {
            type = Type,
            channel = Channel
        });
    }

    /// <summary>
    /// Parses from a JsonElement. Returns null if required fields are missing.
    /// </summary>
    public static ClearScheme? FromJson(JsonElement root)
    {
        if (!root.TryGetProperty("channel", out var channelProp))
            return null;

        return new ClearScheme
        {
            Channel = channelProp.GetString() ?? string.Empty
        };
    }
}
