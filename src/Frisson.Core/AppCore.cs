using Frisson.Core.Agent;
using Frisson.Core.Error;
using Frisson.Core.Frisson;
using Frisson.Core.Networking.WebSocket;

namespace Frisson.Core;

public class AppCore : IDisposable
{
    private static AppCore? _instance;
    public static AppCore Instance => _instance ??= new AppCore();

    /// <summary>
    /// Per-instance dummy frontend UUID. Represents "Frisson as frontend" for DG-LAB Device binding.
    /// </summary>
    public static Guid DummyFrontendId { get; } = Guid.NewGuid();

    private readonly ControlDesk _controlDesk;
    private readonly WebSocketServer _wsServer;
    private readonly AgentManager _agentManager;

    public event Action<string>? ErrorOccurred;

    // Forwarded from AgentManager
    public event EventHandler<AgentEventArgs>? AgentConnected;
    public event EventHandler<AgentEventArgs>? AgentClosing;
    public event Action<Guid>? DeviceActivated;
    public event Action<Guid>? DeviceDeactivated;
    public event Action<Guid>? SourceActivated;
    public event Action? SourceDeactivated;

    public ErrorMessager ErrorMessager { get; private init; } = new();

    private AppCore()
    {
        _controlDesk = new ControlDesk();
        _wsServer = new WebSocketServer();
        _agentManager = new AgentManager(_controlDesk, id => _wsServer.TryRemove(id));

        // WebSocketServer → AgentManager
        _wsServer.AgentCreated += agent => _agentManager.AddAgent(agent);
        _wsServer.ClientDisconnected += id => _agentManager.RemoveAgent(id);

        // Forward AgentManager events to AppCore consumers
        _agentManager.AgentConnected += (_, e) => AgentConnected?.Invoke(this, e);
        _agentManager.AgentClosing += (_, e) => AgentClosing?.Invoke(this, e);
        _agentManager.DeviceActivated += id => DeviceActivated?.Invoke(id);
        _agentManager.DeviceDeactivated += id => DeviceDeactivated?.Invoke(id);
        _agentManager.SourceActivated += id => SourceActivated?.Invoke(id);
        _agentManager.SourceDeactivated += () => SourceDeactivated?.Invoke();
    }

    public void Startup(int port)
    {
        try
        {
            _wsServer.Start(port);
        }
        catch (Exception e)
        {
            ErrorMessager.Send(new ErrorMessage(ErrorCode.Unknown, e.Message));
        }
    }

    public void Dispose()
    {
        ErrorMessager.Dispose();
        _wsServer.Dispose();
    }

    public void DisconnectAgent(Guid agentId)
    {
        _wsServer.TryRemove(agentId);
    }

    public void ActivateDevice(Guid id) => _agentManager.ActivateDevice(id);
    public void DeactivateDevice(Guid id) => _agentManager.DeactivateDevice(id);
    public void SetActiveSource(Guid id) => _agentManager.SetActiveSource(id);
    public void ClearActiveSource() => _agentManager.ClearActiveSource();
    public Agent.Agent? GetAgent(Guid id) => _agentManager.GetAgent(id);
}
