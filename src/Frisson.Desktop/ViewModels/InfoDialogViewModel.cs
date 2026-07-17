using System;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Frisson.Desktop.ViewModels;

public partial class InfoDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _closeText = string.Empty;

    public event Action? RequestClose;

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }
}
