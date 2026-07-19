using System.Text.Json;

namespace Frisson.Core.Scheme.Remote;

/// <summary>
/// Represents a single UI declaration item in a Remote's bind message.
/// Supports types: group, number, switch, select, text.
/// </summary>
public class UiItem
{
    public string Type { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string? Title { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public double Step { get; set; } = 1;
    public JsonElement? Value { get; set; }
    public List<string>? Options { get; set; }

    public static UiItem? FromJson(JsonElement el)
    {
        if (!el.TryGetProperty("type", out var typeProp) ||
            !el.TryGetProperty("key", out var keyProp))
            return null;

        var item = new UiItem
        {
            Type = typeProp.GetString() ?? string.Empty,
            Key = keyProp.GetString() ?? string.Empty,
        };

        if (el.TryGetProperty("label", out var labelProp))
            item.Label = labelProp.GetString();

        if (el.TryGetProperty("title", out var titleProp))
            item.Title = titleProp.GetString();

        if (el.TryGetProperty("min", out var minProp) && minProp.ValueKind == JsonValueKind.Number)
            item.Min = minProp.GetDouble();

        if (el.TryGetProperty("max", out var maxProp) && maxProp.ValueKind == JsonValueKind.Number)
            item.Max = maxProp.GetDouble();

        if (el.TryGetProperty("step", out var stepProp) && stepProp.ValueKind == JsonValueKind.Number)
            item.Step = stepProp.GetDouble();

        if (el.TryGetProperty("value", out var valueProp))
            item.Value = valueProp;

        if (el.TryGetProperty("options", out var optsProp) && optsProp.ValueKind == JsonValueKind.Array)
        {
            var list = new List<string>();
            foreach (var o in optsProp.EnumerateArray())
            {
                if (o.ValueKind == JsonValueKind.String)
                    list.Add(o.GetString() ?? string.Empty);
            }
            if (list.Count > 0)
                item.Options = list;
        }

        return item;
    }

    /// <summary>
    /// Validates the entire UI declaration array.
    /// Returns null if valid, or an error message string if invalid.
    /// </summary>
    public static string? Validate(List<UiItem> items)
    {
        if (items.Count == 0)
            return "EmptyUI";

        var allowedTypes = new HashSet<string> { "group", "number", "switch", "select", "text" };
        var keys = new HashSet<string>();

        foreach (var item in items)
        {
            if (!allowedTypes.Contains(item.Type))
                return $"UnknownType:{item.Type}";

            if (string.IsNullOrEmpty(item.Key))
                return "MissingKey";

            if (!keys.Add(item.Key))
                return $"DuplicateKey:{item.Key}";

            switch (item.Type)
            {
                case "number":
                    if (item.Min == null || item.Max == null)
                        return $"MissingMinMax:{item.Key}";
                    if (item.Min > item.Max)
                        return $"MinGtMax:{item.Key}";
                    if (item.Step == 0)
                        return $"StepZero:{item.Key}";
                    break;

                case "switch":
                    if (item.Value.HasValue && item.Value.Value.ValueKind != JsonValueKind.True &&
                        item.Value.Value.ValueKind != JsonValueKind.False)
                        return $"InvalidSwitchValue:{item.Key}";
                    break;

                case "select":
                    if (item.Options == null || item.Options.Count == 0)
                        return $"EmptyOptions:{item.Key}";
                    break;

                case "text":
                    if (!item.Value.HasValue)
                        return $"MissingTextValue:{item.Key}";
                    break;
            }
        }

        return null; // valid
    }
}
