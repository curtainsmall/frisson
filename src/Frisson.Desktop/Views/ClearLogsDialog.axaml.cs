using Avalonia.Controls;
using Avalonia.Input;

using Frisson.Desktop.ViewModels;

namespace Frisson.Desktop.Views;

public partial class ClearLogsDialog : Window
{
    public ClearLogsDialog()
    {
        InitializeComponent();
    }

    public ClearLogsDialog(ClearLogsDialogViewModel viewModel) : this()
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
