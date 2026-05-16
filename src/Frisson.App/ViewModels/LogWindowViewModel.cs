using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Frisson.Core;

namespace Frisson.App.ViewModels;

public partial class LogWindowViewModel : ViewModelBase
{
    public ObservableCollection<LogEntry> LogEntries => LoggerService.Instance.Entries;
}
