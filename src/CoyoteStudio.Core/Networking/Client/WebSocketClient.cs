using System.Diagnostics;
using System.Text.Json;

using CoyoteStudio.Core.Networking.Client.Scheme;
using CoyoteStudio.Core.Networking.Server;

namespace CoyoteStudio.Core.Networking.Client;

internal enum WebSocketClientKind
{
    Unknown = 0,
    Remote,
    Device
}

internal class WebSocketClient : IDisposable
{
    internal record ChannelState
    {
        public int Strength { get; private set; }
    }

    public Action OnDisposing { get; init; }

    public Guid Id { get; init; }
    public WebSocketClientKind Kind { get; private set; }
    public ClientData? Data { get; private set; }

    public WebSocketClient(Guid id, Action onDisposing)
    {
        Id = id;
        OnDisposing = onDisposing;
    }

    public void Setup(ConnectionData data)
    {
        var jsonDoc = JsonDocument.Parse(data.Message);
        if (jsonDoc is null)
            return;

        switch (Kind)
        {
            case WebSocketClientKind.Device:
            {
                var scheme = new DeviceProtocolScheme(jsonDoc);
                var deviceData = new DeviceClientData();
                if (scheme.Strength is not null)
                {
                    deviceData.ChannelA.Strength = scheme.Strength.StrengthA;
                    deviceData.ChannelB.Strength = scheme.Strength.StrengthB;
                    deviceData.ChannelA.Limit = scheme.Strength.limitA;
                    deviceData.ChannelB.Limit = scheme.Strength.limitB;
                    Data = deviceData;
                }
                else if (scheme.Feedback is not null)
                {
                    switch (scheme.Feedback)
                    {
                        case >= 0 and <= 4:
                        {
                            deviceData.ChannelA.FeedbackKind = (DeviceFeedbackKind)(scheme.Feedback + 1);
                            break;
                        }
                        case >= 5 and <= 9:
                        {
                            deviceData.ChannelB.FeedbackKind = (DeviceFeedbackKind)scheme.Feedback - 5 + 1;
                            break;
                        }
                        default:
                            throw new UnreachableException($"Invalid feedback property of device scheme: {scheme.Feedback}");
                    }
                }
                break;
            }
            case WebSocketClientKind.Remote:
            {
                break;
            }
            case WebSocketClientKind.Unknown:
                return;
            default:
                throw new InvalidOperationException($"Invalid client kind");
        }
    }

    public void Dispose()
    {
        OnDisposing.Invoke();
    }
}
