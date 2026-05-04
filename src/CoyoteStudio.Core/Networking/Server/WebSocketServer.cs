using System.Collections.Concurrent;
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

    internal class ClientDisconnectingEventArgs(Guid clientId) : EventArgs
    {
        public Guid ClientId { get; init; } = clientId;
    }

    internal class ClientMessageReceivedEventArgs(Guid clientId, string message) : EventArgs
    {
        public Guid ClientId { get; init; } = clientId;
        public string Message { get; init; } = message;
    }

    private const int _bufferSize = 4096;
    private const int _maxMessageLength = 1950;

    private HttpListener? _listener;
    private readonly CancellationTokenSource _serverCts = new();
    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();

    public event EventHandler<ClientConnectedEventArgs>? ClientConnected;
    public event EventHandler<ClientDisconnectingEventArgs>? ClientDisconnecting;
    public event EventHandler<ClientDisconnectedEventArgs>? ClientDisconnected;
    public event EventHandler<ClientMessageReceivedEventArgs>? ClientMessageReceived;

    public WebSocketServer()
    {
    }

    public async Task RunAsync(int port)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{port}/");
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

            // Register client
            _clients.TryAdd(id, ws);

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
                // Notify before unregistering so listeners can still send messages
                ClientDisconnecting?.Invoke(this, new ClientDisconnectingEventArgs(id));
                // Unregister client
                _clients.TryRemove(id, out _);
                ClientDisconnected?.Invoke(this, new ClientDisconnectedEventArgs(id));
            }
        }

    }

    /// <summary>
    /// Sends a message to a specific client.
    /// </summary>
    /// <param name="clientId">The target client ID.</param>
    /// <param name="message">The message to send.</param>
    /// <returns>True if the message was sent successfully.</returns>
    public async Task<bool> SendAsync(Guid clientId, string message)
    {
        if (message.Length > _maxMessageLength)
        {
            Debug.WriteLine($"Message too long ({message.Length} chars), max {_maxMessageLength}");
            return false;
        }

        if (!_clients.TryGetValue(clientId, out var ws))
            return false;

        if (ws.State != WebSocketState.Open)
            return false;

        try
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await ws.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                endOfMessage: true,
                CancellationToken.None);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to send message to client {clientId}: {ex.Message}");
            return false;
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

        // Close all client connections
        foreach (var ws in _clients.Values)
        {
            _ = CloseClientConnection(ws);
        }
        _clients.Clear();

        _serverCts.Dispose();
        _listener?.Stop();
        _listener?.Close();
    }
}