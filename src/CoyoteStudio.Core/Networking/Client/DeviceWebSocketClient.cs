using System.Diagnostics;
using System.Text.Json;

using CoyoteStudio.Core.Networking.Client.Scheme;

namespace CoyoteStudio.Core.Networking.Client;

internal enum DeviceFeedbackKind
{
    Circle,
    Triangle,
    Square,
    Pentagram,
    Hexagon,
}

internal enum DeviceChannelKind
{
    A,
    B,
}

internal class FeedbackTriggeredEventArgs : EventArgs
{
    public DeviceChannelKind ChannelKind { get; init; }
    public DeviceFeedbackKind FeedbackKind { get; init; }

    public FeedbackTriggeredEventArgs(DeviceChannelKind channelKind, DeviceFeedbackKind feedbackKind)
    {
        ChannelKind = channelKind;
        FeedbackKind = feedbackKind;
    }

    public FeedbackTriggeredEventArgs(int feedbackRaw)
    {
        switch (feedbackRaw)
        {
            case >= 0 and <= 4:
            {
                ChannelKind = DeviceChannelKind.A;
                FeedbackKind = (DeviceFeedbackKind)feedbackRaw;
                break;
            }
            case >= 5 and <= 9:
            {
                ChannelKind = DeviceChannelKind.B;
                FeedbackKind = (DeviceFeedbackKind)feedbackRaw - 5;
                break;
            }
            default:
                throw new UnreachableException($"Invalid feedback property of device scheme: {feedbackRaw}");
        }
    }
}
internal sealed class DeviceChannelData
{
    public int Strength { get; set; } = 0;
    public int Limit { get; set; } = 100;
}

internal class DeviceWebSocketClient : WebSocketClient
{
    public event EventHandler<FeedbackTriggeredEventArgs>? FeedbackTriggered;

    public DeviceChannelData ChannelA { get; private set; } = new();
    public DeviceChannelData ChannelB { get; private set; } = new();

    public DeviceWebSocketClient(Guid id, Action? onDisposing) : base(id, onDisposing)
    {
    }

    public override void Setup(string jsonString)
    {
        using var jsonDoc = JsonDocument.Parse(jsonString);

        var scheme = new DeviceInputProtocolScheme(jsonDoc);

        if (scheme.Strength is not null)
        {
            ChannelA.Strength = scheme.Strength.StrengthA;
            ChannelB.Strength = scheme.Strength.StrengthB;
            ChannelA.Limit = scheme.Strength.limitA;
            ChannelB.Limit = scheme.Strength.limitB;
        }
        else if (scheme.Feedback is not null)
        {
            FeedbackTriggered?.Invoke(this, new FeedbackTriggeredEventArgs(scheme.Feedback.Value));
        }
    }

}
