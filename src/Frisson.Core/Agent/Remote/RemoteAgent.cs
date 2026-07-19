using System.Text.Json;

using Frisson.Core.Scheme;

using RemoteUiItem = Frisson.Core.Scheme.Remote.UiItem;

namespace Frisson.Core.Agent.Remote;

public sealed class RemoteAgent : Agent
{
    private static readonly string InvalidJson = JsonSerializer.Serialize(new { type = "error", message = "Invalid" });
    private static readonly string InactiveJson = JsonSerializer.Serialize(new { type = "error", message = "Inactive" });
    private static readonly string UnknownKeyJson = JsonSerializer.Serialize(new { type = "error", message = "UnknownKey" });
    private static readonly string OutOfRangeJson = JsonSerializer.Serialize(new { type = "error", message = "OutOfRange" });
    private static readonly string InvalidValueJson = JsonSerializer.Serialize(new { type = "error", message = "InvalidValue" });

    public string Name { get; }
    public bool AlwaysReply { get; set; }

    /// <summary>
    /// Whether this Remote is currently the active (writing) remote.
    /// Set by AgentManager on activation/deactivation.
    /// </summary>
    public bool IsActive { get; set; }

    public Action<Scheme.Scheme>? ForwardToControlDesk { get; set; }

    /// <summary>
    /// UI declaration from the Remote's bind message.
    /// Null if the Remote did not declare any UI.
    /// </summary>
    public List<RemoteUiItem>? Ui { get; set; }

    /// <summary>
    /// Fires when the Remote sends a type:ui value update.
    /// Parameters: (key, rawValue)
    /// </summary>
    public event Action<string, object?>? UiValueChanged;

    public RemoteAgent(Guid id, string name, Action? onDisposing = null) : base(id, onDisposing)
    {
        Name = name;
    }

    public override async Task HandleMessage(string json)
    {
        // Try type:ui message first
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "ui")
            {
                HandleUiMessage(root);
                return;
            }
        }
        catch (JsonException)
        {
            SendFunc?.Invoke(InvalidJson);
            return;
        }

        // Fall through to existing scheme parsing (set/vary)
        var scheme = Scheme.Scheme.Parse(json);
        if (scheme == null)
        {
            SendFunc?.Invoke(InvalidJson);
            return;
        }

        ForwardToControlDesk?.Invoke(scheme);
        await Task.CompletedTask;
    }

    private void HandleUiMessage(JsonElement root)
    {
        // Inactive Remote may not send UI commands
        if (!IsActive)
        {
            SendFunc?.Invoke(InactiveJson);
            return;
        }

        // Extract key
        if (!root.TryGetProperty("key", out var keyProp))
        {
            SendFunc?.Invoke(InvalidValueJson);
            return;
        }
        var key = keyProp.GetString() ?? string.Empty;

        // Validate key exists in declaration
        if (Ui == null)
        {
            SendFunc?.Invoke(UnknownKeyJson);
            return;
        }

        var declaration = Ui.FirstOrDefault(u => u.Key == key);
        if (declaration == null)
        {
            SendFunc?.Invoke(UnknownKeyJson);
            return;
        }

        // Extract and validate value
        if (!root.TryGetProperty("value", out var valueProp))
        {
            SendFunc?.Invoke(InvalidValueJson);
            return;
        }

        object? rawValue = null;

        switch (declaration.Type)
        {
            case "number":
                if (valueProp.ValueKind != JsonValueKind.Number)
                {
                    SendFunc?.Invoke(InvalidValueJson);
                    return;
                }
                var numVal = valueProp.GetDouble();
                if (declaration.Min.HasValue && declaration.Max.HasValue &&
                    (numVal < declaration.Min.Value || numVal > declaration.Max.Value))
                {
                    SendFunc?.Invoke(OutOfRangeJson);
                    return;
                }
                declaration.Value = valueProp;
                rawValue = numVal;
                break;

            case "switch":
                if (valueProp.ValueKind != JsonValueKind.True && valueProp.ValueKind != JsonValueKind.False)
                {
                    SendFunc?.Invoke(InvalidValueJson);
                    return;
                }
                declaration.Value = valueProp;
                rawValue = valueProp.GetBoolean();
                break;

            case "select":
                if (valueProp.ValueKind != JsonValueKind.String)
                {
                    SendFunc?.Invoke(InvalidValueJson);
                    return;
                }
                var strVal = valueProp.GetString() ?? string.Empty;
                if (declaration.Options != null && !declaration.Options.Contains(strVal))
                {
                    SendFunc?.Invoke(OutOfRangeJson);
                    return;
                }
                declaration.Value = valueProp;
                rawValue = strVal;
                break;

            case "text":
                // Text is read-only from Frisson perspective
                return;

            default:
                SendFunc?.Invoke(InvalidValueJson);
                return;
        }

        UiValueChanged?.Invoke(key, rawValue);

        // Ack if AlwaysReply is set
        if (AlwaysReply)
        {
            SendFunc?.Invoke(JsonSerializer.Serialize(new { type = "ack" }));
        }
    }

    /// <summary>
    /// Sends a type:ui value update to the Remote.
    /// </summary>
    public Task SendUiValue(string key, object? value)
    {
        var json = JsonSerializer.Serialize(new { type = "ui", key, value });
        return SendFunc?.Invoke(json) ?? Task.CompletedTask;
    }
}
