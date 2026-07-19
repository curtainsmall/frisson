using System;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Frisson.Desktop.ViewModels;

public enum ClearLogsAction { None, ClearAll, KeepRecent }

public partial class ClearLogsDialogViewModel : ViewModelBase
{
    public TaskCompletionSource<ClearLogsAction> Completion { get; } = new();
    public event Action? RequestClose;

    [RelayCommand]
    private void ClearAll()
    {
        Completion.TrySetResult(ClearLogsAction.ClearAll);
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void KeepRecent()
    {
        Completion.TrySetResult(ClearLogsAction.KeepRecent);
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        Completion.TrySetResult(ClearLogsAction.None);
        RequestClose?.Invoke();
    }
}
