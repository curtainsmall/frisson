using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace CoyoteStudio.Core.Network;

internal static class ServerConstants
{
    public const int BufferSize = 4096;
}

internal class WebSocketServer
{
    private HttpListener? _listener;

    public async Task RunAsync(int port, CancellationToken cts)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        Debug.WriteLine("Server started");

        try
        {
            while (!cts.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync();

                if (context.Request.IsWebSocketRequest)
                {
                    _ = HandleConnection(context, cts);
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

    private async Task HandleConnection(HttpListenerContext context, CancellationToken cts)
    {
        var wsContext = await context.AcceptWebSocketAsync(null);
        using var ws = wsContext.WebSocket;
        var buffer = new byte[ServerConstants.BufferSize];

        while (ws.State == System.Net.WebSockets.WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            string received = Encoding.UTF8.GetString(buffer, 0, result.Count);

        }

    }

    public void Dispose()
    {
        _listener?.Stop();
        _listener?.Close();
    }
}
