using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Threading;

using Avalonia.Data.Converters;
using System.Collections.Generic;

namespace CoyoteStudio.App.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private static LocalizationService? _instance;
    public static LocalizationService Instance => _instance ??= new LocalizationService();

    private readonly ResourceManager _resourceManager;
    private CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

    public event PropertyChangedEventHandler? PropertyChanged;

    private LocalizationService()
    {
        _resourceManager = new ResourceManager("CoyoteStudio.App.Assets.Resources", typeof(LocalizationService).Assembly);
        // Default to en-US if current culture is not supported
        if (_currentCulture.Name != "en-US" && _currentCulture.Name != "zh-CN")
        {
            _currentCulture = new CultureInfo("en-US");
        }
    }

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture != value)
            {
                _currentCulture = value;
                CultureInfo.CurrentCulture = value;
                CultureInfo.CurrentUICulture = value;
                Thread.CurrentThread.CurrentCulture = value;
                Thread.CurrentThread.CurrentUICulture = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            }
        }
    }

    public string this[string key]
    {
        get
        {
            var value = _resourceManager.GetString(key, _currentCulture);
            return value ?? key;
        }
    }

    public string GetString(string key)
    {
        return this[key];
    }

    public void SetLanguage(string languageCode)
    {
        CurrentCulture = new CultureInfo(languageCode);
    }
}

public class LocConverter : IValueConverter
{
    private readonly string _key;

    public LocConverter(string key)
    {
        _key = key;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // value is the CurrentCulture, we ignore it and just get the localized string
        return LocalizationService.Instance[_key];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// A multi-value converter that takes a resource key and current culture, returns the localized string.
/// Used for dynamic localization in bindings that need to update when language changes.
/// </summary>
public class LocMultiConverter : IMultiValueConverter
{
    private static LocMultiConverter? _instance;
    public static LocMultiConverter Instance => _instance ??= new LocMultiConverter();

    private LocMultiConverter()
    {
    }

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 1 && values[0] is string key)
        {
            return LocalizationService.Instance[key];
        }
        return values[0]?.ToString();
    }

    public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
