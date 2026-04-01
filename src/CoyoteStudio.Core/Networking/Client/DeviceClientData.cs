namespace CoyoteStudio.Core.Networking.Client;

internal enum DeviceFeedbackKind
{
    None, A, B, C, D, E
}

internal sealed class DeviceClientData : ClientData
{
    internal sealed class ChannelData
    {
        public int Strength { get; set; } = 0;
        public int Limit { get; set; } = 100;
        public DeviceFeedbackKind FeedbackKind { get; set; } = DeviceFeedbackKind.None;
    }

    public ChannelData ChannelA { get; set; } = new();
    public ChannelData ChannelB { get; set; } = new();

    public DeviceClientData()
    {
    }

}
