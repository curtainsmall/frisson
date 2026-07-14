using System;

using CommunityToolkit.Mvvm.ComponentModel;

using Frisson.Core;

namespace Frisson.Desktop.ViewModels;

public partial class ControlDeskViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _strengthA;

    [ObservableProperty]
    private int _strengthB;

    [ObservableProperty]
    private int _maxA;

    [ObservableProperty]
    private int _maxB;

    [ObservableProperty]
    private bool _isBlocked;

    [ObservableProperty]
    private string? _activeRemoteName;

    public ControlDeskViewModel()
    {
        AppCore.Instance.ControlDeskStateChanged += OnControlDeskStateChanged;
        SyncFromControlDesk();
    }

    private void SyncFromControlDesk()
    {
        StrengthA = AppCore.Instance.GetControlDeskStrengthA();
        StrengthB = AppCore.Instance.GetControlDeskStrengthB();
        MaxA = AppCore.Instance.GetControlDeskMaxA();
        MaxB = AppCore.Instance.GetControlDeskMaxB();
        IsBlocked = AppCore.Instance.GetControlDeskIsBlocked();
        ActiveRemoteName = AppCore.Instance.GetActiveRemoteName();
    }

    private void OnControlDeskStateChanged()
    {
        SyncFromControlDesk();
    }

    public void AdjustA(int step) =>
        AppCore.Instance.SetControlDeskStrength(StrengthA + step, StrengthB);

    public void AdjustB(int step) =>
        AppCore.Instance.SetControlDeskStrength(StrengthA, StrengthB + step);

    public void OnScrollA(int delta, bool shift) =>
        AdjustA(shift ? 10 * Math.Sign(delta) : Math.Sign(delta));

    public void OnScrollB(int delta, bool shift) =>
        AdjustB(shift ? 10 * Math.Sign(delta) : Math.Sign(delta));

    public void SetMaxA(int value)
    {
        AppCore.Instance.SetControlDeskMax(value, MaxB);
    }

    public void SetMaxB(int value)
    {
        AppCore.Instance.SetControlDeskMax(MaxA, value);
    }
}
