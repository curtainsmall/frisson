namespace Frisson.Core.Agent.Control;

internal class ControlAgent : Agent
{
    public ControlAgent(Agent existing) : base(existing) { }

    protected override async Task HandleProtocolMessage(string json)
    {
        var scheme = Scheme.Scheme.Parse(json);
        // Control-specific protocol
    }
}
