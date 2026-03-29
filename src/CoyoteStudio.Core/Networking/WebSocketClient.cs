using CoyoteStudio.Core.Networking;

namespace CoyoteStudio.Core.Network;

internal enum WebSocketClientKind
{
    Unknown = 0,
    Remote,
    Device
}

internal class WebSocketClient
{

    public Guid Id { get; init; }

    public WebSocketClientKind Kind { get; private set; }

    public WebSocketClient(Guid id)
    {
        Id = id;
    }

    public void Setup(ProtocolScheme scheme)
    {

    }
}
