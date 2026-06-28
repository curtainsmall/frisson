using System.Text.Json;
using SchemeBase = Frisson.Core.Scheme.Scheme;

namespace Frisson.Core.Scheme.Device;

/// <summary>
/// DG-LAB bind request/confirm scheme.
/// Format: { "type": "bind", "clientId": "...", "targetId": "...", "message": "..." }
/// </summary>
public sealed class BindScheme : SchemeBase
{
    public override string Type => "bind";

    /// <summary>
    /// Frontend UUID (in DG-LAB convention, always the frontend).
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// Device UUID (in DG-LAB convention, always the device).
    /// </summary>
    public Guid TargetId { get; set; }

    /// <summary>
    /// Message content (e.g., "200" for success, "targetId" for initial bind).
    /// </summary>
    public string Message { get; set; } = string.Empty;

    public override string ToJson()
    {
        return JsonSerializer.Serialize(new
        {
            type = Type,
            clientId = ClientId.ToString(),
            targetId = TargetId == Guid.Empty ? "" : TargetId.ToString(),
            message = Message
        });
    }

    /// <summary>
    /// Parses a DG-LAB bind message from a JsonElement.
    /// Returns null if the required fields are missing.
    /// </summary>
    public static BindScheme? FromJson(JsonElement root)
    {
        if (!root.TryGetProperty("clientId", out var clientIdProp) ||
            !root.TryGetProperty("targetId", out var targetIdProp) ||
            !root.TryGetProperty("message", out var messageProp))
            return null;

        if (!Guid.TryParse(clientIdProp.GetString(), out var clientId))
            return null;

        var targetIdStr = targetIdProp.GetString() ?? string.Empty;
        var targetId = string.IsNullOrEmpty(targetIdStr)
            ? Guid.Empty
            : Guid.TryParse(targetIdStr, out var tid) ? tid : Guid.Empty;

        return new BindScheme
        {
            ClientId = clientId,
            TargetId = targetId,
            Message = messageProp.GetString() ?? string.Empty
        };
    }

    /// <summary>
    /// Attempts to parse a raw JSON string as a Device bind message.
    /// Returns null if it doesn't match the Device bind format (missing clientId/targetId).
    /// </summary>
    public static BindScheme? TryParseDeviceBind(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp))
                return null;
            if (typeProp.GetString() != "bind")
                return null;

            return FromJson(root);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
