using System.Diagnostics;
using System.Text.Json;

using CoyoteStudio.Core.Networking.Client.Scheme;
using CoyoteStudio.Core.Networking.Server;

namespace CoyoteStudio.Core.Networking.Client;

internal enum WebSocketClientKind
{
    Unknown = 0,
    Remote,
    Device
}

internal class WebSocketClient : IDisposable
{
    internal record ChannelState
    {
        public int Strength { get; private set; }
    }

    public Action? OnDisposing { get; init; }

    public Guid Id { get; init; }
    public WebSocketClientKind Kind { get; private set; }
    public ClientData? Data { get; private set; }

    public WebSocketClient(Guid id, Action? onDisposing)
    {
        Id = id;
        OnDisposing = onDisposing;
    }

    public virtual void Setup(ConnectionData data)
    {

        switch (Kind)
        {
            case WebSocketClientKind.Device:
            {

                break;
            }
            case WebSocketClientKind.Remote:
            {
                break;
            }
            case WebSocketClientKind.Unknown:
                return;
            default:
                throw new InvalidOperationException($"Invalid client kind");
        }
    }

    public void Dispose()
    {
        OnDisposing?.Invoke();
    }
}
