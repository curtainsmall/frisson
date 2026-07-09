using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Styling;
using Frisson.Desktop.ViewModels;
using Frisson.Core;
namespace Frisson.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.Default;
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }

    private void OnMainGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Click outside TextBox → clear focus to trigger LostFocus
        if (e.Source is not TextBox && e.Source is not Border { Child: TextBox })
        {
            TopLevel.GetTopLevel(this)?.FocusManager?.ClearFocus();
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

    private void OnActuatorCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not ConnectedAgentCard card)
            return;

        // Toggle expansion for actuator cards
        card.IsExpanded = !card.IsExpanded;
    }

    private void OnAgentCardDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not ConnectedAgentCard card)
            return;

        if (DataContext is MainWindowViewModel vm)
            vm.ToggleActiveRemoteCommand.Execute(card.AgentId);
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

    // === Max value inline editing (Settings page) ===

    private void OnMaxALostFocus(object? sender, RoutedEventArgs e)
    {
        var vm = (DataContext as MainWindowViewModel);
        if (vm == null) return;
        vm.CommitMaxA();
    }

    private void OnMaxAKeyDown(object? sender, KeyEventArgs e)
    {
        var vm = (DataContext as MainWindowViewModel);
        if (vm == null) return;
        if (e.Key == Key.Enter)
            vm.CommitMaxA();
        else if (e.Key == Key.Escape)
            vm.CancelMaxA();
    }

    private void OnMaxBLostFocus(object? sender, RoutedEventArgs e)
    {
        var vm = (DataContext as MainWindowViewModel);
        if (vm == null) return;
        vm.CommitMaxB();
    }

    private void OnBlockedOverlayClick(object? sender, PointerPressedEventArgs e)
    {
        // Eat the event — overlay blocks all interaction
        e.Handled = true;
    }

    private void OnMaxBKeyDown(object? sender, KeyEventArgs e)
    {
        var vm = (DataContext as MainWindowViewModel);
        if (vm == null) return;
        if (e.Key == Key.Enter)
            vm.CommitMaxB();
        else if (e.Key == Key.Escape)
            vm.CancelMaxB();
    }

    // === Settings section fast-jump handlers ===

    private void OnGeneralSectionClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.SelectedSettingsSection = SettingsSection.General;
        ScrollToSection(GeneralSection);
    }

    private void OnActuatorSectionClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.SelectedSettingsSection = SettingsSection.Actuator;
        ScrollToSection(ActuatorSection);
    }

    private void OnSupportSectionClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.SelectedSettingsSection = SettingsSection.Support;
        ScrollToSection(SupportSection);
    }

    private void OnAboutSectionClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.SelectedSettingsSection = SettingsSection.About;
        ScrollToSection(AboutSection);
    }

    private void ScrollToSection(Border section)
    {
        var transform = section.TransformToVisual(SettingsScrollViewer);
        if (transform.HasValue)
        {
            var point = transform.Value.Transform(new Point(0, 0));
            SettingsScrollViewer.Offset = new Vector(0, point.Y);
        }

        var blinkColor = Color.FromArgb(90, 232, 212, 162);
        var clearColor = Color.FromArgb(0, 232, 212, 162);

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(1100),
            FillMode = FillMode.None,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    KeySpline = new KeySpline(0, 0, 1, 1),
                    Setters = { new Setter(Border.BackgroundProperty, new SolidColorBrush(clearColor)) }
                },
                new KeyFrame
                {
                    Cue = new Cue(0.175),
                    KeySpline = new KeySpline(0, 0, 1, 1),
                    Setters = { new Setter(Border.BackgroundProperty, new SolidColorBrush(blinkColor)) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    KeySpline = new KeySpline(0.4, 0, 0.2, 1),
                    Setters = { new Setter(Border.BackgroundProperty, new SolidColorBrush(clearColor)) }
                }
            }
        };
        _ = animation.RunAsync(section);
    }
}
