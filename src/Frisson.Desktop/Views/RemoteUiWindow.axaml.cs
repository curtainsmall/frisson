using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

using Frisson.Desktop.ViewModels;

namespace Frisson.Desktop.Views;

public partial class RemoteUiWindow : Window
{
    private bool _isPinned;

    private static readonly IBrush UnpinnedBrush = new SolidColorBrush(Color.FromRgb(0xa0, 0xa0, 0xa0));
    private static readonly IBrush PinnedBrush = new SolidColorBrush(Color.FromRgb(0xe8, 0xd4, 0xa2));

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

    private void OnPinToggle(object? sender, RoutedEventArgs e)
    {
        _isPinned = !_isPinned;
        Topmost = _isPinned;
        PinIcon.Foreground = _isPinned ? PinnedBrush : UnpinnedBrush;
    }
}
