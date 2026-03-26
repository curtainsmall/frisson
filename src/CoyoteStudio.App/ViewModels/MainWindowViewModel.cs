using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CoyoteStudio.Shared;

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
    public ObservableCollection<ClientDto> ConnectedClients { get; } = new();
    public string Greeting { get; } = "Welcome to Avalonia!";

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
        ConnectedClients.Add(new ClientDto { ClientName = "localhost" });
    }
}