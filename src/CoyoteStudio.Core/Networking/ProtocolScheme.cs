using System.Text.Json;

using CoyoteStudio.Core.Control;
using CoyoteStudio.Core.Network;

namespace CoyoteStudio.Core.Networking;

internal abstract class ProtocolScheme
{
    private static readonly string[] _deviceIdCadidateNames = { "deviceId", "targetId" };
    private static readonly string[] _remoteIdCadidateNames = { "remoteId", "clientId" };

    internal enum MessageOperationKind
    {
        StrengthIncrease,
        StrengthDecrease,
        StrengthSet,
        Forward,
        Pulse,

        Bind,
        Break,
        Error,
        HeartBeat,
    }

    internal enum MessageChannelKind
    {
        A,
        B,
    }

    internal abstract record StrengthValue;
    internal sealed record StrengthValueIncrease(int Delta) : StrengthValue;
    internal sealed record StrengthValueDecrease(int Delta) : StrengthValue;
    internal sealed record StrengthValueSet(int Value) : StrengthValue;
    internal sealed record StrengthValuePulse(Pulse pulse) : StrengthValue;

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
        public Guid DeviceId { get; init; }
        public Guid RemoteId { get; init; }
        public MessageOperationKind OperationKind { get; init; }
        public MessageChannelKind ChannelKind { get; init; }
        public StrengthValue? Value { get; init; }

        public RemoteScheme(JsonElement root)
        {
            OperationKind = ParseOperationKind(root.GetProperty("type"));

        }

        private MessageOperationKind ParseOperationKind(JsonElement type_json)
        {
            switch (type_json.ValueKind)
            {
                case JsonValueKind.Number:
                {
                    var type = type_json.GetInt32();
                    return type switch
                    {
                        1 => MessageOperationKind.StrengthDecrease,
                        2 => MessageOperationKind.StrengthIncrease,
                        3 => MessageOperationKind.StrengthSet,
                        4 => MessageOperationKind.Forward,
                        _ => throw new InvalidOperationException($"Invalid type as number: {type}")
                    };
                }
                case JsonValueKind.String:
                {
                    var type = type_json.GetString();
                    return type switch
                    {
                        "clientMsg" => MessageOperationKind.Pulse,
                        _ => throw new InvalidOperationException($"Invalid type as string: {type}")
                    };
                }
                default:
                    throw new InvalidOperationException($"Invalid field: 'type' with value {type_json.GetRawText()}");

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


