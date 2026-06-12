namespace Frisson.Core.Agent;

using DeviceAgent = Frisson.Core.Agent.Device.DeviceAgent;
using RemoteControlAgent = Frisson.Core.Agent.Control.RemoteControlAgent;

public enum AgentConnectionStatus { Connected, Disconnected, Pending }

internal class Agent : IDisposable
{
    public Guid Id { get; protected set; }
    public Action? OnDisposing { get; set; }
    public Func<string, Task<bool>>? SendFunc { get; set; }

    // Fired after bind success, passes the target agent type
    public event Action<Type>? OnBound;

    public Agent(Guid id, Action? onDisposing = null)
    {
        Id = id;
        OnDisposing = onDisposing;
    }

    // Copy constructor for subclass upgrades
    protected Agent(Agent existing) : this(existing.Id, existing.OnDisposing)
    {
        SendFunc = existing.SendFunc;
        existing.OnDisposing = null;
    }

    public async Task<bool> SendAsync(string json)
        => SendFunc != null && await SendFunc(json);

    public async Task HandleMessage(string json)
    {
        if (GetType() == typeof(Agent)) { await HandleBind(json); return; }
        await HandleProtocolMessage(json);
    }

    private async Task HandleBind(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            // First connection — send initial bind
            var initialBind = new Scheme.Device.BindScheme
            {
                ClientId = Id,
                TargetId = Guid.Empty,
                Message = "targetId"
            };
            await SendAsync(initialBind.ToJson());
            return;
        }

        var scheme = Scheme.Scheme.Parse(json);
        if (scheme is Scheme.Device.BindScheme deviceBind)
        {
            var ack = new Scheme.Device.BindScheme
            {
                ClientId = AppCore.DummyFrontendId,
                TargetId = deviceBind.TargetId,
                Message = "200"
            };
            await SendAsync(ack.ToJson());
            OnBound?.Invoke(typeof(DeviceAgent));
        }
        else if (scheme is Scheme.Control.BindScheme controlBind)
        {
            var ack = new Scheme.Control.BindScheme { Id = controlBind.Id, Name = controlBind.Name };
            await SendAsync(ack.ToJson());
            OnBound?.Invoke(typeof(RemoteControlAgent));
        }
    }

    protected virtual Task HandleProtocolMessage(string json) => Task.CompletedTask;

    public void Dispose()
    {
        OnDisposing?.Invoke();
        OnDisposing = null;
    }
}
