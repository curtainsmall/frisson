using System.Text.Json;
using SchemeBase = Frisson.Core.Scheme.Scheme;

namespace Frisson.Core.Scheme.Remote;

/// <summary>
/// Control bind reply/confirm scheme.
/// Format: { "type": "bind", "id": "...", "name": "...", "alwaysReply": true }
/// </summary>
public sealed class BindScheme : SchemeBase
{
    public override string Type => "bind";

    /// <summary>
    /// The controller's assigned UUID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The controller's display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// When true, the server will always reply with an ack for every message,
    /// even if the state did not change or the remote is not active.
    /// When false or omitted, the server keeps silent when there is nothing to report.
    /// </summary>
    public bool AlwaysReply { get; set; }

    /// <summary>
    /// Optional UI declaration array sent by the Remote.
    /// Null if the Remote did not declare any UI.
    /// </summary>
    public List<UiItem>? Ui { get; set; }

    public override string ToJson()
    {
        return JsonSerializer.Serialize(new
        {
            type = Type,
            id = Id.ToString(),
            name = Name,
            alwaysReply = AlwaysReply
        });
    }

    /// <summary>
    /// Parses a Control bind message from a JsonElement.
    /// "id" is optional — Remote may not send it; Frisson assigns the UUID.
    /// </summary>
    public static BindScheme? FromJson(JsonElement root)
    {
        Guid id = Guid.Empty;
        if (root.TryGetProperty("id", out var idProp))
        {
            var idStr = idProp.GetString() ?? string.Empty;
            if (!Guid.TryParse(idStr, out id))
                return null;
        }

        root.TryGetProperty("name", out var nameProp);

        bool alwaysReply = false;
        if (root.TryGetProperty("alwaysReply", out var arProp))
            alwaysReply = arProp.GetBoolean();

        List<UiItem>? ui = null;
        if (root.TryGetProperty("ui", out var uiProp) && uiProp.ValueKind == JsonValueKind.Array)
        {
            ui = new List<UiItem>();
            foreach (var el in uiProp.EnumerateArray())
            {
                var item = UiItem.FromJson(el);
                if (item == null)
                    return null; // malformed UI item → reject entire bind
                ui.Add(item);
            }
        }

        return new BindScheme
        {
            Id = id,
            Name = nameProp.GetString() ?? string.Empty,
            AlwaysReply = alwaysReply,
            Ui = ui
        };
    }

    /// <summary>
    /// Attempts to parse a raw JSON string as a Remote bind message.
    /// Returns null if it doesn't match the Remote bind format.
    /// </summary>
    public static BindScheme? TryParse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp))
                return null;
            if (typeProp.GetString() != "bind")
                return null;

            // Remote bind has "id" and "name" but NOT "clientId"/"targetId"
            if (root.TryGetProperty("clientId", out _))
                return null;

            return FromJson(root);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
