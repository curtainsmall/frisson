using CoyoteStudio.Core.Network;

namespace CoyoteStudio.Core;

public class AppCore : IDisposable
{
    private readonly WebSocketServer _server = new();
    private readonly CancellationTokenSource _tokenSource = new();

    public event Action<string>? ErrorOccurred;

    private void NotifyUIThread(string msg)
    {
        ErrorOccurred?.Invoke(msg);
    }

    public AppCore()
    {
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
                NotifyUIThread("Server Crash: " + e.Message);
            }
        });

    }

    public void Dispose()
    {
        _tokenSource.Cancel();

        _server.Dispose();
        _tokenSource.Dispose();
    }
}