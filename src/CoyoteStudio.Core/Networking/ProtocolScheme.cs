using System.Text.Json;

using CoyoteStudio.Core.Control;
using CoyoteStudio.Core.Network;

namespace CoyoteStudio.Core.Networking;

internal class ProtocolSchemeException : Exception
{
    public enum ErrorKind
    {
        InvalidFieldType,
        InvalidFieldValue,
    }

    public ErrorKind Kind { get; init; }
    public ProtocolSchemeException(ErrorKind kind, string message) : base(message)
    {
        Kind = kind;
    }
}

internal abstract class ProtocolScheme
{
    private static readonly string[] _deviceIdCadidateNames = { "deviceId", "targetId" };
    private static readonly string[] _remoteIdCadidateNames = { "remoteId", "clientId" };

    internal enum MessageOperationKind
    {
        StrengthIncrease,
        StrengthDecrease,
        StrengthSet,
        Pulse,

        Clear,

        Heartbeat,
    }

    internal enum MessageChannelKind
    {
        A,
        B,
    }

    internal abstract record MessageStrength;
    internal sealed record MessageStrengthValue(int Delta) : MessageStrength;
    internal sealed record MessageStrengthPulse(Pulse Pulse) : MessageStrength;

    internal sealed class DeviceScheme : ProtocolScheme
    {
        public Guid DeviceId { get; init; }
        public Guid RemoteId { get; init; }
        public MessageOperationKind OperationKind { get; init; }
        public MessageChannelKind ChannelKind { get; init; }

        public DeviceScheme(JsonElement root)
        {

        }
    }

    internal sealed class RemoteScheme : ProtocolScheme
    {
        public List<Guid> RemoteIds { get; init; }
        public List<Guid> DeviceIds { get; init; }
        public MessageOperationKind OperationKind { get; init; }
        public MessageChannelKind? ChannelKind { get; init; }
        public MessageStrength? StrengthValue { get; init; }

        public RemoteScheme(JsonElement root)
        {
            OperationKind = ParseOperationKind(root);
            ChannelKind = ParseChannelKind(root);
            RemoteIds = ParseRemoteIds(root);
            DeviceIds = ParseDeviceIds(root);
            StrengthValue = ParseStrengthValue(root);

        }

        private MessageOperationKind ParseOperationKind(JsonElement rootElement)
        {
            var typeElement = rootElement.GetProperty("type");
            switch (rootElement.ValueKind)
            {
                case JsonValueKind.Number:
                {
                    var type = rootElement.GetInt32();
                    return type switch
                    {
                        1 => MessageOperationKind.StrengthDecrease,
                        2 => MessageOperationKind.StrengthIncrease,
                        3 => MessageOperationKind.StrengthSet,
                        4 => MessageOperationKind.Clear,
                        _ => throw new ProtocolSchemeException(ProtocolSchemeException.ErrorKind.InvalidFieldValue, $"Invalid field value as number: 'type' with value {type}")
                    };
                }
                case JsonValueKind.String:
                {
                    var type = rootElement.GetString();
                    return type switch
                    {
                        "increase" => MessageOperationKind.StrengthIncrease,
                        "decrease" => MessageOperationKind.StrengthDecrease,
                        "set" => MessageOperationKind.StrengthSet,
                        "pulse" or "clientMsg" => MessageOperationKind.Pulse,
                        "clear" => MessageOperationKind.Clear,
                        "hearbeat" => MessageOperationKind.Heartbeat,
                        _ => throw new ProtocolSchemeException(ProtocolSchemeException.ErrorKind.InvalidFieldValue, $"Invalid field as string: 'type' with value {type}")
                    };
                }
                default:
                    throw new ProtocolSchemeException(ProtocolSchemeException.ErrorKind.InvalidFieldType, $"Invalid field: 'type' with value {rootElement.GetRawText()}");

            }
        }

        private MessageChannelKind? ParseChannelKind(JsonElement rootElement)
        {
            if (!rootElement.TryGetProperty("channel", out var channelElement))
            {
                return null;
            }
            switch (channelElement.ValueKind)
            {
                case JsonValueKind.Number:
                {
                    var channel = channelElement.GetInt32();
                    return channel switch
                    {
                        1 => MessageChannelKind.A,
                        2 => MessageChannelKind.B,
                        _ => throw new ProtocolSchemeException(ProtocolSchemeException.ErrorKind.InvalidFieldValue, $"Invalid field as string: 'channel' with value {channel}")
                    };
                }
                case JsonValueKind.String:
                {
                    var channel = channelElement.GetString();
                    return channel switch
                    {
                        "A" or "a" => MessageChannelKind.A,
                        "B" or "b" => MessageChannelKind.B,
                        _ => throw new ProtocolSchemeException(ProtocolSchemeException.ErrorKind.InvalidFieldValue, $"Invalid field as number: 'channel' with value {channel}")
                    };
                }
                default:
                    throw new ProtocolSchemeException(ProtocolSchemeException.ErrorKind.InvalidFieldType, $"Invalid field: 'channel' with value {channelElement.GetRawText()}");

            }
        }

