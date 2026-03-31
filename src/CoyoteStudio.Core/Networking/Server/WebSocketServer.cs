using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace CoyoteStudio.Core.Networking.Server;

internal class WebSocketServer : IDisposable
{
    private const int _bufferSize = 4096;
    private readonly ConnectionManager _connectionManager;

    public WebSocketServer(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    private HttpListener? _listener;

    public async Task RunAsync(int port, CancellationToken token, IProgress<ConnectionData> progress)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        Debug.WriteLine("Server started");

        try
        {
            while (!token.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync();

                if (context.Request.IsWebSocketRequest)
                {
                    _ = HandleConnectionAync(progress, context, token);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }
        catch (HttpListenerException e)
        {
            Debug.WriteLine($"Error: {e.Message}");
        }
    }

    private async Task HandleConnectionAync(IProgress<ConnectionData> progress, HttpListenerContext context, CancellationToken token)
    {
        Guid id = Guid.NewGuid();

        var wsContext = await context.AcceptWebSocketAsync(null);
        using var ws = wsContext.WebSocket;
        var buffer = new byte[_bufferSize];

        _connectionManager.Register(id, () =>
        {
            _ = CloseClientConnection(ws);
        });

        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                progress.Report(new ConnectionData(id, Encoding.UTF8.GetString(buffer, 0, result.Count)));
            }
        }
        finally
        {
            _connectionManager.TryUnregister(id);
        }

    }

    private async Task CloseClientConnection(WebSocket ws, string reason = "Server closing")
    {
        if (ws.State == WebSocketState.Open)
        {
            await ws.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    reason,
                    CancellationToken.None
                );
        }
        ws.Dispose();
    }

    public void Dispose()
    {
        _listener?.Stop();
        _listener?.Close();
    }
}