using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Frisson.Desktop.Converters;

/// <summary>
/// Converts a boolean to an IBrush. Parameter format: "trueHex;falseHex"
/// (e.g. "#D4AF37;#555555" — Active=gold, Inactive=gray).
/// </summary>
public class BoolToBrushConverter : IValueConverter
{
    public static readonly BoolToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isTrue = value is true;
        if (parameter is string paramStr)
        {
            var parts = paramStr.Split(';');
            if (parts.Length == 2)
            {
                var hex = isTrue ? parts[0] : parts[1];
                return Brush.Parse(hex);
            }
        }
        return isTrue ? Brush.Parse("#D4AF37") : Brush.Parse("#555555");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
