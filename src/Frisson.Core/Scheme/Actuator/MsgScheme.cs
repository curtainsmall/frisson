using System.Text.Json;
using SchemeBase = Frisson.Core.Scheme.Scheme;

namespace Frisson.Core.Scheme.Actuator;

public enum MsgKind
{
    Unknown,
    StrengthCommand,   // strength-{ch}+{mode}+{val}   (ch: 1/2, mode: 0=减/1=增/2=设)
    PulseCommand,      // pulse-{ch}:["hex",...]        (波形)
    ClearCommand,      // clear-{ch}                     (清空队列)
    StrengthStatus,    // strength-{a}+{b}+{aMax}+{bMax} (APP 状态上报)
    Feedback,          // feedback-{idx}                 (APP 按钮反馈)
}

/// <summary>
/// DG-LAB msg scheme — envelope + message field parsing.
/// Envelope: { "type": "msg", "clientId": "...", "targetId": "...", "message": "..." }
/// </summary>
public sealed class MsgScheme : SchemeBase
{
    public override string Type => "msg";

    // Envelope fields
    public string ClientId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    // Parsed message content
    public MsgKind Kind { get; set; } = MsgKind.Unknown;
    public int Channel { get; set; }       // 1/2 for strength/clear, -1 for pulse (encoded in raw)
    public int Mode { get; set; }          // 0=减, 1=增, 2=设 (only for StrengthCommand)
    public int Value { get; set; }         // strength value 0-200 (only for StrengthCommand)
    public int StrengthA { get; set; }     // APP status report
    public int StrengthB { get; set; }
    public int MaxA { get; set; }
    public int MaxB { get; set; }
    public int FeedbackIndex { get; set; } // 0-4: channel A, 5-9: channel B
    public string PulseData { get; set; } = string.Empty; // raw hex JSON array string

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

    public static MsgScheme? FromJson(JsonElement root)
    {
        if (!root.TryGetProperty("clientId", out var clientIdProp) ||
            !root.TryGetProperty("targetId", out var targetIdProp) ||
            !root.TryGetProperty("message", out var messageProp))
            return null;

        var msg = new MsgScheme
        {
            ClientId = clientIdProp.GetString() ?? string.Empty,
            TargetId = targetIdProp.GetString() ?? string.Empty,
            Message = messageProp.GetString() ?? string.Empty
        };
        ParseMessage(msg);
        return msg;
    }

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

    /// <summary>
    /// Parses the message field content and fills parsed properties.
    /// </summary>
    private static void ParseMessage(MsgScheme msg)
    {
        var content = msg.Message ?? string.Empty;

        if (content.StartsWith("strength-"))
        {
            var parts = content["strength-".Length..].Split('+');
            if (parts.Length == 3 && int.TryParse(parts[0], out var ch) &&
                int.TryParse(parts[1], out var mode) && int.TryParse(parts[2], out var val))
            {
                msg.Kind = MsgKind.StrengthCommand;
                msg.Channel = ch;
                msg.Mode = mode;
                msg.Value = val;
                return;
            }
            if (parts.Length == 4 && int.TryParse(parts[0], out var a) &&
                int.TryParse(parts[1], out var b) && int.TryParse(parts[2], out var aMax) &&
                int.TryParse(parts[3], out var bMax))
            {
                msg.Kind = MsgKind.StrengthStatus;
                msg.StrengthA = a;
                msg.StrengthB = b;
                msg.MaxA = aMax;
                msg.MaxB = bMax;
                return;
            }
        }

        if (content.StartsWith("pulse-"))
        {
            var colonIdx = content.IndexOf(':');
            if (colonIdx > "pulse-".Length)
            {
                var chStr = content["pulse-".Length..colonIdx];
                msg.Kind = MsgKind.PulseCommand;
                msg.Channel = chStr == "A" ? 1 : (chStr == "B" ? 2 : 0);
                msg.PulseData = content[(colonIdx + 1)..];
                return;
            }
        }

        if (content.StartsWith("clear-"))
        {
            var chStr = content["clear-".Length..];
            if (int.TryParse(chStr, out var ch))
            {
                msg.Kind = MsgKind.ClearCommand;
                msg.Channel = ch;
                return;
            }
        }

        if (content.StartsWith("feedback-"))
        {
            var idxStr = content["feedback-".Length..];
            if (int.TryParse(idxStr, out var idx))
            {
                msg.Kind = MsgKind.Feedback;
                msg.FeedbackIndex = idx;
                return;
            }
        }
    }
}
