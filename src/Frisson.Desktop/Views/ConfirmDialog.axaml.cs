using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Frisson.Desktop.ViewModels;

namespace Frisson.Desktop.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public ConfirmDialog(ConfirmDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.RequestClose += Close;
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }
}
