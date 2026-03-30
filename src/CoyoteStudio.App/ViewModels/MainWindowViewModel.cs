using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CoyoteStudio.App.ViewModels;

public enum NavPage
{
    // Top
    Home,

    Waves,
    Settings,

    // Bottom
    Help,
}

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<string> PlaceholderItems { get; } = new ObservableCollection<string>
    {
        "Item 1",
        "Item 2",
        "Item 3"
    };

    [ObservableProperty]
    private NavPage _currentPage = NavPage.Home;

    public bool IsHomeSelected => CurrentPage == NavPage.Home;
    public bool IsWavesSelected => CurrentPage == NavPage.Waves;
    public bool IsSettingsSelected => CurrentPage == NavPage.Settings;
    public bool IsHelpSelected => CurrentPage == NavPage.Help;

    partial void OnCurrentPageChanged(NavPage value)
    {
        OnPropertyChanged(nameof(IsHomeSelected));
        OnPropertyChanged(nameof(IsWavesSelected));
        OnPropertyChanged(nameof(IsSettingsSelected));
        OnPropertyChanged(nameof(IsHelpSelected));
    }

    [RelayCommand]
    private void SelectPage(NavPage page)
    {
        CurrentPage = page;
    }

    public MainWindowViewModel()
    {
    }
}