using Frisson.Core.Error;
using Frisson.Core.Networking.Client;
using Frisson.Core.Networking.Server;

namespace Frisson.Core;

public class AppCore : IDisposable
{
    private static AppCore? _instance;
    public static AppCore Instance => _instance ??= new AppCore();

    private readonly WebSocketManager _wsManager = new();

    public event Action<string>? ErrorOccurred;
    public event EventHandler<ClientConnectionEventArgs>? ClientConnected;
    public event EventHandler<ClientConnectionEventArgs>? ClientDisconnected;

    public ErrorMessager ErrorMessager { get; private init; } = new();

    private AppCore()
    {
        _wsManager.ClientConnected += (s, e) => ClientConnected?.Invoke(s, e);
        _wsManager.ClientDisconnected += (s, e) => ClientDisconnected?.Invoke(s, e);
    }

    public void Startup(int port)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _wsManager.RunAsync(port);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                ErrorMessager.Send(new ErrorMessage(ErrorCode.Unknown, e.Message));
            }
        });
    }

    public void Dispose()
    {
        ErrorMessager.Dispose();
        _wsManager.Dispose();
    }

    /// <summary>
    /// Disconnects a specific client by ID.
    /// </summary>
    public void DisconnectClient(Guid clientId)
    {
        _wsManager.DisconnectClient(clientId);
    }
}
