using Avalonia.Controls;
using Avalonia.Input;

using Frisson.Desktop.ViewModels;

namespace Frisson.Desktop.Views;

public partial class RemoteUiWindow : Window
{
    public RemoteUiWindow()
    {
        InitializeComponent();
    }

    public RemoteUiWindow(ConnectedAgentCard card) : this()
    {
        DataContext = card;
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
