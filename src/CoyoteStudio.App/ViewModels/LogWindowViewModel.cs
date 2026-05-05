using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using CoyoteStudio.Core;

namespace CoyoteStudio.App.ViewModels;

public partial class LogWindowViewModel : ViewModelBase
{
    public ObservableCollection<LogEntry> LogEntries => LoggerService.Instance.Entries;
}
