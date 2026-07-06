using System;
using System.Globalization;
using System.Reflection;

using Avalonia.Data.Converters;

namespace Frisson.Desktop.Converters;

/// <summary>
/// Converts (ignored) to the application version string, or a localized "Unknown"
/// when no real version is configured (i.e. the SDK default 1.0.0.0).
/// </summary>
public class VersionConverter : IValueConverter
{
    public static readonly VersionConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var assembly = typeof(VersionConverter).Assembly;

        // Check if publish.py explicitly set a non-default AssemblyVersion.
        // Debug/default builds use 1.0.0.0; publish.py sets {major}.{minor}.{patch}.1.
        var v = assembly.GetName().Version;
        if (v is null || v.Equals(new Version(1, 0, 0, 0)))
            return Services.LocalizationService.Instance["VersionUnknown"];

        // Release build: prefer InformationalVersion for full SemVer display
        // (catches prerelease like "1.0.0-beta.1" and build metadata).
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrEmpty(informational))
            return informational;

        // Fallback to numeric (3-segment, omitting the .1 revision)
        return v.ToString(3);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
