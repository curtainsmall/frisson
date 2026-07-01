using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;

using Frisson.Desktop.ViewModels;
using Frisson.Core;

namespace Frisson.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.Default;
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }

    private void OnAgentCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not ConnectedAgentCard card)
            return;

        // Update selection
        if (DataContext is MainWindowViewModel vm)
            vm.SelectedCard = card;
    }

    private void OnDeviceCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not ConnectedAgentCard card)
            return;

        // Toggle expansion for device cards
        card.IsExpanded = !card.IsExpanded;
    }

    private void OnAgentCardDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not ConnectedAgentCard card)
            return;

        if (DataContext is MainWindowViewModel vm)
            vm.ToggleActiveSourceCommand.Execute(card.AgentId);
    }

    // === Control Desk strength control handlers ===

    private ControlDeskViewModel? GetControlDeskVM()
    {
        return (DataContext as MainWindowViewModel)?.ControlDeskViewModel;
    }

    private void OnChannelAWheel(object? sender, PointerWheelEventArgs e)
    {
        if (sender is Control ctrl)
            ToolTip.SetIsOpen(ctrl, false);

        var vm = GetControlDeskVM();
        if (vm == null) return;
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        vm.OnScrollA(e.Delta.Y > 0 ? 1 : -1, shift);
    }

    private void OnChannelAIncrease(object? sender, PointerPressedEventArgs e)
    {
        var vm = GetControlDeskVM();
        if (vm == null) return;
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        vm.AdjustA(shift ? 10 : 1);
    }

    private void OnChannelADecrease(object? sender, PointerPressedEventArgs e)
    {
        var vm = GetControlDeskVM();
        if (vm == null) return;
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        vm.AdjustA(shift ? -10 : -1);
    }

    private void OnChannelBWheel(object? sender, PointerWheelEventArgs e)
    {
        if (sender is Control ctrl)
            ToolTip.SetIsOpen(ctrl, false);

        var vm = GetControlDeskVM();
        if (vm == null) return;
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        vm.OnScrollB(e.Delta.Y > 0 ? 1 : -1, shift);
    }

    private void OnChannelBIncrease(object? sender, PointerPressedEventArgs e)
    {
        var vm = GetControlDeskVM();
        if (vm == null) return;
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        vm.AdjustB(shift ? 10 : 1);
    }

    private void OnChannelBDecrease(object? sender, PointerPressedEventArgs e)
    {
        var vm = GetControlDeskVM();
        if (vm == null) return;
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        vm.AdjustB(shift ? -10 : -1);
    }
}
