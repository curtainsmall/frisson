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
    private int _maxA = 200;

    [ObservableProperty]
    private int _maxB = 200;

    public ControlDeskViewModel()
    {
        AppCore.Instance.ControlDeskStateChanged += OnControlDeskStateChanged;
        SyncFromControlDesk();
    }

    private void SyncFromControlDesk()
    {
        StrengthA = AppCore.Instance.GetControlDeskStrengthA();
        StrengthB = AppCore.Instance.GetControlDeskStrengthB();
    }

    private void OnControlDeskStateChanged()
    {
        SyncFromControlDesk();
    }

    public void AdjustA(int step) =>
        AppCore.Instance.SetControlDeskStrength(Math.Clamp(StrengthA + step, 0, MaxA), StrengthB);

    public void AdjustB(int step) =>
        AppCore.Instance.SetControlDeskStrength(StrengthA, Math.Clamp(StrengthB + step, 0, MaxB));

    public void OnScrollA(int delta, bool shift) =>
        AdjustA(shift ? 10 * Math.Sign(delta) : Math.Sign(delta));

    public void OnScrollB(int delta, bool shift) =>
        AdjustB(shift ? 10 * Math.Sign(delta) : Math.Sign(delta));
}
