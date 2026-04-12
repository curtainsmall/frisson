using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CoyoteStudio.App.Services;

namespace CoyoteStudio.App.ViewModels;

public enum NavPage
{
    Mixer,
    Waves,
    Settings,
}

/// <summary>
/// Represents a language option for the language selector.
/// </summary>
public class LanguageOption
{
    public required string Code { get; init; }
    public required string DisplayName { get; init; }
}

/// <summary>
/// Represents a connected client information for UI display.
/// Uses localization keys for ClientType and Status to support dynamic language switching.
/// </summary>
public class ConnectedClientInfo
{
    public required string ClientId { get; init; }
    /// <summary>
    /// Localization key for client type (e.g., "ClientTypeDevice", "ClientTypeRemote", "ClientTypeUnknown").
    /// </summary>
    public required string ClientTypeKey { get; init; }
    /// <summary>
    /// Localization key for status (e.g., "StatusConnected", "StatusBound", "StatusPending").
    /// </summary>
    public required string StatusKey { get; init; }
    public required string StatusColor { get; init; }
}

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<string> PlaceholderItems { get; } = new ObservableCollection<string>
    {
        LocalizationService.Instance.GetString("MenuItem1"),
        LocalizationService.Instance.GetString("MenuItem2"),
        LocalizationService.Instance.GetString("MenuItem3")
    };

    /// <summary>
    /// List of all connected clients for display in Mixer page.
    /// </summary>
    public ObservableCollection<ConnectedClientInfo> ConnectedClients { get; } = new();

    /// <summary>
    /// Available languages for selection.
    /// </summary>
    public ObservableCollection<LanguageOption> AvailableLanguages { get; } = new()
    {
        new LanguageOption { Code = "en-US", DisplayName = "English" },
        new LanguageOption { Code = "zh-CN", DisplayName = "中文" }
    };

    [ObservableProperty]
    private NavPage _currentPage = NavPage.Mixer;

    [ObservableProperty]
    private LanguageOption _selectedLanguage;

    [ObservableProperty]
    private bool _isClientPanelExpanded = true;

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

    [RelayCommand]
    private void ToggleClientPanel()
    {
        IsClientPanelExpanded = !IsClientPanelExpanded;
    }

    public MainWindowViewModel()
    {
        // Set default language to en-US
        _selectedLanguage = AvailableLanguages.First(l => l.Code == "en-US");
        LocalizationService.Instance.SetLanguage("en-US");

        AddPlaceholderClients();
    }

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        if (value != null)
        {
            LocalizationService.Instance.SetLanguage(value.Code);
        }
    }

    /// <summary>
    /// Adds placeholder clients for UI testing.
    /// </summary>
    private void AddPlaceholderClients()
    {
        ConnectedClients.Add(new ConnectedClientInfo
        {
            ClientId = "550e8400-e29b-41d4-a716-446655440001",
            ClientTypeKey = "ClientTypeDevice",
            StatusKey = "StatusConnected",
            StatusColor = "#00FF00"
        });
        ConnectedClients.Add(new ConnectedClientInfo
        {
            ClientId = "550e8400-e29b-41d4-a716-446655440002",
            ClientTypeKey = "ClientTypeRemote",
            StatusKey = "StatusBound",
            StatusColor = "#FFD700"
        });
        ConnectedClients.Add(new ConnectedClientInfo
        {
            ClientId = "550e8400-e29b-41d4-a716-446655440003",
            ClientTypeKey = "ClientTypeUnknown",
            StatusKey = "StatusPending",
            StatusColor = "#888888"
        });
    }
}