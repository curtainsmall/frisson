namespace Frisson.Core.Agent;

public abstract class Agent : IDisposable
{
    public Guid Id { get; }
    public Func<string, Task<bool>>? SendFunc { get; set; }
    Action? _onDisposing;

    protected Agent(Guid id, Action? onDisposing = null)
    {
        Id = id;
        _onDisposing = onDisposing;
    }

    public abstract Task HandleMessage(string json);

    public void Dispose()
    {
        _onDisposing?.Invoke();
        _onDisposing = null;
    }
}
