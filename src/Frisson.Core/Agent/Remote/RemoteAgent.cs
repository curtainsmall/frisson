using Frisson.Core.Scheme;

namespace Frisson.Core.Agent.Remote;

public sealed class RemoteAgent : Agent
{
    public string Name { get; }
    public bool AlwaysReply { get; set; }
    public Action<Scheme.Scheme>? ForwardToControlDesk { get; set; }

    public RemoteAgent(Guid id, string name, Action? onDisposing = null) : base(id, onDisposing)
    {
        Name = name;
    }

    public override async Task HandleMessage(string json)
    {
        var scheme = Scheme.Scheme.Parse(json);
        if (scheme == null)
        {
            SendFunc?.Invoke("{\"type\":\"error\",\"message\":\"Invalid\"}");
            return;
        }

        ForwardToControlDesk?.Invoke(scheme);
        await Task.CompletedTask;
    }
}
