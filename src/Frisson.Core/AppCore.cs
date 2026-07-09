using Frisson.Core.Agent;
using Frisson.Core.Error;
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
    public event Action<Guid>? ActuatorActivated;
    public event Action<Guid>? ActuatorDeactivated;
    public event Action<Guid>? RemoteActivated;
    public event Action? RemoteDeactivated;
    public event Action<Guid>? ActuatorStateUpdated;
    public event Action<Guid, string>? RemoteBindingRequested;

    public event Action? ControlDeskStateChanged;

    public ErrorMessager ErrorMessager { get; private init; } = new();

    private AppCore()
    {
        _controlDesk = new ControlDesk();
        _wsServer = new WebSocketServer();
        _agentManager = new AgentManager(_controlDesk, id => _wsServer.TryRemove(id));

        // Apply persisted settings (caller provides code defaults)
        var s = SettingsService.Instance;
        _controlDesk.SetMax(s.TryGet("maxA", out int ma) ? ma : 100, s.TryGet("maxB", out int mb) ? mb : 100);

        // WebSocketServer → AgentManager
        _wsServer.AgentCreated += agent => _agentManager.AddAgent(agent);
        _wsServer.ClientDisconnected += id => _agentManager.RemoveAgent(id);

        // WebSocketServer → forward Remote bind requests to UI
        _wsServer.RemoteBindingRequested += (id, name) =>
            RemoteBindingRequested?.Invoke(id, name);

        // Forward AgentManager events to AppCore consumers
        _agentManager.AgentConnected += (_, e) => AgentConnected?.Invoke(this, e);
        _agentManager.AgentClosing += (_, e) => AgentClosing?.Invoke(this, e);
        _agentManager.ActuatorActivated += id => ActuatorActivated?.Invoke(id);
        _agentManager.ActuatorDeactivated += id => ActuatorDeactivated?.Invoke(id);
        _agentManager.RemoteActivated += id => RemoteActivated?.Invoke(id);
        _agentManager.RemoteDeactivated += () => RemoteDeactivated?.Invoke();
        _agentManager.ActuatorStateUpdated += id => ActuatorStateUpdated?.Invoke(id);

        // Forward ControlDesk state changes to UI layer
        _controlDesk.StateChanged += () => ControlDeskStateChanged?.Invoke();
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

    public void ActivateActuator(Guid id) => _agentManager.ActivateActuator(id);
    public void DeactivateActuator(Guid id) => _agentManager.DeactivateActuator(id);
    public void AcceptRemote(Guid clientId) => _wsServer.AcceptRemote(clientId);
    public void RejectRemote(Guid clientId) => _wsServer.RejectRemote(clientId);

    public void SetActiveRemote(Guid id) => _agentManager.SetActiveRemote(id);
    public void ClearActiveRemote() => _agentManager.ClearActiveRemote();
    public Agent.Agent? GetAgent(Guid id) => _agentManager.GetAgent(id);

    public int GetControlDeskStrengthA() => _controlDesk.StrengthA;
    public int GetControlDeskStrengthB() => _controlDesk.StrengthB;
    public void SetControlDeskStrength(int a, int b) => _controlDesk.SetLocalStrength(a, b);
    public int GetControlDeskMaxA() => _controlDesk.MaxA;
    public int GetControlDeskMaxB() => _controlDesk.MaxB;
    public void SetControlDeskMax(int a, int b) => _controlDesk.SetMax(a, b);
    public bool GetControlDeskIsBlocked() => _controlDesk.IsBlocked;
    public string? GetActiveRemoteName() => _agentManager.GetActiveRemoteName();
    public Guid? GetActiveRemoteId() => _agentManager.GetActiveRemoteId();
}
