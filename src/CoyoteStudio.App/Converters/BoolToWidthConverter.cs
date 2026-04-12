using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace CoyoteStudio.App.Converters;

/// <summary>
/// Converts a boolean value to a width value (double).
/// When true, returns the converter parameter as width; when false, returns 40 (collapsed width).
/// </summary>
public class BoolToWidthConverter : IValueConverter
{
    public static BoolToWidthConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            if (isExpanded)
            {
                // Parse the parameter as the expanded width
                if (parameter is string paramStr && double.TryParse(paramStr, out var width))
                {
                    return width;
                }
                return 300; // Default expanded width
            }
            else
            {
                return 40; // Collapsed width (just enough for the toggle button)
            }
        }
        return 300;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
