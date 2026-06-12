using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Frisson.App.Services;
using Frisson.Core;
using Frisson.Core.Agent;

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
/// Represents a connected agent for UI display.
/// </summary>
public class ConnectedAgentInfo : INotifyPropertyChanged
{
    public required Guid AgentId { get; init; }

    /// <summary>
    /// Connection status of the agent.
    /// </summary>
    public required AgentConnectionStatus Status { get; set; }
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
    /// List of all connected agents for display in Control Panel page.
    /// </summary>
    public ObservableCollection<ConnectedAgentInfo> ConnectedAgents { get; } = new();

    /// <summary>
    /// Count of active (connected) agents.
    /// </summary>
    public int ActiveAgentCount => ConnectedAgents.Count(a => a.Status == AgentConnectionStatus.Connected);

    /// <summary>
    /// Count of pending (unbound) agents.
    /// </summary>
    public int PendingAgentCount => ConnectedAgents.Count(a => a.Status == AgentConnectionStatus.Pending);

    /// <summary>
    /// Whether there are any pending agents.
    /// </summary>
    public bool HasPendingAgents => PendingAgentCount > 0;

    /// <summary>
    /// Whether there are any connected agents.
    /// </summary>
    public bool HasConnectedAgents => ConnectedAgents.Any(a => a.Status == AgentConnectionStatus.Connected);

    /// <summary>
    /// Whether an agent is currently selected for control.
    /// </summary>
    public bool HasSelectedAgent => SelectedAgentId.HasValue;

    /// <summary>
    /// Show 'No Agent Selected' panel when no agent is selected.
    /// </summary>
    public bool ShowNoAgentPanel => !HasSelectedAgent;

    /// <summary>
    /// Show controls when an agent is selected.
    /// </summary>
    public bool ShowAgentControls => HasSelectedAgent;

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
    private bool _isAgentPanelExpanded = true;

    public bool IsAgentPanelCollapsed => !IsAgentPanelExpanded;

    /// <summary>
    /// Currently selected agent ID.
    /// </summary>
    [ObservableProperty]
    private Guid? _selectedAgentId;

    public bool IsMixerSelected => CurrentPage == NavPage.Mixer;
    public bool IsWavesSelected => CurrentPage == NavPage.Waves;
    public bool IsSettingsSelected => CurrentPage == NavPage.Settings;

    partial void OnCurrentPageChanged(NavPage value)
    {
        OnPropertyChanged(nameof(IsMixerSelected));
        OnPropertyChanged(nameof(IsWavesSelected));
        OnPropertyChanged(nameof(IsSettingsSelected));
    }

    partial void OnSelectedAgentIdChanged(Guid? value)
    {
        foreach (var agent in ConnectedAgents)
        {
            agent.IsSelected = agent.AgentId == value;
        }

        OnPropertyChanged(nameof(HasSelectedAgent));
        OnPropertyChanged(nameof(ShowNoAgentPanel));
        OnPropertyChanged(nameof(ShowAgentControls));
    }

    partial void OnIsAgentPanelExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsAgentPanelCollapsed));
    }

    [RelayCommand]
    private void SelectPage(NavPage page)
    {
        CurrentPage = page;
    }

    [RelayCommand]
    private void ToggleAgentPanel()
    {
        IsAgentPanelExpanded = !IsAgentPanelExpanded;
    }

    [RelayCommand]
    private void SelectAgent(Guid agentId)
    {
        var agent = ConnectedAgents.FirstOrDefault(a => a.AgentId == agentId);
        if (agent?.Status != AgentConnectionStatus.Connected)
            return;

        SelectedAgentId = agentId;
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
    private void DisconnectAgent(Guid agentId)
    {
        AppCore.Instance.DisconnectAgent(agentId);

        if (SelectedAgentId == agentId)
        {
            SelectedAgentId = null;
        }
    }

    public MainWindowViewModel()
    {
        var currentCulture = LocalizationService.Instance.CurrentCulture.Name;
        var defaultLang = AvailableLanguages.FirstOrDefault(l => l.Code == currentCulture)
                          ?? AvailableLanguages.First(l => l.Code == "en-US");
        _selectedLanguage = defaultLang;

        LocalizationService.Instance.SetLanguage(defaultLang.Code);

        ConnectedAgents.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ActiveAgentCount));
            OnPropertyChanged(nameof(PendingAgentCount));
            OnPropertyChanged(nameof(HasPendingAgents));
            OnPropertyChanged(nameof(HasConnectedAgents));
        };

        AppCore.Instance.AgentConnected += OnAgentConnected;
        AppCore.Instance.AgentDisconnected += OnAgentDisconnected;
    }

    private void OnAgentConnected(object? sender, AgentConnectionEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var existing = ConnectedAgents.FirstOrDefault(a => a.AgentId == e.AgentId);
            if (existing is not null)
                ConnectedAgents.Remove(existing);

            ConnectedAgents.Add(new ConnectedAgentInfo
            {
                AgentId = e.AgentId,
                Status = e.Status,
                StatusColor = "#00FF00"
            });
        });
    }

    private void OnAgentDisconnected(object? sender, AgentConnectionEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var existing = ConnectedAgents.FirstOrDefault(a => a.AgentId == e.AgentId);
            if (existing is not null)
            {
                ConnectedAgents.Remove(existing);

                if (SelectedAgentId == e.AgentId)
                {
                    SelectedAgentId = null;
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
