using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace CoyoteStudio.Core.Networking.Server;

internal class WebSocketServer : IDisposable
{
    internal class ClientConnectedEventArgs : EventArgs
    {
        public Guid ClientId { get; init; }
        public Action? OnDisposing { get; init; }

        public ClientConnectedEventArgs(Guid clientId, Action? onDisposing)
        {
            ClientId = clientId;
            OnDisposing = onDisposing;
        }
    }

    internal class ClientDisconnectedEventArgs(Guid clientId) : EventArgs
    {
        public Guid ClientId { get; init; } = clientId;
    }

    internal class ClientMessageReceivedEventArgs(Guid clientId, string message) : EventArgs
    {
        public Guid ClientId { get; init; } = clientId;
        public string Message { get; init; } = message;
    }

    private const int _bufferSize = 4096;

    private HttpListener? _listener;
    private readonly CancellationTokenSource _serverCts = new();

    public event EventHandler<ClientConnectedEventArgs>? ClientConnected;
    public event EventHandler<ClientDisconnectedEventArgs>? ClientDisconnected;
    public event EventHandler<ClientMessageReceivedEventArgs>? ClientMessageReceived;

    public WebSocketServer()
    {
    }

    public async Task RunAsync(int port)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        Debug.WriteLine("Server started");

        try
        {
            while (!_serverCts.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync();

                if (context.Request.IsWebSocketRequest)
                {
                    var clientCts = CancellationTokenSource.CreateLinkedTokenSource(_serverCts.Token);
                    _ = HandleConnectionAync(context, clientCts);
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

    private async Task HandleConnectionAync(HttpListenerContext context, CancellationTokenSource clientCts)
    {
        using (clientCts)
        {
            Guid id = Guid.NewGuid();

            var wsContext = await context.AcceptWebSocketAsync(null);
            using var ws = wsContext.WebSocket;
            var buffer = new byte[_bufferSize];

            ClientConnected?.Invoke(this, new ClientConnectedEventArgs(id, () => clientCts.Cancel()));

            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), clientCts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    ClientMessageReceived?.Invoke(this, new ClientMessageReceivedEventArgs(id, Encoding.UTF8.GetString(buffer, 0, result.Count)));
                }
            }
            finally
            {
                ClientDisconnected?.Invoke(this, new ClientDisconnectedEventArgs(id));
            }
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
        _serverCts.Cancel();
        _serverCts.Dispose();
        _listener?.Stop();
        _listener?.Close();
    }
}