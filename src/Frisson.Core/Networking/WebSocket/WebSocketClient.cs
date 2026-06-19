namespace Frisson.Core.Networking.WebSocket;

using FleckSocket = global::Fleck.IWebSocketConnection;

internal class WebSocketClient
{
    public Guid Id { get; }
    private readonly FleckSocket _socket;

    public WebSocketClient(Guid id, FleckSocket socket)
    {
        Id = id;
        _socket = socket;
    }

    public Task<bool> Send(string message)
    {
        if (!_socket.IsAvailable) return Task.FromResult(false);
        try
        {
            _socket.Send(message);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public void Close() => _socket.Close();

    /// <summary>
    /// Whether this client has been bound to an Agent (first reply processed).
    /// </summary>
    public bool IsBound { get; set; }

    public Action<string>? MessageHandler { get; set; }
    internal void OnMessage(string message)
    {
        if (IsBound)
            MessageHandler?.Invoke(message);
    }
}
