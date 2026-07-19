using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace Frisson.Desktop.Converters;

/// <summary>
/// Returns true when the bound value is not null (or not an empty string).
/// Used to conditionally show labels.
/// </summary>
public class IsNotNullConverter : IValueConverter
{
    public static readonly IsNotNullConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string s ? s.Length > 0 : value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
