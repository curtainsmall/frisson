using System.Text.Json;
using System.Text.Json.Nodes;

namespace Frisson.Core;

/// <summary>
/// Generic key-value settings store in %APPDATA%/Frisson/settings.json.
/// Only values explicitly set via Set() are persisted. Absent keys mean
/// "use the application's code default" — the caller provides the fallback.
/// </summary>
public sealed class SettingsService
{
    private static readonly Lazy<SettingsService> _instance = new(() => new SettingsService());
    public static SettingsService Instance => _instance.Value;

    private const string FileName = "settings.json";

    private string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Frisson",
        FileName);

    private JsonObject _data = new();

    private SettingsService()
    {
        Load();
    }

    /// <summary>Get a persisted value, or default if absent. Returns false if key not found.</summary>
    public bool TryGet<T>(string key, out T value)
    {
        if (_data.TryGetPropertyValue(key, out var node) && node is not null)
        {
            value = node.GetValue<T>();
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>Set a value to be persisted on next Save().</summary>
    public void Set<T>(string key, T value)
    {
        _data[key] = JsonValue.Create(value);
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                _data = new JsonObject();
                return;
            }

            var root = JsonNode.Parse(File.ReadAllText(FilePath));
            _data = root as JsonObject ?? new JsonObject();
        }
        catch
        {
            _data = new JsonObject();
        }
    }

    public void Save()
    {
        try
        {
            if (_data.Count == 0)
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
                return;
            }

            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(FilePath, _data.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Ignore save errors
        }
    }
}
