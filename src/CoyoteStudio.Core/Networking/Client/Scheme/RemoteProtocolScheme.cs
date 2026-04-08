using System.Text.Json;

namespace CoyoteStudio.Core.Networking.Client.Scheme;

internal enum RemoteConnectionMessageKind
{
    Error,
    Bind,
    Break,
    Heartbeat,
}

internal enum RemoteStrengthOperationKind
{
    VaryValue,
    SetValue,
    SetLimit
}

internal abstract record RemoteMessage();

internal record RemoteConnectionMessage(RemoteConnectionMessageKind MessageKind) : RemoteMessage;

internal record RemoteStrengthMessage(DeviceChannelKind ChannelKind, RemoteStrengthOperationKind OperationKind, int Value) : RemoteMessage;

internal sealed class RemoteProtocolScheme : ProtocolScheme
{
    public Guid? RemoteId { get; init; }
    public List<Guid> DeviceIds { get; init; }

    public RemoteMessage? Message { get; init; }

    public RemoteProtocolScheme(JsonDocument jsonDoc)
    {
        var rootElement = jsonDoc.RootElement;
        RemoteId = ParseRemoteId(rootElement);
        DeviceIds = ParseDeviceIds(rootElement);
        Message = ParseMessage(rootElement);
    }

    private Guid? ParseRemoteId(JsonElement rootElement)
    {
        if (!rootElement.TryGetProperty("remoteId", out var remoteIdElement))
            return null;

        if (remoteIdElement.ValueKind != JsonValueKind.String)
            return null;

        if (!remoteIdElement.TryGetGuid(out var remoteId))
            return null;

        return remoteId;
    }

    private List<Guid> ParseDeviceIds(JsonElement rootElement)
    {
        if (!rootElement.TryGetProperty("deviceIds", out var deviceIdsElement))
            return [];

        var deviceIds = new List<Guid>();
        switch (deviceIdsElement.ValueKind)
        {
            case JsonValueKind.Array:
            {
                foreach (var deviceIdElement in deviceIdsElement.EnumerateArray())
                {
                    if (!deviceIdElement.TryGetGuid(out var deviceId))
                        continue;

                    deviceIds.Add(deviceId);
                }
                break;
            }
            case JsonValueKind.String:
            {
                if (!deviceIdsElement.TryGetGuid(out var deviceId))
                    break;

                deviceIds.Add(deviceId);
                break;
            }
            default:
                throw new ProtocolSchemeException(ProtocolSchemeException.ErrorKind.InvalidFieldType, $"Invalid deviceIds field type {deviceIdsElement.ValueKind}");
        }
        return deviceIds;
    }

    private RemoteMessage? ParseMessage(JsonElement rootElement)
    {
        if (rootElement.TryGetProperty("heartbeat", out var heartbeatElement))
        {
            return new RemoteConnectionMessage(RemoteConnectionMessageKind.Heartbeat);
        }
        else if (rootElement.TryGetProperty("error", out var errorElement))
        {
            return new RemoteConnectionMessage(RemoteConnectionMessageKind.Error);
        }
        else if (rootElement.TryGetProperty("bind", out var bindElement))
        {
            return new RemoteConnectionMessage(RemoteConnectionMessageKind.Bind);
        }
        else if (rootElement.TryGetProperty("break", out var breakElement))
        {
            return new RemoteConnectionMessage(RemoteConnectionMessageKind.Break);
        }
        else
            return null;
    }
}