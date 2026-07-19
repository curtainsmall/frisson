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
    Connections,
    Settings,
}

public enum SettingsSection
{
    General,
    Actuator,
    Support,
    About,
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

    private bool _isActiveRemote;
    public bool IsActiveRemote
    {
        get => _isActiveRemote;
        set
        {
            if (_isActiveRemote != value)
            {
                _isActiveRemote = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActiveRemote)));
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

    /// <summary>
    /// Remote UI grouped sections (null if Remote has no UI declaration).
    /// </summary>
    private ObservableCollection<RemoteUiSectionViewModel>? _remoteUiSections;
    public ObservableCollection<RemoteUiSectionViewModel>? RemoteUiSections
    {
        get => _remoteUiSections;
        set
        {
            if (_remoteUiSections != value)
            {
                _remoteUiSections = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RemoteUiSections)));
            }
        }
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
        new LanguageOption { Code = "zh-TW", DisplayName = "繁體中文" }
    };

    /// <summary>All connected agents (for internal tracking).</summary>
    public ObservableCollection<ConnectedAgentCard> AgentCards { get; } = new();

    /// <summary>Only Remote agents — shown on the Connections page.</summary>
    public IEnumerable<ConnectedAgentCard> RemoteCards => AgentCards.Where(c => c.IsRemote);
    public bool HasNoRemoteCards => !AgentCards.Any(c => c.IsRemote);

    [ObservableProperty]
    private ConnectedAgentCard? _selectedCard;

    public bool HasSelectedCard => SelectedCard != null;
    public bool HasNoSelectedCard => SelectedCard == null;

    /// <summary>Show inline Remote UI when the selected Remote has UI and floating mode is off.</summary>
    public bool ShowInlineRemoteUi => SelectedCard?.RemoteUiSections?.Count > 0 && !UseFloatingRemoteUi;

    partial void OnSelectedCardChanged(ConnectedAgentCard? value)
    {
        OnPropertyChanged(nameof(HasSelectedCard));
        OnPropertyChanged(nameof(HasNoSelectedCard));
        OnPropertyChanged(nameof(ShowInlineRemoteUi));
    }

    [ObservableProperty]
    private NavPage _currentPage = NavPage.ControlDesk;

    [ObservableProperty]
    private SettingsSection _selectedSettingsSection = SettingsSection.General;

    [ObservableProperty]
    private LanguageOption _selectedLanguage;

    public ControlDeskViewModel ControlDeskViewModel { get; } = new();

    private Window? _qrCodeWindow;
    private Window? _remoteUiWindow;

    public bool IsControlDeskSelected => CurrentPage == NavPage.ControlDesk;
    public bool IsConnectionsSelected => CurrentPage == NavPage.Connections;
    public bool IsSettingsSelected => CurrentPage == NavPage.Settings;
    public bool IsGeneralSelected => SelectedSettingsSection == SettingsSection.General;
    public bool IsActuatorSelected => SelectedSettingsSection == SettingsSection.Actuator;
    public bool IsSupportSelected => SelectedSettingsSection == SettingsSection.Support;
    public bool IsAboutSelected => SelectedSettingsSection == SettingsSection.About;
    public bool HasActuator => ActuatorCard != null;

    partial void OnCurrentPageChanged(NavPage value)
    {
        OnPropertyChanged(nameof(IsControlDeskSelected));
        OnPropertyChanged(nameof(IsConnectionsSelected));
        OnPropertyChanged(nameof(IsSettingsSelected));
    }

    partial void OnSelectedSettingsSectionChanged(SettingsSection value)
    {
        OnPropertyChanged(nameof(IsGeneralSelected));
        OnPropertyChanged(nameof(IsActuatorSelected));
        OnPropertyChanged(nameof(IsSupportSelected));
        OnPropertyChanged(nameof(IsAboutSelected));
    }

    [RelayCommand]
    private void SelectPage(NavPage page) => CurrentPage = page;

    [RelayCommand]
    private void SelectSettingsSection(SettingsSection section) => SelectedSettingsSection = section;

    [RelayCommand]
    private void SelectAgent(Guid agentId)
    {
        var card = AgentCards.FirstOrDefault(c => c.AgentId == agentId);
        if (card != null)
            SelectedCard = card;
    }

    [RelayCommand]
    private void SelectAgentAndNavigate(Guid agentId)
    {
        SelectAgent(agentId);
        if (CurrentPage == NavPage.ControlDesk)
            CurrentPage = NavPage.Connections;
    }

    [RelayCommand]
    private void NavigateToActiveRemote()
    {
        CurrentPage = NavPage.Connections;
        var id = AppCore.Instance.GetActiveRemoteId();
        if (id.HasValue)
            SelectAgent(id.Value);
    }

    private readonly HashSet<Guid> _intentionalDisconnects = new();

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
        {
            _intentionalDisconnects.Add(agentId);
            AppCore.Instance.DisconnectAgent(agentId);
        }
    }

    [RelayCommand]
    private void ShowLogWindow()
    {
        var window = new Views.LogWindow();
        if (Desktop.MainWindow is not null)
            window.Show(Desktop.MainWindow);
    }

    [RelayCommand]
    private void ShowDisclaimer()
    {
        var ls = LocalizationService.Instance;
        var vm = new DisclaimerDialogViewModel
        {
            Title = ls["DisclaimerTitle"],
            Body = ls["DisclaimerBody"],
            CloseText = ls["DisclaimerClose"]
        };
        var dialog = new Views.DisclaimerDialog(vm);
        if (Desktop.MainWindow is not null)
            dialog.ShowDialog(Desktop.MainWindow);
    }

    [RelayCommand]
    private async Task ClearLogs()
    {
        var vm = new ClearLogsDialogViewModel();
        var dialog = new Views.ClearLogsDialog(vm);
        if (Desktop.MainWindow is not null)
            await dialog.ShowDialog<ClearLogsAction>(Desktop.MainWindow);
        var action = await vm.Completion.Task;

        if (action == ClearLogsAction.ClearAll)
            LoggerService.Instance.Clear();
        else if (action == ClearLogsAction.KeepRecent)
            LoggerService.Instance.KeepRecent(10);
    }

    [RelayCommand]
    private void OpenLogDir()
    {
        var logDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Frisson", "logs");
        if (!System.IO.Directory.Exists(logDir))
            System.IO.Directory.CreateDirectory(logDir);
        using var _ = System.Diagnostics.Process.Start("explorer.exe", logDir);
    }

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
        // Use persisted language, or OS culture, or fallback to en-US
        var savedLang = SettingsService.Instance.TryGet("language", out string lang) ? lang : null;
        var currentCulture = savedLang ?? LocalizationService.Instance.CurrentCulture.Name;
        var defaultLang = AvailableLanguages.FirstOrDefault(l => l.Code == currentCulture)
                          ?? AvailableLanguages.First(l => l.Code == "en-US");
        _selectedLanguage = defaultLang;
        LocalizationService.Instance.SetLanguage(defaultLang.Code);

        AppCore.Instance.AgentConnected += OnAgentConnected;
        AppCore.Instance.AgentClosing += OnAgentClosing;
        AppCore.Instance.RemoteActivated += OnRemoteActivated;
        AppCore.Instance.RemoteDeactivated += OnRemoteDeactivated;
        AppCore.Instance.ActuatorStateUpdated += OnActuatorStateUpdated;
        AppCore.Instance.RemoteBindingRequested += OnRemoteBindingRequested;

        AgentCards.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(RemoteCards));
            OnPropertyChanged(nameof(HasNoRemoteCards));
        };
    }

    private ConnectedAgentCard? _actuatorCard;
    public ConnectedAgentCard? ActuatorCard
    {
        get => _actuatorCard;
        set
        {
            if (_actuatorCard != value)
            {
                _actuatorCard = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasActuator));
            }
        }
    }

    private Guid? _activeRemoteId;

    private void OnRemoteActivated(Guid agentId)
    {
        _activeRemoteId = agentId;
        foreach (var card in AgentCards)
        {
            var isActive = card.AgentId == agentId;
            card.IsActiveRemote = isActive;
            SetCardUiEnabled(card, isActive);
        }
    }

    private void OnRemoteDeactivated()
    {
        _activeRemoteId = null;
        foreach (var card in AgentCards)
        {
            card.IsActiveRemote = false;
            SetCardUiEnabled(card, false);
        }
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
    private void ToggleActiveRemote(Guid agentId)
    {
        if (_activeRemoteId == agentId)
            AppCore.Instance.ClearActiveRemote();
        else
        {
            AppCore.Instance.ClearActiveRemote();
            AppCore.Instance.SetActiveRemote(agentId);
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

            // Wire up Remote UI items
            if (agent is RemoteAgent ra && ra.Ui != null)
            {
                var sections = BuildUiSections(ra.Ui, (key, val) => ra.SendUiValue(key, val));
                card.RemoteUiSections = sections;
                // New Remotes start inactive — disable their UI
                Avalonia.Threading.Dispatcher.UIThread.Post(() => SetCardUiEnabled(card, false));

                // Subscribe to value updates from the Remote
                ra.UiValueChanged += (key, rawValue) =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        foreach (var section in sections)
                        {
                            var target = section.Items.FirstOrDefault(ivm => ivm.Key == key);
                            if (target != null)
                            {
                                target.UpdateFromRemote(rawValue);
                                break;
                            }
                        }
                    });
                };

                // Auto-open floating window if setting is already on
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (UseFloatingRemoteUi)
                        OpenRemoteUiFloatingWindow();
                });
            }

            AgentCards.Add(card);

            // Track the single actuator card
            if (card.IsActuator)
                ActuatorCard = card;

            // Auto-close QR window when a Remote connects
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
            var wasIntentional = _intentionalDisconnects.Remove(e.AgentId);
            var card = AgentCards.FirstOrDefault(c => c.AgentId == e.AgentId);
            if (card != null)
            {
                AgentCards.Remove(card);
                if (SelectedCard == card)
                    SelectedCard = null;
                if (card.IsActuator)
                {
                    ActuatorCard = null;
                    if (!wasIntentional)
                        ShowActuatorDisconnectedDialog();
                }
            }

            // Close floating window if the disconnected agent was being shown
            if (_remoteUiWindow != null && card != null && card.RemoteUiSections != null)
            {
                _remoteUiWindow.Close();
                _remoteUiWindow = null;
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

    private void ShowActuatorDisconnectedDialog()
    {
        var ls = LocalizationService.Instance;
        var vm = new InfoDialogViewModel
        {
            Title = ls["ActuatorDisconnectedTitle"],
            Message = ls["ActuatorDisconnectedMsg"],
            CloseText = ls["Ok"]
        };
        var dialog = new Views.InfoDialog(vm);
        if (Desktop.MainWindow is not null)
            dialog.ShowDialog(Desktop.MainWindow);
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
        {
            LocalizationService.Instance.SetLanguage(value.Code);
            SettingsService.Instance.Set("language", value.Code);
            SettingsService.Instance.Save();
        }
    }

    // === Max value editing (Settings page) ===

    [ObservableProperty]
    private string _editMaxAValue = AppCore.Instance.GetControlDeskMaxA().ToString();

    [ObservableProperty]
    private string _editMaxBValue = AppCore.Instance.GetControlDeskMaxB().ToString();

    [ObservableProperty]
    private bool _useActuatorLimits = AppCore.Instance.GetControlDeskUseActuatorLimits();

    [ObservableProperty]
    private bool _useFloatingRemoteUi = SettingsService.Instance.TryGet("useFloatingRemoteUi", out bool floatingVal) ? floatingVal : false;

    partial void OnUseActuatorLimitsChanged(bool value)
    {
        AppCore.Instance.SetControlDeskUseActuatorLimits(value);
        // Refresh edit values to reflect effective max, then revert back to settings values
        // (the overlay prevents editing when useActuatorLimits is on)
        OnPropertyChanged(nameof(UseActuatorLimits));
    }

    partial void OnUseFloatingRemoteUiChanged(bool value)
    {
        SettingsService.Instance.Set("useFloatingRemoteUi", value);
        SettingsService.Instance.Save();
        OnPropertyChanged(nameof(ShowInlineRemoteUi));

        if (value)
            OpenRemoteUiFloatingWindow();
        else
            CloseRemoteUiFloatingWindow();
    }

    private static void SetCardUiEnabled(ConnectedAgentCard card, bool enabled)
    {
        if (card.RemoteUiSections == null) return;
        foreach (var section in card.RemoteUiSections)
            foreach (var item in section.Items)
                item.IsEnabled = enabled;
    }

    private void OpenRemoteUiFloatingWindow()
    {
        if (_remoteUiWindow != null) return;
        if (SelectedCard?.RemoteUiSections == null) return;
        if (Desktop.MainWindow is null) return;

        _remoteUiWindow = new Views.RemoteUiWindow(SelectedCard);
        _remoteUiWindow.Closed += (_, _) => _remoteUiWindow = null;
        _remoteUiWindow.Show(Desktop.MainWindow);
    }

    private void CloseRemoteUiFloatingWindow()
    {
        if (_remoteUiWindow != null)
        {
            _remoteUiWindow.Close();
            _remoteUiWindow = null;
        }
    }

    public void CommitMaxA()
    {
        if (int.TryParse(EditMaxAValue, out var v))
        {
            ControlDeskViewModel.SetMaxA(v);
            EditMaxAValue = ControlDeskViewModel.MaxA.ToString();
            SettingsService.Instance.Set("maxA", ControlDeskViewModel.MaxA);
            SettingsService.Instance.Save();
        }
        else
        {
            EditMaxAValue = ControlDeskViewModel.MaxA.ToString();
        }
    }

    public void CancelMaxA()
    {
        EditMaxAValue = ControlDeskViewModel.MaxA.ToString();
    }

    public void CommitMaxB()
    {
        if (int.TryParse(EditMaxBValue, out var v))
        {
            ControlDeskViewModel.SetMaxB(v);
            EditMaxBValue = ControlDeskViewModel.MaxB.ToString();
            SettingsService.Instance.Set("maxB", ControlDeskViewModel.MaxB);
            SettingsService.Instance.Save();
        }
        else
        {
            EditMaxBValue = ControlDeskViewModel.MaxB.ToString();
        }
    }

    public void CancelMaxB()
    {
        EditMaxBValue = ControlDeskViewModel.MaxB.ToString();
    }

    [RelayCommand]
    private void ResetStrengthLimits()
    {
        ControlDeskViewModel.SetMaxA(SettingsDefaults.MaxA);
        ControlDeskViewModel.SetMaxB(SettingsDefaults.MaxB);
        EditMaxAValue = SettingsDefaults.MaxA.ToString();
        EditMaxBValue = SettingsDefaults.MaxB.ToString();
        UseActuatorLimits = SettingsDefaults.UseActuatorLimits;
        SettingsService.Instance.Set("maxA", SettingsDefaults.MaxA);
        SettingsService.Instance.Set("maxB", SettingsDefaults.MaxB);
        SettingsService.Instance.Save();
    }

    /// <summary>
    /// Processes a flat list of UiItems into grouped sections.
    /// Items after a "group" declaration belong to that group until the next group or end.
    /// Items before any group get a null-title section (rendered without border).
    /// </summary>
    private static ObservableCollection<RemoteUiSectionViewModel> BuildUiSections(
        List<Core.Scheme.Remote.UiItem> uiItems,
        Action<string, object?> sendValue)
    {
        var sections = new ObservableCollection<RemoteUiSectionViewModel>();
        RemoteUiSectionViewModel? currentSection = null;

        foreach (var item in uiItems)
        {
            if (item.Type == "group")
            {
                currentSection = new RemoteUiSectionViewModel { Title = item.Title, Key = item.Key };
                sections.Add(currentSection);
            }
            else
            {
                if (currentSection == null)
                {
                    currentSection = new RemoteUiSectionViewModel();
                    sections.Add(currentSection);
                }
                var vm = new RemoteUiItemViewModel(item);
                vm.SendValue = sendValue;
                currentSection.Items.Add(vm);
            }
        }

        return sections;
    }
}
