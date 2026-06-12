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

namespace Frisson.App.ViewModels;

public enum NavPage
{
    Connections,
    LocalControl,
    Settings,
}

public class LanguageOption
{
    public required string Code { get; init; }
    public required string DisplayName { get; init; }
}

/// <summary>
/// Represents a card in the Connections tab agent list.
/// </summary>
public class ConnectedAgentCard : INotifyPropertyChanged
{
    public Guid AgentId { get; init; }
    public AgentKind Kind { get; init; }

    public bool IsDevice => Kind == AgentKind.Device;
    public bool IsControl => Kind == AgentKind.Control;
    public string AgentKindLabel => IsDevice ? "Device" : IsControl ? "Control" : "Pending";

    public string StatusColor { get; set; } = "#888888";

    public int IndentLevel { get; set; }
    public Guid? LinkedToControlId { get; set; }

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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IndentLevel)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LinkedToControlId)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDevice)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsControl)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Kind)));
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

    /// <summary>
    /// Ordered list of agent cards for the Connections tab.
    /// Control agents first (with linked devices indented below), then unlinked devices.
    /// </summary>
    public ObservableCollection<ConnectedAgentCard> AgentCards { get; } = new();

    /// <summary>
    /// Currently selected agent card.
    /// </summary>
    [ObservableProperty]
    private ConnectedAgentCard? _selectedCard;

    [ObservableProperty]
    private NavPage _currentPage = NavPage.Connections;

    [ObservableProperty]
    private LanguageOption _selectedLanguage;

    public bool IsConnectionsSelected => CurrentPage == NavPage.Connections;
    public bool IsLocalControlSelected => CurrentPage == NavPage.LocalControl;
    public bool IsSettingsSelected => CurrentPage == NavPage.Settings;
    public bool HasAgents => AgentCards.Count > 0;

    partial void OnCurrentPageChanged(NavPage value)
    {
        OnPropertyChanged(nameof(IsConnectionsSelected));
        OnPropertyChanged(nameof(IsLocalControlSelected));
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
        AppCore.Instance.AgentDisconnected += OnAgentDisconnected;
        AppCore.Instance.AgentLinked += OnAgentLinked;
        AppCore.Instance.AgentUnlinked += OnAgentUnlinked;

        AgentCards.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAgents));
    }

    private void OnAgentConnected(object? sender, AgentConnectionEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var card = new ConnectedAgentCard
            {
                AgentId = e.AgentId,
                Kind = e.Kind,
                StatusColor = "#00FF00"
            };
            AgentCards.Add(card);
            ReorderCards();
        });
    }

    private void OnAgentDisconnected(object? sender, AgentConnectionEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var card = AgentCards.FirstOrDefault(c => c.AgentId == e.AgentId);
            if (card != null)
            {
                AgentCards.Remove(card);
                if (SelectedCard == card)
                    SelectedCard = null;
                ReorderCards();
            }
        });
    }

    private void OnAgentLinked(Guid deviceId, Guid controlId)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var deviceCard = AgentCards.FirstOrDefault(c => c.AgentId == deviceId);
            if (deviceCard != null)
            {
                deviceCard.LinkedToControlId = controlId;
                deviceCard.IndentLevel = 1;
                ReorderCards();
            }
        });
    }

    private void OnAgentUnlinked(Guid deviceId)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var deviceCard = AgentCards.FirstOrDefault(c => c.AgentId == deviceId);
            if (deviceCard != null)
            {
                deviceCard.LinkedToControlId = null;
                deviceCard.IndentLevel = 0;
                ReorderCards();
            }
        });
    }

    /// <summary>
    /// Reorders cards: Control agents first (with linked devices indented below), then unlinked devices.
    /// </summary>
    private void ReorderCards()
    {
        var links = AppCore.Instance.GetAgentLinks();

        var controls = AgentCards.Where(c => c.IsControl).ToList();
        var unlinkedDevices = AgentCards.Where(c => c.IsDevice && !links.ContainsKey(c.AgentId)).ToList();

        var ordered = new List<ConnectedAgentCard>();
        foreach (var control in controls)
        {
            ordered.Add(control);
            var linkedDevices = AgentCards
                .Where(c => c.IsDevice && links.TryGetValue(c.AgentId, out var cid) && cid == control.AgentId)
                .ToList();
            foreach (var device in linkedDevices)
            {
                device.IndentLevel = 1;
                device.LinkedToControlId = control.AgentId;
                ordered.Add(device);
            }
        }
        ordered.AddRange(unlinkedDevices);

        AgentCards.Clear();
        foreach (var card in ordered)
            AgentCards.Add(card);
    }

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        if (value != null)
            LocalizationService.Instance.SetLanguage(value.Code);
    }
}
