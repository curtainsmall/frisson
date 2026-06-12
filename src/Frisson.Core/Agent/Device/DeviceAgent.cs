namespace Frisson.Core.Agent.Device;

internal sealed class DeviceAgent : Agent
{
    public DeviceAgent(Agent existing) : base(existing) { }

    protected override async Task HandleProtocolMessage(string json)
    {
        var scheme = Scheme.Scheme.Parse(json);
        // Device-specific protocol (msg etc.)
    }
}
