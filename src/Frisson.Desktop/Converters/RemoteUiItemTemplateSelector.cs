using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Frisson.Desktop.Converters;

/// <summary>
/// Selects a DataTemplate for a RemoteUiItemViewModel based on its Type property.
/// Implements IDataTemplate to serve as a direct ContentTemplate.
/// </summary>
public class RemoteUiItemTemplateSelector : IDataTemplate
{
    public IDataTemplate? NumberTemplate { get; set; }
    public IDataTemplate? SwitchTemplate { get; set; }
    public IDataTemplate? SelectTemplate { get; set; }
    public IDataTemplate? TextTemplate { get; set; }

    public Control? Build(object? param)
    {
        if (param is ViewModels.RemoteUiItemViewModel vm)
        {
            var template = vm.Type switch
            {
                "number" => NumberTemplate,
                "switch" => SwitchTemplate,
                "select" => SelectTemplate,
                "text" => TextTemplate,
                _ => null
            };
            return template?.Build(param);
        }
        return null;
    }

    public bool Match(object? data)
    {
        return data is ViewModels.RemoteUiItemViewModel;
    }
}