        private List<Guid> ParseRemoteIds(JsonElement rootElement)
        {
            JsonElement remoteIdElement = default;
            bool found = false;
            foreach (var key in _remoteIdCadidateNames)
                found = rootElement.TryGetProperty(key, out remoteIdElement);

            if (!found)
                return [];

            List<Guid> remoteIds = [];
            switch (remoteIdElement.ValueKind)
            {
                case JsonValueKind.String:
                {
                    if (Guid.TryParse(remoteIdElement.GetString() ?? string.Empty, out var remoteId))
                        remoteIds.Add(remoteId);
                    break;
                }
                case JsonValueKind.Array:
                {
                    foreach (var remoteIdStringElement in remoteIdElement.EnumerateArray())
                    {
                        if (!Guid.TryParse(remoteIdStringElement.GetString() ?? string.Empty, out var remoteId))
                            continue;

                        remoteIds.Add(remoteId);
                    }
                    break;
                }
                default:
                    throw new ProtocolSchemeException(ProtocolSchemeException.ErrorKind.InvalidFieldType, $"Invalid field: remoteId with value {remoteIdElement.GetRawText()}");
            }
            return remoteIds;

        }

        private List<Guid> ParseDeviceIds(JsonElement rootElement)
        {
            JsonElement deviceIdElement = default;
            bool found = false;
            foreach (var key in _deviceIdCadidateNames)
                found = rootElement.TryGetProperty(key, out deviceIdElement);

            if (!found)
                return [];

            List<Guid> deviceIds = [];
            switch (deviceIdElement.ValueKind)
            {
                case JsonValueKind.String:
                {
                    if (Guid.TryParse(deviceIdElement.GetString() ?? string.Empty, out var deviceId))
                        deviceIds.Add(deviceId);
                    break;
                }
                case JsonValueKind.Array:
                {
                    foreach (var remoteIdStringElement in deviceIdElement.EnumerateArray())
                    {
                        if (!Guid.TryParse(remoteIdStringElement.GetString() ?? string.Empty, out var deviceId))
                            continue;

                        deviceIds.Add(deviceId);
                    }
                    break;
                }
                default:
                    throw new ProtocolSchemeException(ProtocolSchemeException.ErrorKind.InvalidFieldType, $"Invalid field: deviceId with value {deviceIdElement.GetRawText()}");

            }
            return deviceIds;

        }

        private MessageStrength? ParseStrengthValue(JsonElement rootElement)
        {
            if (!rootElement.TryGetProperty("value", out var valueElement))
                return null;

            switch (OperationKind)
            {
                case MessageOperationKind.StrengthDecrease:
                case MessageOperationKind.StrengthIncrease:
                case MessageOperationKind.StrengthSet:
                {
                    return valueElement.ValueKind != JsonValueKind.Number || !valueElement.TryGetInt32(out var value)
                        ? null
                        : new MessageStrengthValue(value);
                }
                case MessageOperationKind.Pulse:
                {
                    var pulseString = valueElement.GetString();
                    if (pulseString is null)
                        return null;

                    Pulse pulse = new();
                    int[] pulseWave = new int[8];
                    int count = 0;
                    for (int i = 0; i < pulseString.Length; i += 2)
                    {
                        while (!char.IsAsciiDigit(pulseString[i++])) ;

                        var hexString = pulseString.Substring(i, 2);
                        if (!int.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, default, out pulseWave[i]))
                            break;

                        if (count == 8)
                        {
                            pulse.Add(pulseWave);
                            pulseWave = new int[8];
                            count = 0;
                        }
                    }
                    if (count != 8)
                        throw new ProtocolSchemeException(ProtocolSchemeException.ErrorKind.InvalidFieldValue, "Pulse wave data is less 8 in a group");

                    return new MessageStrengthPulse(pulse);
                }
                default:
                    return null;
            }
        }

    }

    public static ProtocolScheme Create(WebSocketClientKind clientKind, WebSocketConnectionData data)
    {
        var json = JsonDocument.Parse(data.Message);
        return clientKind switch
        {
            WebSocketClientKind.Device => new DeviceScheme(json.RootElement),
            WebSocketClientKind.Remote => new RemoteScheme(json.RootElement),
            _ => throw new InvalidOperationException($"Invalid client type: {clientKind}")
        };
    }

}


