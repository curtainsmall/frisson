using System;
using System.Globalization;

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
        var v = typeof(VersionConverter).Assembly.GetName().Version;
        if (v is not null && !v.Equals(new Version(1, 0, 0, 0)))
            return v.ToString();
        return Services.LocalizationService.Instance["VersionUnknown"];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
