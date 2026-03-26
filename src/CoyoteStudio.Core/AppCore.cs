using System.Runtime.CompilerServices;

using CoyoteStudio.Core.Network;
using CoyoteStudio.Shared;
using CoyoteStudio.Shared.Error;
using CoyoteStudio.Shared.Message;

using Microsoft.Extensions.DependencyInjection;

namespace CoyoteStudio.Core;

internal class AppCore : IAppCore, IDisposable
{
    private readonly IMessager _messager;

    private readonly WebSocketServer _server;

    private readonly CancellationTokenSource _tokenSource = new();

    public event Action<string>? ErrorOccurred;

    public AppCore(IMessager messager, WebSocketServer server)
    {
        _messager = messager;
        _server = server;
    }

    public void StartServerAsync(int port)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _server.RunAsync(port, _tokenSource.Token);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _messager.Send(new ErrorMessage(ErrorCode.Unknown, e.Message));
            }
        });
    }

    public void Dispose()
    {
        _tokenSource.Cancel();
        _tokenSource.Dispose();
    }
}