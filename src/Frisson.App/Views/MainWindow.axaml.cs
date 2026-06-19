using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;

using Frisson.App.ViewModels;
using Frisson.Core;

namespace Frisson.App.Views;

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
}
