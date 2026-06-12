using System.Collections.Concurrent;

using Frisson.Core.Networking.WebSocket;

namespace Frisson.Core.Agent;

public enum AgentKind { Pending, Device, Control }

public sealed class AgentConnectionEventArgs : EventArgs
{
    public Guid AgentId { get; }
    public AgentConnectionStatus Status { get; }
    public Type AgentType { get; }
    public AgentKind Kind { get; }

    public AgentConnectionEventArgs(Guid agentId, AgentConnectionStatus status, Type agentType, AgentKind kind)
    {
        AgentId = agentId;
        Status = status;
        AgentType = agentType;
        Kind = kind;
    }
}

internal class AgentManager
{
    private readonly ConcurrentDictionary<Guid, Agent> _agents = new();
    private readonly ConcurrentDictionary<Guid, Guid> _agentLinks = new(); // DeviceAgentId → ControlAgentId

    public event EventHandler<AgentConnectionEventArgs>? AgentConnected;
    public event EventHandler<AgentConnectionEventArgs>? AgentDisconnected;
    public event Action<Guid, Guid>? AgentLinked;   // (deviceId, controlId)
    public event Action<Guid>? AgentUnlinked;        // deviceId

    public void CreateAgent(AgentCreationArgs conn)
    {
        var agent = new Agent(conn.Id, onDisposing: conn.CloseAction);

        // Hot path direct wire: Agent → transport
        agent.SendFunc = conn.SendFunc;

        // Hot path direct wire: transport → Agent
        conn.SetMessageHandler(json => agent.HandleMessage(json));

        // Meta flow: re-wire on Agent upgrade
        agent.OnBound += type =>
        {
            var upgraded = (Agent)Activator.CreateInstance(type, agent)!;
            conn.SetMessageHandler(json => upgraded.HandleMessage(json));
            UpgradeAgent(conn.Id, upgraded);
        };

        _agents.TryAdd(conn.Id, agent);
        _ = agent.HandleMessage(string.Empty); // trigger initial bind
    }

    private static AgentKind GetKind(Type type) => type.Name switch
    {
        nameof(AgentKind.Device) + "Agent" => AgentKind.Device,
        _ when type.Name.Contains("Control") => AgentKind.Control,
        _ => AgentKind.Pending,
    };

    public void RemoveAgent(Guid id)
    {
        if (_agents.TryRemove(id, out var agent))
        {
            // Remove any links involving this agent
            _agentLinks.TryRemove(id, out _);
            foreach (var kvp in _agentLinks.Where(k => k.Value == id).ToList())
                _agentLinks.TryRemove(kvp.Key, out _);

            AgentDisconnected?.Invoke(this, new AgentConnectionEventArgs(id, AgentConnectionStatus.Disconnected, agent.GetType(), GetKind(agent.GetType())));
            agent.Dispose();
        }
    }

    private void UpgradeAgent(Guid id, Agent upgraded)
    {
        _agents.TryRemove(id, out _);
        _agents.TryAdd(id, upgraded);
        AgentConnected?.Invoke(this, new AgentConnectionEventArgs(id, AgentConnectionStatus.Connected, upgraded.GetType(), GetKind(upgraded.GetType())));
    }

    /// <summary>
    /// Links a Device agent to a Control agent.
    /// </summary>
    public void LinkAgents(Guid deviceId, Guid controlId)
    {
        _agentLinks[deviceId] = controlId;
        AgentLinked?.Invoke(deviceId, controlId);
    }

    /// <summary>
    /// Unlinks a Device agent from its Control agent.
    /// </summary>
    public void UnlinkAgent(Guid deviceId)
    {
        if (_agentLinks.TryRemove(deviceId, out _))
            AgentUnlinked?.Invoke(deviceId);
    }

    /// <summary>
    /// Gets all linked Device agent IDs for a given Control agent.
    /// </summary>
    public IReadOnlyList<Guid> GetLinkedDevices(Guid controlId)
    {
        return _agentLinks
            .Where(kvp => kvp.Value == controlId)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>
    /// Gets all current agent links.
    /// </summary>
    public IReadOnlyDictionary<Guid, Guid> GetAgentLinks() => new Dictionary<Guid, Guid>(_agentLinks);
}
