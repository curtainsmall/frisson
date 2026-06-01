using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Frisson.App.Services;
using Frisson.Core;
using Frisson.Core.Networking.Client;
using Frisson.Core.Networking.Server;

namespace Frisson.App.ViewModels;

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
/// Uses localization keys for ClientType to support dynamic language switching.
/// </summary>
public class ConnectedClientInfo : INotifyPropertyChanged
{
    public required Guid ClientId { get; init; }
    /// <summary>
    /// Type of the connected client.
    /// </summary>
    public required WebSocketClientKind ClientType { get; init; }
    /// <summary>
    /// Connection status of the client.
    /// </summary>
    public required WebClientConnectionStatus Status { get; init; }
    public required string StatusColor { get; init; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// List of all connected clients for display in Control Panel page.
    /// </summary>
    public ObservableCollection<ConnectedClientInfo> ConnectedClients { get; } = new();

    /// <summary>
    /// List of device-type clients for selection.
    /// </summary>
    public ObservableCollection<ConnectedClientInfo> DeviceClients => new(
        ConnectedClients.Where(c => c.ClientType == WebSocketClientKind.Device));

    /// <summary>
    /// Count of active clients (Device + Remote).
    /// </summary>
    public int ActiveClientCount => ConnectedClients.Count(c => c.ClientType is WebSocketClientKind.Device or WebSocketClientKind.Remote);

    /// <summary>
    /// Count of unknown clients.
    /// </summary>
    public int UnknownClientCount => ConnectedClients.Count(c => c.ClientType == WebSocketClientKind.Unknown);

    /// <summary>
    /// Whether there are any unknown clients (for visibility binding).
    /// </summary>
    public bool HasUnknownClients => UnknownClientCount > 0;

    /// <summary>
    /// Whether there are any device clients connected.
    /// </summary>
    public bool HasDeviceClients => ConnectedClients.Any(c => c.ClientType == WebSocketClientKind.Device);

    /// <summary>
    /// Whether a device is currently selected for control.
    /// </summary>
    public bool HasSelectedDevice => SelectedDeviceClientId.HasValue;

    /// <summary>
    /// Show 'No Device Selected' panel when no device is selected.
    /// </summary>
    public bool ShowNoDevicePanel => !HasSelectedDevice;

    /// <summary>
    /// Show channel controls when a device is selected.
    /// </summary>
    public bool ShowChannelControls => HasSelectedDevice;

    /// <summary>
    /// Available languages for selection.
    /// </summary>
    public ObservableCollection<LanguageOption> AvailableLanguages { get; } = new()
    {
        new LanguageOption { Code = "en-US", DisplayName = "English" },
        new LanguageOption { Code = "zh-CN", DisplayName = "简体中文" },
        new LanguageOption { Code = "zh-TW", DisplayName = "繁體中文" },
        new LanguageOption { Code = "ja-JP", DisplayName = "日本語" }
    };

    [ObservableProperty]
    private NavPage _currentPage = NavPage.Mixer;

    [ObservableProperty]
    private LanguageOption _selectedLanguage;

    [ObservableProperty]
    private bool _isClientPanelExpanded = true;

    public bool IsClientPanelCollapsed => !IsClientPanelExpanded;

    /// <summary>
    /// Currently selected device client ID for Channel control.
    /// </summary>
    [ObservableProperty]
    private Guid? _selectedDeviceClientId;

    /// <summary>
    /// Channel A strength value from the selected device.
    /// </summary>
    public int ChannelAStrength => AppCore.Instance.GetDeviceChannelStrength(SelectedDeviceClientId, 'A');

    /// <summary>
    /// Channel A limit value from the selected device.
    /// </summary>
    public int ChannelALimit => AppCore.Instance.GetDeviceChannelLimit(SelectedDeviceClientId, 'A');

    /// <summary>
    /// Channel B strength value from the selected device.
    /// </summary>
    public int ChannelBStrength => AppCore.Instance.GetDeviceChannelStrength(SelectedDeviceClientId, 'B');

    /// <summary>
    /// Channel B limit value from the selected device.
    /// </summary>
    public int ChannelBLimit => AppCore.Instance.GetDeviceChannelLimit(SelectedDeviceClientId, 'B');

    public bool IsMixerSelected => CurrentPage == NavPage.Mixer;
    public bool IsWavesSelected => CurrentPage == NavPage.Waves;
    public bool IsSettingsSelected => CurrentPage == NavPage.Settings;

    partial void OnCurrentPageChanged(NavPage value)
    {
        OnPropertyChanged(nameof(IsMixerSelected));
        OnPropertyChanged(nameof(IsWavesSelected));
        OnPropertyChanged(nameof(IsSettingsSelected));
    }

    partial void OnSelectedDeviceClientIdChanged(Guid? value)
    {
        // Update selection highlight on client cards
        foreach (var client in ConnectedClients)
        {
            client.IsSelected = client.ClientId == value;
        }

        OnPropertyChanged(nameof(ChannelAStrength));
        OnPropertyChanged(nameof(ChannelALimit));
        OnPropertyChanged(nameof(ChannelBStrength));
        OnPropertyChanged(nameof(ChannelBLimit));
        OnPropertyChanged(nameof(HasSelectedDevice));
        OnPropertyChanged(nameof(ShowNoDevicePanel));
        OnPropertyChanged(nameof(ShowChannelControls));
    }

    partial void OnIsClientPanelExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsClientPanelCollapsed));
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

    [RelayCommand]
    private void SelectDevice(Guid clientId)
    {
        // Only allow selecting Device-type clients
        var client = ConnectedClients.FirstOrDefault(c => c.ClientId == clientId);
        if (client?.ClientType != WebSocketClientKind.Device)
            return;

        SelectedDeviceClientId = clientId;
    }

    [RelayCommand]
    private void ShowQrCode()
    {
        var qrContent = AppCore.Instance.GetQrCodeContent();
        var window = new Views.QrCodeWindow(qrContent);
        if (App.MainWindow is not null)
        {
            window.ShowDialog(App.MainWindow);
        }
    }

    [RelayCommand]
    private void ShowLogWindow()
    {
        var window = new Views.LogWindow();
        if (App.MainWindow is not null)
        {
            window.Show(App.MainWindow);
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        LoggerService.Instance.Clear();
    }

    [RelayCommand]
    private void DisconnectClient(Guid clientId)
    {
        AppCore.Instance.DisconnectClient(clientId);
        
        // Clear selected device if it was the disconnected one
        if (SelectedDeviceClientId == clientId)
        {
            SelectedDeviceClientId = null;
        }
    }

    [RelayCommand]
    private async Task IncreaseChannelA()
    {
        if (SelectedDeviceClientId.HasValue)
            await AppCore.Instance.SendStrengthStepAsync(SelectedDeviceClientId.Value, 1, 5);
    }

    [RelayCommand]
    private async Task DecreaseChannelA()
    {
        if (SelectedDeviceClientId.HasValue)
            await AppCore.Instance.SendStrengthStepAsync(SelectedDeviceClientId.Value, 1, -5);
    }

    [RelayCommand]
    private async Task IncreaseChannelB()
    {
        if (SelectedDeviceClientId.HasValue)
            await AppCore.Instance.SendStrengthStepAsync(SelectedDeviceClientId.Value, 2, 5);
    }

    [RelayCommand]
    private async Task DecreaseChannelB()
    {
        if (SelectedDeviceClientId.HasValue)
            await AppCore.Instance.SendStrengthStepAsync(SelectedDeviceClientId.Value, 2, -5);
    }

    /// <summary>
    /// Direct-set strength for Remote forwarding. Channel 1=A, 2=B.
    /// </summary>
    public async Task SetChannelStrengthAsync(int channel, int value)
    {
        if (SelectedDeviceClientId.HasValue)
            await AppCore.Instance.SendStrengthSetAsync(SelectedDeviceClientId.Value, channel, value);
    }

    public MainWindowViewModel()
    {
        // Set default language based on current culture
        var currentCulture = LocalizationService.Instance.CurrentCulture.Name;
        var defaultLang = AvailableLanguages.FirstOrDefault(l => l.Code == currentCulture) 
                          ?? AvailableLanguages.First(l => l.Code == "en-US");
        _selectedLanguage = defaultLang;
        
        // Sync to LocalizationService (already set in LocalizationService constructor)
        LocalizationService.Instance.SetLanguage(defaultLang.Code);

        ConnectedClients.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ActiveClientCount));
            OnPropertyChanged(nameof(UnknownClientCount));
            OnPropertyChanged(nameof(HasUnknownClients));
            OnPropertyChanged(nameof(HasDeviceClients));
            OnPropertyChanged(nameof(DeviceClients));
            OnPropertyChanged(nameof(ShowNoDevicePanel));
            OnPropertyChanged(nameof(ShowChannelControls));
        };

        AppCore.Instance.DeviceStateChanged += OnDeviceStateChanged;
        AppCore.Instance.ClientConnected += OnClientConnected;
        AppCore.Instance.ClientDisconnected += OnClientDisconnected;
    }

    private void OnDeviceStateChanged(object? sender, DeviceStateChangedEventArgs e)
    {
        if (e.DeviceId == SelectedDeviceClientId)
        {
            OnPropertyChanged(nameof(ChannelAStrength));
            OnPropertyChanged(nameof(ChannelALimit));
            OnPropertyChanged(nameof(ChannelBStrength));
            OnPropertyChanged(nameof(ChannelBLimit));
        }
    }

    private void OnClientConnected(object? sender, ClientConnectionEventArgs e)
    {
        var statusColor = e.Status switch
        {
            WebClientConnectionStatus.Connected => "#00FF00",
            _ => "#888888"
        };

        // Marshal to UI thread for ObservableCollection modification
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // Remove existing entry if present (e.g., reconnection)
            var existing = ConnectedClients.FirstOrDefault(c => c.ClientId == e.ClientId);
            if (existing is not null)
                ConnectedClients.Remove(existing);

            ConnectedClients.Add(new ConnectedClientInfo
            {
                ClientId = e.ClientId,
                ClientType = e.Kind,
                Status = e.Status,
                StatusColor = statusColor
            });
        });
    }

    private void OnClientDisconnected(object? sender, ClientConnectionEventArgs e)
    {
        // Marshal to UI thread for ObservableCollection modification
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var existing = ConnectedClients.FirstOrDefault(c => c.ClientId == e.ClientId);
            if (existing is not null)
            {
                ConnectedClients.Remove(existing);

                // Clear selected device if it was the disconnected one
                if (SelectedDeviceClientId == e.ClientId)
                {
                    SelectedDeviceClientId = null;
                }
            }
        });
    }

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        if (value != null)
        {
            LocalizationService.Instance.SetLanguage(value.Code);
        }
    }

}