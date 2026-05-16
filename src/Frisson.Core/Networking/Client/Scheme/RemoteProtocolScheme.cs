using System.Text.Json;

namespace Frisson.Core.Networking.Client.Scheme;

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
        if (rootElement.TryGetProperty("heartbeat", out _))
        {
            return new RemoteConnectionMessage(RemoteConnectionMessageKind.Heartbeat);
        }
        else if (rootElement.TryGetProperty("error", out _))
        {
            return new RemoteConnectionMessage(RemoteConnectionMessageKind.Error);
        }
        else if (rootElement.TryGetProperty("bind", out _))
        {
            return new RemoteConnectionMessage(RemoteConnectionMessageKind.Bind);
        }
        else if (rootElement.TryGetProperty("break", out _))
        {
            return new RemoteConnectionMessage(RemoteConnectionMessageKind.Break);
        }
        else if (rootElement.TryGetProperty("strength", out var strengthElement))
        {
            return ParseStrengthMessage(strengthElement);
        }
        else
            return null;
    }

    private static RemoteStrengthMessage? ParseStrengthMessage(JsonElement strengthElement)
    {
        if (strengthElement.ValueKind != JsonValueKind.Object)
            return null;

        if (!strengthElement.TryGetProperty("channel", out var channelElement))
            return null;

        var channelKind = channelElement.GetString() switch
        {
            "A" => DeviceChannelKind.A,
            "B" => DeviceChannelKind.B,
            _ => (DeviceChannelKind?)null
        };
        if (channelKind is null)
            return null;

        if (!strengthElement.TryGetProperty("operation", out var operationElement))
            return null;

        var operationKind = operationElement.GetString() switch
        {
            "vary" => RemoteStrengthOperationKind.VaryValue,
            "set" => RemoteStrengthOperationKind.SetValue,
            "limit" => RemoteStrengthOperationKind.SetLimit,
            _ => (RemoteStrengthOperationKind?)null
        };
        if (operationKind is null)
            return null;

        if (!strengthElement.TryGetProperty("value", out var valueElement))
            return null;

        if (!valueElement.TryGetInt32(out var value))
            return null;

        return new RemoteStrengthMessage(channelKind.Value, operationKind.Value, value);
    }
}