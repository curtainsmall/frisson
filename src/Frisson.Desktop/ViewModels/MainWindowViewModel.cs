using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Frisson.Desktop.Services;
using Frisson.Core;
using Frisson.Core.Agent;
using Frisson.Core.Agent.Remote;
using Frisson.Core.Agent.Actuator;

namespace Frisson.Desktop.ViewModels;

public enum NavPage
{
    ControlDesk,
    Remotes,
    Settings,
}

public class LanguageOption
{
    public required string Code { get; init; }
    public required string DisplayName { get; init; }
}

public class ConnectedAgentCard : INotifyPropertyChanged
{
    public Guid AgentId { get; init; }
    public Type AgentType { get; init; } = typeof(Agent);
    public string DisplayName { get; set; } = "";

    public bool IsActuator => AgentType == typeof(ActuatorAgent);
    public bool IsRemote => AgentType == typeof(RemoteAgent);

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value)
            {
                _isActive = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
            }
        }
    }

    private bool _isActiveSource;
    public bool IsActiveSource
    {
        get => _isActiveSource;
        set
        {
            if (_isActiveSource != value)
            {
                _isActiveSource = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActiveSource)));
            }
        }
    }

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

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            }
        }
    }

    // --- Actuator state (updated from ActuatorAgent.StateUpdated) ---

    private int _strengthA;
    public int StrengthA
    {
        get => _strengthA;
        set { if (_strengthA != value) { _strengthA = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StrengthA))); } }
    }

    private int _strengthB;
    public int StrengthB
    {
        get => _strengthB;
        set { if (_strengthB != value) { _strengthB = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StrengthB))); } }
    }

    private int _maxA;
    public int MaxA
    {
        get => _maxA;
        set { if (_maxA != value) { _maxA = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxA))); } }
    }

    private int _maxB;
    public int MaxB
    {
        get => _maxB;
        set { if (_maxB != value) { _maxB = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxB))); } }
    }

    private int _waveQueueCount;
    public int WaveQueueCount
    {
        get => _waveQueueCount;
        set { if (_waveQueueCount != value) { _waveQueueCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WaveQueueCount))); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void NotifyLayoutChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActuator)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRemote)));
    }
}

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<LanguageOption> AvailableLanguages { get; } = new()
    {
        new LanguageOption { Code = "en-US", DisplayName = "English" },
        new LanguageOption { Code = "zh-CN", DisplayName = "简体中文" },
        new LanguageOption { Code = "zh-TW", DisplayName = "繁體中文" },
        new LanguageOption { Code = "ja-JP", DisplayName = "日本語" }
    };

    /// <summary>All connected agents (for internal tracking).</summary>
    public ObservableCollection<ConnectedAgentCard> AgentCards { get; } = new();

    /// <summary>Only Actuator agents — shown in the global right panel.</summary>
    public IEnumerable<ConnectedAgentCard> ActuatorCards => AgentCards.Where(c => c.IsActuator);

    /// <summary>Only Remote agents — shown on the Remotes page.</summary>
    public IEnumerable<ConnectedAgentCard> RemoteCards => AgentCards.Where(c => c.IsRemote);

    [ObservableProperty]
    private ConnectedAgentCard? _selectedCard;

    public bool HasSelectedCard => SelectedCard != null;
    public bool HasNoSelectedCard => SelectedCard == null;

    partial void OnSelectedCardChanged(ConnectedAgentCard? value)
    {
        OnPropertyChanged(nameof(HasSelectedCard));
        OnPropertyChanged(nameof(HasNoSelectedCard));
    }

    [ObservableProperty]
    private NavPage _currentPage = NavPage.ControlDesk;

    [ObservableProperty]
    private LanguageOption _selectedLanguage;

    public ControlDeskViewModel ControlDeskViewModel { get; } = new();

    private Window? _qrCodeWindow;

    public bool IsControlDeskSelected => CurrentPage == NavPage.ControlDesk;
    public bool IsRemotesSelected => CurrentPage == NavPage.Remotes;
    public bool IsSettingsSelected => CurrentPage == NavPage.Settings;
    public bool HasAgents => AgentCards.Count > 0;

    partial void OnCurrentPageChanged(NavPage value)
    {
        OnPropertyChanged(nameof(IsControlDeskSelected));
        OnPropertyChanged(nameof(IsRemotesSelected));
        OnPropertyChanged(nameof(IsSettingsSelected));
    }

    [RelayCommand]
    private void SelectPage(NavPage page) => CurrentPage = page;

    [RelayCommand]
    private void SelectAgent(Guid agentId)
    {
        var card = AgentCards.FirstOrDefault(c => c.AgentId == agentId);
        if (card != null)
            SelectedCard = card;
    }

    [RelayCommand]
    private async Task DisconnectAgent(Guid agentId)
    {
        var card = AgentCards.FirstOrDefault(c => c.AgentId == agentId);
        if (card == null) return;

        var title = LocalizationService.Instance["ConfirmDisconnectTitle"];
        var message = card.IsActuator
            ? LocalizationService.Instance["ConfirmDisconnectActuatorMsg"]
            : LocalizationService.Instance["ConfirmDisconnectRemoteMsg"];

        var confirmed = await ShowConfirmDialogAsync(title, message);
        if (confirmed)
            AppCore.Instance.DisconnectAgent(agentId);
    }

    [RelayCommand]
    private void ShowLogWindow()
    {
        var window = new Views.LogWindow();
        if (Desktop.MainWindow is not null)
            window.Show(Desktop.MainWindow);
    }

    [RelayCommand]
    private void ClearLogs() => LoggerService.Instance.Clear();

    private static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        }
        return "127.0.0.1";
    }

    [RelayCommand]
    private void ShowQrCodeWindow()
    {
        var lanIP = GetLocalIPAddress();
        var clientId = AppCore.DummyFrontendId;
        var qrContent = $"https://www.dungeon-lab.com/app-download.php#DGLAB-SOCKET#ws://{lanIP}:6969/{clientId}";
        _qrCodeWindow = new Views.QrCodeWindow(qrContent);
        if (Desktop.MainWindow is not null)
            _qrCodeWindow.Show(Desktop.MainWindow);
    }

    public MainWindowViewModel()
    {
        var currentCulture = LocalizationService.Instance.CurrentCulture.Name;
        var defaultLang = AvailableLanguages.FirstOrDefault(l => l.Code == currentCulture)
                          ?? AvailableLanguages.First(l => l.Code == "en-US");
        _selectedLanguage = defaultLang;
        LocalizationService.Instance.SetLanguage(defaultLang.Code);

        AppCore.Instance.AgentConnected += OnAgentConnected;
        AppCore.Instance.AgentClosing += OnAgentClosing;
        AppCore.Instance.SourceActivated += OnSourceActivated;
        AppCore.Instance.SourceDeactivated += OnSourceDeactivated;
        AppCore.Instance.ActuatorStateUpdated += OnActuatorStateUpdated;
        AppCore.Instance.RemoteBindingRequested += OnRemoteBindingRequested;

        AgentCards.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasAgents));
            OnPropertyChanged(nameof(ActuatorCards));
            OnPropertyChanged(nameof(RemoteCards));
        };
    }

    private Guid? _activeSourceId;

    private void OnSourceActivated(Guid agentId)
    {
        _activeSourceId = agentId;
        foreach (var card in AgentCards)
            card.IsActiveSource = card.AgentId == agentId;
    }

    private void OnSourceDeactivated()
    {
        _activeSourceId = null;
        foreach (var card in AgentCards)
            card.IsActiveSource = false;
    }

    private void OnActuatorStateUpdated(Guid agentId)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var agent = AppCore.Instance.GetAgent(agentId);
            if (agent is ActuatorAgent da)
            {
                var card = AgentCards.FirstOrDefault(c => c.AgentId == agentId);
                if (card != null)
                {
                    card.StrengthA = da.StrengthA;
                    card.StrengthB = da.StrengthB;
                    card.MaxA = da.MaxA;
                    card.MaxB = da.MaxB;
                }
            }
        });
    }

    [RelayCommand]
    private void ToggleActiveSource(Guid agentId)
    {
        if (_activeSourceId == agentId)
            AppCore.Instance.ClearActiveSource();
        else
        {
            AppCore.Instance.ClearActiveSource();
            AppCore.Instance.SetActiveSource(agentId);
        }
    }

    private void OnAgentConnected(object? sender, AgentEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var agent = AppCore.Instance.GetAgent(e.AgentId);
            var card = new ConnectedAgentCard
            {
                AgentId = e.AgentId,
                AgentType = e.AgentType,
                DisplayName = (agent as RemoteAgent)?.Name
                              ?? (agent as ActuatorAgent)?.Id.ToString("N")[..8].ToUpper()
                              ?? e.AgentId.ToString()
            };
            AgentCards.Add(card);

            // Auto-close QR window when an Actuator connects
            if (e.AgentType == typeof(RemoteAgent) && _qrCodeWindow is not null)
            {
                _qrCodeWindow.Close();
                _qrCodeWindow = null;
            }
        });
    }

    private void OnAgentClosing(object? sender, AgentEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var card = AgentCards.FirstOrDefault(c => c.AgentId == e.AgentId);
            if (card != null)
            {
                AgentCards.Remove(card);
                if (SelectedCard == card)
                    SelectedCard = null;
            }
        });
    }

    private async Task<bool> ShowConfirmDialogAsync(string title, string message)
    {
        var vm = new ConfirmDialogViewModel
        {
            Title = title,
            Message = message,
            ConfirmText = LocalizationService.Instance["ConfirmYes"],
            CancelText = LocalizationService.Instance["ConfirmNo"]
        };
        var dialog = new Views.ConfirmDialog(vm);
        if (Desktop.MainWindow is not null)
            await dialog.ShowDialog<bool>(Desktop.MainWindow);
        return await vm.Completion.Task;
    }

    private void OnRemoteBindingRequested(Guid clientId, string name)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            var vm = new ConfirmConnectDialogViewModel
            {
                Title = LocalizationService.Instance["ConfirmConnectTitle"],
                Name = name,
                Id = clientId.ToString(),
                Message = LocalizationService.Instance["ConfirmConnectMsg"],
                Warning = LocalizationService.Instance["ConfirmConnectWarning"],
                ConfirmText = LocalizationService.Instance["ConfirmYes"],
                CancelText = LocalizationService.Instance["ConfirmNo"]
            };
            var dialog = new Views.ConfirmConnectDialog(vm);
            if (Desktop.MainWindow is not null)
                await dialog.ShowDialog<bool>(Desktop.MainWindow);
            var confirmed = await vm.Completion.Task;
            if (confirmed)
                AppCore.Instance.AcceptRemote(clientId);
            else
                AppCore.Instance.RejectRemote(clientId);
        });
    }

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        if (value != null)
            LocalizationService.Instance.SetLanguage(value.Code);
    }
}
