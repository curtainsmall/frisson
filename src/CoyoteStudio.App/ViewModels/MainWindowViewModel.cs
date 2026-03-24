using System.Collections.ObjectModel;

using CoyoteStudio.Shared;

namespace CoyoteStudio.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<ClientDto> ConnectedClients { get; } = new();
    public string Greeting { get; } = "Welcome to Avalonia!";

    public MainWindowViewModel()
    {
        ConnectedClients.Add(new ClientDto { ClientName = "localhost" });
    }
}
