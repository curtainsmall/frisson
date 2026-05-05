using Avalonia.Controls;
using Avalonia.Input;

using CoyoteStudio.App.ViewModels;

namespace CoyoteStudio.App.Views;

public partial class LogWindow : Window
{
    public LogWindow()
    {
        InitializeComponent();
        DataContext = new LogWindowViewModel();
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
