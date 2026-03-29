using CoyoteStudio.Core.Protocol;

namespace CoyoteStudio.Core.Network;

internal enum WebSocketClientKind
{
    Remote,
    Device
}

internal class WebSocketClient
{

    public Guid Id { get; init; }

    public WebSocketClientKind? Kind { get; private set; }

    public WebSocketClient(Guid id)
    {
        Id = id;
    }

    public void Setup(ProtocolScheme scheme)
    {

    }
}
