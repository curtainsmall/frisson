namespace Frisson.Core.Agent.Device;

public sealed class DeviceAgent : Agent
{
    // Id is the device ID (extracted from bind reply targetId)
    public int StrengthA { get; set; }
    public int StrengthB { get; set; }
    public int MaxA { get; set; }
    public int MaxB { get; set; }

    public DeviceAgent(Guid id, Action? onDisposing = null) : base(id, onDisposing) { }

    public override async Task HandleMessage(string json)
    {
        var scheme = Scheme.Scheme.Parse(json);
        if (scheme is Scheme.Device.MsgScheme msg)
        {
            // Parse feedback/status from the message field
        }
        await Task.CompletedTask;
    }
}
