using CoyoteStudio.Core.Error;
using CoyoteStudio.Core.Networking.Client.Scheme;
using CoyoteStudio.Core.Networking.Server;

namespace CoyoteStudio.Core;

public class AppCore : IDisposable
{

    private readonly WebSocketManager _manager = new();

    public event Action<string>? ErrorOccurred;

    public ErrorMessager ErrorMessager { get; private init; } = new();

    public AppCore()
    {
    }

    public void Startup(int port)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _manager.StartupAsync(port);
            }
            catch (ProtocolSchemeException e)
            {
                ErrorMessager.Send(new ErrorMessage(ErrorCode.InvalidJson, e.Message));
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
        _manager.Dispose();
    }
}