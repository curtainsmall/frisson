namespace Frisson.Core.Agent.Control;

public sealed class ControlSourceAgent : Agent
{
    public string SourceName { get; }
    public Action<string>? ForwardToControlDesk { get; set; }

    public ControlSourceAgent(Guid id, string sourceName, Action? onDisposing = null) : base(id, onDisposing)
    {
        SourceName = sourceName;
    }

    public override async Task HandleMessage(string json)
    {
        ForwardToControlDesk?.Invoke(json);
        await Task.CompletedTask;
    }
}
