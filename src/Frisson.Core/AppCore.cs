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

    private readonly WebSocketServer _wsServer = new();
    private readonly AgentManager _agentManager = new();

    public event Action<string>? ErrorOccurred;
    public event EventHandler<AgentConnectionEventArgs>? AgentConnected;
    public event EventHandler<AgentConnectionEventArgs>? AgentDisconnected;

    public ErrorMessager ErrorMessager { get; private init; } = new();

    private AppCore()
    {
        // Bridge transport events to AgentManager
        _wsServer.ClientConnected += conn => _agentManager.CreateAgent(conn);
        _wsServer.ClientDisconnected += id => _agentManager.RemoveAgent(id);

        // Forward AgentManager events to AppCore consumers
        _agentManager.AgentConnected += (s, e) => AgentConnected?.Invoke(s, e);
        _agentManager.AgentDisconnected += (s, e) => AgentDisconnected?.Invoke(s, e);
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

    /// <summary>
    /// Disconnects a specific agent by ID (closes the underlying transport).
    /// </summary>
    public void DisconnectAgent(Guid agentId)
    {
        _wsServer.Close(agentId);
    }
}
