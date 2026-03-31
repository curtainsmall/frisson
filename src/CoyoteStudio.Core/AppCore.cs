using CoyoteStudio.Core.Error;
using CoyoteStudio.Core.Networking.Client.Scheme;
using CoyoteStudio.Core.Networking.Server;

namespace CoyoteStudio.Core;

public class AppCore : IDisposable
{

    private readonly ConnectionManager _connectionManager = new();

    private readonly CancellationTokenSource _tokenSource = new();

    private readonly WebSocketServer _server;

    public event Action<string>? ErrorOccurred;

    public ErrorMessager Messager { get; private init; } = new();

    public AppCore()
    {
        _server = new WebSocketServer(_connectionManager);
    }

    public void StartServerAsync(int port)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _server.RunAsync(
                    port,
                    _tokenSource.Token,
                    new Progress<ConnectionData>(connectionData => _connectionManager.SetupClient(connectionData)));
            }
            catch (ProtocolSchemeException e)
            {
                Messager.Send(new ErrorMessage(ErrorCode.InvalidJson, e.Message));
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Messager.Send(new ErrorMessage(ErrorCode.Unknown, e.Message));
            }
        });
    }

    public void Dispose()
    {
        _tokenSource.Cancel();
        _tokenSource.Dispose();

        Messager.Dispose();
        _server.Dispose();
    }
}