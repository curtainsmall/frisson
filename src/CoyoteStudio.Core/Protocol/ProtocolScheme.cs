using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

using CoyoteStudio.Core.Network;
using CoyoteStudio.Core.Networking;

namespace CoyoteStudio.Core.Protocol;

internal class ProtocolScheme
{
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

    private static readonly string[] _deviceIdCadidateNames = { "deviceId", "targetId" };
    private static readonly string[] _remoteIdCadidateNames = { "remoteId", "clientId" };

    public WebSocketClientKind? ClientKind { get; private set; }
    public Guid DeviceId { get; private set; }
    public Guid RemoteId { get; private set; }
    public MessageOperationKind OperationKind { get; private set; }
    public MessageChannelKind ChannelKind { get; private set; }

    public ProtocolScheme(WebSocketConnectionData data)
    {
        using var doc = JsonDocument.Parse(data.Message);
        var root = doc.RootElement;

        ParseType(root);
        ReadDeviceId(root);
        ReadRemoteId(root);
        ParseMessage(root);
    }

    private void ParseType(JsonElement json)
    {
        var type_json = json.GetProperty("type");
        switch (type_json.ValueKind)
        {
            case JsonValueKind.Number:
            {
                var type = type_json.GetInt32();
                OperationKind = type switch
                {
                    0 => MessageOperationKind.StrengthDecrease,
                    1 => MessageOperationKind.StrengthIncrease,
                    2 => MessageOperationKind.StrengthSet,
                    3 => MessageOperationKind.Forward,
                    _ => throw new InvalidOperationException($"Invalid type field: {type}")
                };
                break;
            }
            case JsonValueKind.String:
            {
                var type = type_json.GetString();
                OperationKind = type switch
                {
                    "clienMsg" => MessageOperationKind.Pulse,
                    "bind" => MessageOperationKind.Bind,
                    "break" => MessageOperationKind.Break,
                    "error" => MessageOperationKind.Error,
                    "heartBeat" => MessageOperationKind.HeartBeat,
                    _ => throw new InvalidOperationException($"Invalid type field: {type}")
                };
                break;
            }
            default:
                throw new InvalidOperationException($"Invalid type field: {type_json.ValueKind}");
        }

    }

    private void ParseChannel(JsonElement json)
    {

    }

    private void ReadDeviceId(JsonElement json)
    {
        foreach (var candidate in _deviceIdCadidateNames)
        {
            if (json.TryGetProperty(candidate, out var deviceId))
            {
                if (Guid.TryParse(deviceId.GetString(), out var uuid))
                {
                    DeviceId = uuid;
                }
                break;
            }
        }
    }

    private void ReadRemoteId(JsonElement json)
    {
        foreach (var candidate in _remoteIdCadidateNames)
        {
            if (json.TryGetProperty(candidate, out var remoteId))
            {
                if (Guid.TryParse(remoteId.GetString(), out var uuid))
                {
                    RemoteId = uuid;
                }
                break;
            }
        }
    }

    private void ParseMessage(JsonElement json)
    {

    }
}
