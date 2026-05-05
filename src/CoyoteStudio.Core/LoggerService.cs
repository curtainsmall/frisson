using System;
using System.Collections.ObjectModel;

namespace CoyoteStudio.Core;

/// <summary>
/// Simple in-memory logger for WebSocket events and diagnostics.
/// </summary>
public sealed class LoggerService
{
    private static readonly Lazy<LoggerService> _instance = new(() => new LoggerService());
    public static LoggerService Instance => _instance.Value;

    public ObservableCollection<LogEntry> Entries { get; } = new();

    private LoggerService() { }

    public void Log(string message)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Message = message
        };
        Entries.Add(entry);
    }

    public void SaveToFile(string path)
    {
        var directory = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            System.IO.Directory.CreateDirectory(directory);

        var lines = Entries.Select(e => e.Formatted);
        System.IO.File.WriteAllLines(path, lines);
    }
}

public sealed class LogEntry
{
    public DateTime Timestamp { get; init; }
    public string Message { get; init; } = string.Empty;

    public string Formatted => $"[{Timestamp:HH:mm:ss.fff}] {Message}";
}
