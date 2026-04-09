using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CoyoteStudio.App.ViewModels;

public enum NavPage
{
    Mixer,
    Waves,
    Settings,
}

/// <summary>
/// Represents a connected client information for UI display.
/// </summary>
public class ConnectedClientInfo
{
    public required string ClientId { get; init; }
    public required string ClientType { get; init; }
    public required string Status { get; init; }
    public required string StatusColor { get; init; }
}

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<string> PlaceholderItems { get; } = new ObservableCollection<string>
    {
        "Item 1",
        "Item 2",
        "Item 3"
    };

    /// <summary>
    /// List of all connected clients for display in Mixer page.
    /// </summary>
    public ObservableCollection<ConnectedClientInfo> ConnectedClients { get; } = new();

    [ObservableProperty]
    private NavPage _currentPage = NavPage.Mixer;

    public bool IsMixerSelected => CurrentPage == NavPage.Mixer;
    public bool IsWavesSelected => CurrentPage == NavPage.Waves;
    public bool IsSettingsSelected => CurrentPage == NavPage.Settings;

    partial void OnCurrentPageChanged(NavPage value)
    {
        OnPropertyChanged(nameof(IsMixerSelected));
        OnPropertyChanged(nameof(IsWavesSelected));
        OnPropertyChanged(nameof(IsSettingsSelected));
    }

    [RelayCommand]
    private void SelectPage(NavPage page)
    {
        CurrentPage = page;
    }

    public MainWindowViewModel()
    {
        AddPlaceholderClients();
    }

    /// <summary>
    /// Adds placeholder clients for UI testing.
    /// </summary>
    private void AddPlaceholderClients()
    {
        ConnectedClients.Add(new ConnectedClientInfo
        {
            ClientId = "550e8400-e29b-41d4-a716-446655440001",
            ClientType = "Device",
            Status = "Connected",
            StatusColor = "#00FF00"
        });
        ConnectedClients.Add(new ConnectedClientInfo
        {
            ClientId = "550e8400-e29b-41d4-a716-446655440002",
            ClientType = "Remote",
            Status = "Bound",
            StatusColor = "#FFD700"
        });
        ConnectedClients.Add(new ConnectedClientInfo
        {
            ClientId = "550e8400-e29b-41d4-a716-446655440003",
            ClientType = "WebSocketClient",
            Status = "Pending",
            StatusColor = "#888888"
        });
    }
}