using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Frisson.Desktop.ViewModels;

namespace Frisson.Desktop.Views;

public partial class QrCodeWindow : Window
{
    public QrCodeWindow()
    {
        InitializeComponent();
    }

    public QrCodeWindow(string qrContent) : this()
    {
        DataContext = new QrCodeWindowViewModel(qrContent);
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
