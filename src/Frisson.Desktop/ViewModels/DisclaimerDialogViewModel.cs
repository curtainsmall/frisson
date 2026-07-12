using System;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Frisson.Desktop.Services;

namespace Frisson.Desktop.ViewModels;

public partial class DisclaimerDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _body = string.Empty;

    [ObservableProperty]
    private string _closeText = string.Empty;

    public static string ProviderUrl => "https://dungeon-lab.com/help-support.html";

    public event Action? RequestClose;

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void OpenProviderLink()
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = ProviderUrl,
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(psi);
    }
}
