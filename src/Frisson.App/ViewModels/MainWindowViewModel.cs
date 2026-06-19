using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Frisson.App.Services;
using Frisson.Core;
using Frisson.Core.Agent;
using Frisson.Core.Agent.Control;
using Frisson.Core.Agent.Device;

namespace Frisson.App.ViewModels;

public enum NavPage
{
    ControlDesk,
    ControlSources,
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

    public bool IsDevice => AgentType == typeof(DeviceAgent);
    public bool IsControl => AgentType == typeof(ControlSourceAgent);
    public string AgentKindLabel => IsDevice ? "Device" : IsControl ? "Control" : "?";

    public string StatusColor { get; set; } = "#888888";

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

    public void NotifyLayoutChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDevice)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsControl)));
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

    /// <summary>Only Device agents — shown in the global right panel.</summary>
    public IEnumerable<ConnectedAgentCard> DeviceCards => AgentCards.Where(c => c.IsDevice);

    /// <summary>Only ControlSource agents — shown on the Sources page.</summary>
    public IEnumerable<ConnectedAgentCard> SourceCards => AgentCards.Where(c => c.IsControl);

    [ObservableProperty]
    private ConnectedAgentCard? _selectedCard;

    [ObservableProperty]
    private NavPage _currentPage = NavPage.ControlDesk;

    [ObservableProperty]
    private LanguageOption _selectedLanguage;

    public bool IsControlDeskSelected => CurrentPage == NavPage.ControlDesk;
    public bool IsControlSourcesSelected => CurrentPage == NavPage.ControlSources;
    public bool IsSettingsSelected => CurrentPage == NavPage.Settings;
    public bool HasAgents => AgentCards.Count > 0;

    partial void OnCurrentPageChanged(NavPage value)
    {
        OnPropertyChanged(nameof(IsControlDeskSelected));
        OnPropertyChanged(nameof(IsControlSourcesSelected));
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
    private void ShowLogWindow()
    {
        var window = new Views.LogWindow();
        if (App.MainWindow is not null)
            window.Show(App.MainWindow);
    }

    [RelayCommand]
    private void ClearLogs() => LoggerService.Instance.Clear();

    public MainWindowViewModel()
    {
        var currentCulture = LocalizationService.Instance.CurrentCulture.Name;
        var defaultLang = AvailableLanguages.FirstOrDefault(l => l.Code == currentCulture)
                          ?? AvailableLanguages.First(l => l.Code == "en-US");
        _selectedLanguage = defaultLang;
        LocalizationService.Instance.SetLanguage(defaultLang.Code);

        AppCore.Instance.AgentConnected += OnAgentConnected;
        AppCore.Instance.AgentClosing += OnAgentClosing;

        AgentCards.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasAgents));
            OnPropertyChanged(nameof(DeviceCards));
            OnPropertyChanged(nameof(SourceCards));
        };
    }

    private void OnAgentConnected(object? sender, AgentEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var card = new ConnectedAgentCard
            {
                AgentId = e.AgentId,
                AgentType = e.AgentType,
                StatusColor = "#00FF00"
            };
            AgentCards.Add(card);
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

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        if (value != null)
            LocalizationService.Instance.SetLanguage(value.Code);
    }
}
