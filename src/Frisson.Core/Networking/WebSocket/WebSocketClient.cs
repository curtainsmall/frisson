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

    public async Task<bool> Send(string message)
    {
        if (!_socket.IsAvailable)
        {
            LoggerService.Instance.Log($"[Server] Failed to send to {Id}: socket unavailable");
            return false;
        }
        try
        {
            await _socket.Send(message);
            LoggerService.Instance.Log($"[Server] Sent to {Id}: {message}");
            return true;
        }
        catch (Exception ex)
        {
            LoggerService.Instance.Log($"[Server] Failed to send to {Id}: {ex.Message}");
            return false;
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
