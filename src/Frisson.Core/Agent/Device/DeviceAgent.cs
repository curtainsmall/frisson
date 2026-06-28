namespace Frisson.Core.Agent.Device;

public sealed class DeviceAgent : Agent
{
    // Id is the device ID (extracted from bind reply targetId)
    public int StrengthA { get; set; }
    public int StrengthB { get; set; }
    public int MaxA { get; set; } = 100;
    public int MaxB { get; set; } = 100;

    /// <summary>
    /// Fired when device reports updated state (StrengthStatus from APP).
    /// </summary>
    public event Action? StateUpdated;

    public DeviceAgent(Guid id, Action? onDisposing = null) : base(id, onDisposing) { }

    public override async Task HandleMessage(string json)
    {
        var scheme = Scheme.Scheme.Parse(json);
        if (scheme is Scheme.Device.MsgScheme msg)
        {
            switch (msg.Kind)
            {
                case Scheme.Device.MsgKind.StrengthStatus:
                    StrengthA = msg.StrengthA;
                    StrengthB = msg.StrengthB;
                    MaxA = msg.MaxA;
                    MaxB = msg.MaxB;
                    StateUpdated?.Invoke();
                    break;
                case Scheme.Device.MsgKind.Feedback:
                    // Future: fire event to notify UI
                    break;
            }
        }
        await Task.CompletedTask;
    }
}
