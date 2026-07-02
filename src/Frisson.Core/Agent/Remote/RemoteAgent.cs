namespace Frisson.Core.Agent.Remote;

public sealed class RemoteAgent : Agent
{
    public string Name { get; }
    public Action<string>? ForwardToControlDesk { get; set; }

    public RemoteAgent(Guid id, string name, Action? onDisposing = null) : base(id, onDisposing)
    {
        Name = name;
    }

    public override async Task HandleMessage(string json)
    {
        ForwardToControlDesk?.Invoke(json);
        await Task.CompletedTask;
    }
}
