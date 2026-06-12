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

    private async void OnAgentCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not ConnectedAgentCard card)
            return;

        // Update selection
        if (DataContext is MainWindowViewModel vm)
            vm.SelectedCard = card;

        // Only device cards can be dragged
        if (!card.IsDevice)
            return;

        var point = e.GetCurrentPoint(border);
        if (!point.Properties.IsLeftButtonPressed)
            return;

        var dataObject = new DataObject();
        dataObject.Set("DeviceAgentId", card.AgentId.ToString());
        await DragDrop.DoDragDrop(e, dataObject, DragDropEffects.Move);
    }

    private void OnAgentCardDragOver(object? sender, DragEventArgs e)
    {
        if (sender is Border { DataContext: ConnectedAgentCard card } && card.IsControl)
        {
            e.DragEffects = DragDropEffects.Move;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnAgentCardDrop(object? sender, DragEventArgs e)
    {
        if (sender is not Border { DataContext: ConnectedAgentCard targetCard } || !targetCard.IsControl)
            return;

        var deviceIdStr = e.Data.Get("DeviceAgentId") as string;
        if (deviceIdStr == null || !Guid.TryParse(deviceIdStr, out var deviceId))
            return;

        AppCore.Instance.LinkAgents(deviceId, targetCard.AgentId);
    }
}
