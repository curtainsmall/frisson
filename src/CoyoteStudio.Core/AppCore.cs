using System.Runtime.CompilerServices;

using CoyoteStudio.Core.Control;
using CoyoteStudio.Core.Network;
using CoyoteStudio.Shared;
using CoyoteStudio.Shared.Error;

using Microsoft.Extensions.DependencyInjection;

namespace CoyoteStudio.Core;

public class AppCore : IDisposable
{

    private readonly WebSocketServer _server = new();
    private readonly Controller _controller = new();

    private readonly CancellationTokenSource _tokenSource = new();

    public event Action<string>? ErrorOccurred;

    public Messager Messager { get; private init; } = new();

    public AppCore()
    {
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
                    new Progress<string>(value =>
                        _controller.Receive(value))
                    );
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
        _controller.Dispose();
    }
}