using System.Text.Json;

namespace CoyoteStudio.Core.Networking.Client.Scheme;

internal sealed class RemoteProtocolScheme : ProtocolScheme
{
    public List<Guid> RemoteIds { get; init; }
    public List<Guid> DeviceIds { get; init; }
    public MessageOperationKind OperationKind { get; init; }
    public MessageChannelKind? ChannelKind { get; init; }
    public int? Vary { get; init; }
    public int? Set { get; init; }

    public RemoteProtocolScheme(JsonElement root)
    {

    }

}