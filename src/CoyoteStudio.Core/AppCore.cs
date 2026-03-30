using CoyoteStudio.Core.Error;
using CoyoteStudio.Core.Network;
using CoyoteStudio.Core.Networking;

namespace CoyoteStudio.Core;

public class AppCore : IDisposable
{

    private readonly WebSocketConnectionManager _connectionManager = new();

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
                    new Progress<WebSocketConnectionData>(connectionData =>
                    {
                        _connectionManager.TryGetClient(connectionData.Id, out var client);
                        client?.Setup(ProtocolScheme.Create(client.Kind, connectionData));
                    }
                    ));
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