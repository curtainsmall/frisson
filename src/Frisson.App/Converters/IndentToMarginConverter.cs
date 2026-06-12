using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace Frisson.App.Converters;

/// <summary>
/// Converts an indent level (int) to a left margin (double) for visual indentation.
/// </summary>
public class IndentToMarginConverter : IValueConverter
{
    public static readonly IndentToMarginConverter Instance = new();

    private const double IndentPerLevel = 24.0;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int level)
            return level * IndentPerLevel;
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
