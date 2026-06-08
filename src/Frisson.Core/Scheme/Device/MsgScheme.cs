using System.Text.Json;
using SchemeBase = Frisson.Core.Scheme.Scheme;

namespace Frisson.Core.Scheme.Device;

/// <summary>
/// DG-LAB msg scheme — envelope + message field parsing.
/// Envelope: { "type": "msg", "clientId": "...", "targetId": "...", "message": "..." }
/// Message field formats:
///   strength-{ch}+{mode}+{val}     (ch: 1=A, 2=B; mode: 0=decrease, 1=increase, 2=set; val: 0-200)
///   pulse-{ch}:{hex_json_array}    (ch: A/B; hex array of 8-byte waveform entries)
///   clear-{ch}                     (ch: 1=A, 2=B)
///   feedback-{idx}                 (0-4: channel A, 5-9: channel B)
///   strength-{a}+{b}+{aMax}+{bMax} (Device status report, 4-part)
/// </summary>
public sealed class MsgScheme : SchemeBase
{
    public override string Type => "msg";

    public string ClientId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public override string ToJson()
    {
        return JsonSerializer.Serialize(new
        {
            type = Type,
            clientId = ClientId,
            targetId = TargetId,
            message = Message
        });
    }

    /// <summary>
    /// Parses a DG-LAB msg envelope from a JsonElement.
    /// </summary>
    public static MsgScheme? FromJson(JsonElement root)
    {
        if (!root.TryGetProperty("clientId", out var clientIdProp) ||
            !root.TryGetProperty("targetId", out var targetIdProp) ||
            !root.TryGetProperty("message", out var messageProp))
            return null;

        return new MsgScheme
        {
            ClientId = clientIdProp.GetString() ?? string.Empty,
            TargetId = targetIdProp.GetString() ?? string.Empty,
            Message = messageProp.GetString() ?? string.Empty
        };
    }

    /// <summary>
    /// Parses a DG-LAB msg envelope from a raw JSON string.
    /// </summary>
    public static MsgScheme? TryParse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp))
                return null;
            if (typeProp.GetString() != "msg")
                return null;

            return FromJson(root);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
