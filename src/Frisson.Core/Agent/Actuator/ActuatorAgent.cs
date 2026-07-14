namespace Frisson.Core.Agent.Actuator;

public sealed class ActuatorAgent : Agent
{
    // Id is the actuator ID (extracted from bind reply targetId)
    public int StrengthA { get; set; }
    public int StrengthB { get; set; }
    public int MaxA { get; set; } = 100;
    public int MaxB { get; set; } = 100;

    /// <summary>
    /// Fired when actuator reports updated state (StrengthStatus from APP).
    /// </summary>
    public event Action? StateUpdated;

    public ActuatorAgent(Guid id, Action? onDisposing = null) : base(id, onDisposing) { }

    public override async Task HandleMessage(string json)
    {
        var scheme = Scheme.Scheme.Parse(json);
        if (scheme is Scheme.Actuator.MsgScheme msg)
        {
            switch (msg.Kind)
            {
                case Scheme.Actuator.MsgKind.StrengthStatus:
                    StrengthA = msg.StrengthA;
                    StrengthB = msg.StrengthB;
                    MaxA = msg.MaxA;
                    MaxB = msg.MaxB;
                    StateUpdated?.Invoke();
                    break;
                case Scheme.Actuator.MsgKind.Feedback:
                    // Future: fire event to notify UI
                    break;
            }
        }
        await Task.CompletedTask;
    }
}
