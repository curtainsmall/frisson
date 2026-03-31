using System.Text.Json;

namespace CoyoteStudio.Core.Networking.Client.Scheme;

internal sealed class DeviceProtocolScheme : ProtocolScheme
{
    private readonly JsonDocument _jsonDocument;

    public record DeviceStrength(int StrengthA, int StrengthB, int limitA, int limitB);

    public Guid? DeviceId { get; init; }
    public Guid? RemoteId { get; init; }
    public DeviceStrength Strength { get; init; }
    public int CornerMark { get; init; }

    public DeviceProtocolScheme(JsonDocument jsonDoc)
    {
        _jsonDocument = jsonDoc;
        DeviceId = ParseDeviceId();
        RemoteId = ParseRemoteId();
        var list = ParseMessage();
        switch (list?.Count)
        {
            case 4:
            {
                Strength = new DeviceStrength(list[0], list[1], list[2], list[3]);
                break;
            }
            case 1:
            {
                CornerMark = list[0];
                break;
            }
            case null:
            {
                break;
            }
            default:
                throw new ProtocolSchemeException(ProtocolSchemeException.ErrorKind.InvalidFieldValue, $"Invalid count of message numbers: {list.Count}");
        }
    }

    private Guid? ParseDeviceId()
    {
        var rootElement = _jsonDocument.RootElement;
        if (!rootElement.TryGetProperty("targetId", out var deviceIdElement))
            return null;

        if (deviceIdElement.ValueKind != JsonValueKind.String)
            return null;

        if (!Guid.TryParse(deviceIdElement.GetString(), out var deviceId))
            return null;

        return deviceId;
    }

    private Guid? ParseRemoteId()
    {
        var rootElement = _jsonDocument.RootElement;
        if (!rootElement.TryGetProperty("targetId", out var deviceIdElement))
            return null;

        if (deviceIdElement.ValueKind != JsonValueKind.String)
            return null;

        if (!Guid.TryParse(deviceIdElement.GetString(), out var remoteId))
            return null;

        return remoteId;
    }

    private List<int> ParseMessage()
    {
        var rootElement = _jsonDocument.RootElement;
        if (!rootElement.TryGetProperty("message", out var messageElement))
            return [];

        if (messageElement.ValueKind != JsonValueKind.String)
            return [];

        var messageString = messageElement.GetString();
        if (messageString is null || messageString.Length < 8)
            return [];

        var typeString = messageString[..8];
        switch (typeString)
        {
            case "strength":
            {
                string[] numStrings = typeString[9..].Split('+');
                if (numStrings.Length != 4)
                    return [];

                List<int> res = [];
                for (int i = 0; i < 4; i++)
                {
                    if (!int.TryParse(numStrings[i], default, out var num))
                        throw new ProtocolSchemeException(ProtocolSchemeException.ErrorKind.InvalidFieldValue, $"Invalid strength value: {numStrings[i]}");
                    res.Add(num);
                }
                return res;
            }
            case "feedback":
            {
                string numString = typeString[9..];

                if (!int.TryParse(numString, default, out var num))
                    throw new ProtocolSchemeException(ProtocolSchemeException.ErrorKind.InvalidFieldValue, $"Invalid strength value: {numString}");
                return [num];
            }
            default:
                throw new ProtocolSchemeException(ProtocolSchemeException.ErrorKind.InvalidFieldValue, $"Unknown messager leader: {typeString}");
        }
    }

}
