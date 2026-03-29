using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Text;

using CoyoteStudio.Core.Networking;

namespace CoyoteStudio.Core.Network;

internal class WebSocketServer : IDisposable
{
    private const int _bufferSize = 4096;
    private readonly WebSocketConnectionManager _connectionManager;

    public WebSocketServer(WebSocketConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    private HttpListener? _listener;

    public async Task RunAsync(int port, CancellationToken token, IProgress<WebSocketConnectionData> progress)
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

    private async Task HandleConnectionAync(IProgress<WebSocketConnectionData> progress, HttpListenerContext context, CancellationToken token)
    {
        Guid id = Guid.NewGuid();
        _connectionManager.Register(id);

        var wsContext = await context.AcceptWebSocketAsync(null);
        using var ws = wsContext.WebSocket;
        var buffer = new byte[_bufferSize];

        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                progress.Report(new WebSocketConnectionData(id, Encoding.UTF8.GetString(buffer, 0, result.Count)));
            }
        }
        finally
        {
            _connectionManager.Unregister(id);
        }

    }

    public void Dispose()
    {
        _listener?.Stop();
        _listener?.Close();
    }
}