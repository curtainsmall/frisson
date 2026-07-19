using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Frisson.Core;

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

    public void Clear()
    {
        Entries.Clear();
        
        // Also clear log files from disk
        try
        {
            var logDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Frisson", "logs");
            
            if (System.IO.Directory.Exists(logDir))
            {
                var logFiles = System.IO.Directory.GetFiles(logDir, "*.log");
                foreach (var file in logFiles)
                {
                    System.IO.File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[Logger] Failed to clear log files: {ex.Message}");
        }
    }

    public void KeepRecent(int count)
    {
        // Keep only the latest 'count' entries in memory
        while (Entries.Count > count)
            Entries.RemoveAt(0);
        
        // Keep only the latest 'count' log files on disk
        try
        {
            var logDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Frisson", "logs");

            if (System.IO.Directory.Exists(logDir))
            {
                var logFiles = System.IO.Directory.GetFiles(logDir, "*.log")
                    .OrderByDescending(f => System.IO.File.GetLastWriteTime(f))
                    .Skip(count)
                    .ToArray();
                foreach (var file in logFiles)
                {
                    System.IO.File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[Logger] Failed to trim log files: {ex.Message}");
        }
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
