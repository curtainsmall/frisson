using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

using CoyoteStudio.Core.Networking.Client.Scheme;
using CoyoteStudio.Core.Networking.Server;

namespace CoyoteStudio.Core.Networking.Client;

internal enum DeviceFeedbackKind
{
    None,
    Circle,
    Triangle,
    Square,
    Pentagram,
    Hexagon,
}

internal class FeedbackTriggeredEventArgs : EventArgs
{
    public DeviceFeedbackKind FeedbackKind { get; init; }

    public FeedbackTriggeredEventArgs(DeviceFeedbackKind feedbackKind)
    {
        FeedbackKind = feedbackKind;
    }
}

internal class DeviceWebSocketClient : WebSocketClient
{
    public event EventHandler? FeedbackTriggered;

    internal sealed class ChannelData
    {
        public int Strength { get; set; } = 0;
        public int Limit { get; set; } = 100;
        public DeviceFeedbackKind FeedbackKind { get; set; } = DeviceFeedbackKind.None;
    }

    public ChannelData ChannelA { get; set; } = new();
    public ChannelData ChannelB { get; set; } = new();

    public DeviceWebSocketClient(Guid id, Action? onDisposing) : base(id, onDisposing)
    {
    }

    public override void Setup(ConnectionData data)
    {
        base.Setup(data);

        var jsonDoc = JsonDocument.Parse(data.Message);
        if (jsonDoc is null)
            return;

        var scheme = new DeviceProtocolScheme(jsonDoc);
        if (scheme.Strength is not null)
        {
            ChannelA.Strength = scheme.Strength.StrengthA;
            ChannelB.Strength = scheme.Strength.StrengthB;
            ChannelA.Limit = scheme.Strength.limitA;
            ChannelB.Limit = scheme.Strength.limitB;
        }
        else if (scheme.Feedback is not null)
        {
            switch (scheme.Feedback)
            {
                case >= 0 and <= 4:
                {
                    ChannelA.FeedbackKind = (DeviceFeedbackKind)(scheme.Feedback + 1);
                    FeedbackTriggered?.Invoke(this, new FeedbackTriggeredEventArgs(ChannelA.FeedbackKind));
                    break;
                }
                case >= 5 and <= 9:
                {
                    ChannelB.FeedbackKind = (DeviceFeedbackKind)(scheme.Feedback - 5 + 1);
                    FeedbackTriggered?.Invoke(this, new FeedbackTriggeredEventArgs(ChannelB.FeedbackKind));
                    break;
                }
                default:
                    throw new UnreachableException($"Invalid feedback property of device scheme: {scheme.Feedback}");
            }
        }
    }

}
