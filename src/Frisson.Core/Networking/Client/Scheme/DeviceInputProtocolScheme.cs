using System.Text.Json;

namespace Frisson.Core.Networking.Client.Scheme;

internal sealed class DeviceInputProtocolScheme : ProtocolScheme
{
    public record DeviceStrength(int StrengthA, int StrengthB, int limitA, int limitB);

    public Guid? DeviceId { get; init; }
    public Guid? RemoteId { get; init; }
    public DeviceStrength? Strength { get; init; }
    public int? Feedback { get; init; }

    public DeviceInputProtocolScheme(JsonDocument jsonDoc)
    {
        var rootElement = jsonDoc.RootElement;

        // Validate message type
        if (!ValidateMessageType(rootElement))
            throw new ProtocolSchemeException(ProtocolSchemeException.ErrorKind.InvalidFieldValue, "Invalid or missing 'type' field, expected 'msg'");

        DeviceId = ParseDeviceId(rootElement);
        RemoteId = ParseRemoteId(rootElement);
        var list = ParseMessage(rootElement);
        switch (list?.Count)
        {
            case 4:
            {
                Strength = new DeviceStrength(list[0], list[1], list[2], list[3]);
                break;
            }
            case 1:
            {
                Feedback = list[0];
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

    private static bool ValidateMessageType(JsonElement rootElement)
    {
        if (!rootElement.TryGetProperty("type", out var typeElement))
            return false;

        if (typeElement.ValueKind != JsonValueKind.String)
            return false;

        return typeElement.GetString() == "msg";
    }

    private static Guid? ParseDeviceId(JsonElement rootElement)
    {
        if (!rootElement.TryGetProperty("targetId", out var deviceIdElement))
            return null;

        if (deviceIdElement.ValueKind != JsonValueKind.String)
            return null;

        if (!deviceIdElement.TryGetGuid(out var deviceId))
            return null;

        return deviceId;
    }

    private static Guid? ParseRemoteId(JsonElement rootElement)
    {
        if (!rootElement.TryGetProperty("clientId", out var remoteIdElement))
            return null;

        if (remoteIdElement.ValueKind != JsonValueKind.String)
            return null;

        if (!remoteIdElement.TryGetGuid(out var remoteId))
            return null;

        return remoteId;
    }

    private static List<int> ParseMessage(JsonElement rootElement)
    {
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
                // Format: "strength-A+B+limitA+limitB" (e.g., "strength-10+20+100+100")
                var payload = messageString.Length > 9 ? messageString[9..] : "";
                string[] numStrings = payload.Split('+');
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
                // Format: "feedback-N" (e.g., "feedback-5")
                var payload = messageString.Length > 9 ? messageString[9..] : "";

                if (!int.TryParse(payload, default, out var num))
                    throw new ProtocolSchemeException(ProtocolSchemeException.ErrorKind.InvalidFieldValue, $"Invalid feedback value: {payload}");
                return [num];
            }
            default:
                throw new ProtocolSchemeException(ProtocolSchemeException.ErrorKind.InvalidFieldValue, $"Unknown message leader: {typeString}");
        }
    }

}
