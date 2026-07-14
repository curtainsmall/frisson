using System;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Frisson.Desktop.ViewModels;

public partial class ConfirmDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _confirmText = string.Empty;

    [ObservableProperty]
    private string _cancelText = string.Empty;

    public TaskCompletionSource<bool> Completion { get; } = new();

    public event Action? RequestClose;

    [RelayCommand]
    private void Confirm()
    {
        Completion.TrySetResult(true);
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        Completion.TrySetResult(false);
        RequestClose?.Invoke();
    }
}
