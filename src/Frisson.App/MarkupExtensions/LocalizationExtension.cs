using System;

using Avalonia.Data;
using Avalonia.Markup.Xaml;

using Frisson.App.Services;

namespace Frisson.App.MarkupExtensions;

public class LocExtension : MarkupExtension
{
    private readonly string _key;

    public LocExtension(string key)
    {
        _key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // Create a binding that updates when CurrentCulture changes
        // We use a converter that takes the key and returns the localized string
        var binding = new Binding(nameof(LocalizationService.CurrentCulture))
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay,
            Converter = new LocConverter(_key)
        };
        return binding;
    }
}
