using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace Frisson.Desktop.Converters;

/// <summary>
/// Converts a boolean value to an icon string.
/// Converter parameter format: "trueIcon;falseIcon" (e.g., "fa-solid fa-chevron-right;fa-solid fa-chevron-left")
/// </summary>
public class BoolToIconConverter : IValueConverter
{
    public static BoolToIconConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string paramStr)
        {
            var icons = paramStr.Split(';');
            if (icons.Length == 2)
            {
                return boolValue ? icons[0] : icons[1];
            }
        }
        return "fa-solid fa-question";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
