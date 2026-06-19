using System.Collections.Concurrent;

using Frisson.Core.Agent.Control;
using Frisson.Core.Agent.Device;
using Frisson.Core.Frisson;

namespace Frisson.Core.Agent;

public sealed class AgentEventArgs : EventArgs
{
    public Guid AgentId { get; }
    public Type AgentType { get; }

    public AgentEventArgs(Guid agentId, Type agentType)
    {
        AgentId = agentId;
        AgentType = agentType;
    }
}

internal class AgentManager
{
    ControlDesk _desk;
    Action<Guid> _closeClient;
    HashSet<Guid> _removing = new();
    ConcurrentDictionary<Guid, Agent> _agents = new();
    HashSet<Guid> _activeDevices = new();
    Guid? _activeSource;

    public event EventHandler<AgentEventArgs>? AgentConnected;
    public event EventHandler<AgentEventArgs>? AgentClosing;
    public event Action<Guid>? DeviceActivated;
    public event Action<Guid>? DeviceDeactivated;
    public event Action<Guid>? SourceActivated;
    public event Action? SourceDeactivated;

    public AgentManager(ControlDesk desk, Action<Guid> closeClient)
    {
        _desk = desk;
        _closeClient = closeClient;

        // Subscribe to ControlDesk state changes — broadcast to all active devices
        _desk.StateChanged += () =>
        {
            var msg = _desk.ToPulseMessage();
            foreach (var id in _activeDevices)
                if (_agents.TryGetValue(id, out var a) && a is DeviceAgent)
                    a.SendFunc?.Invoke(msg);
        };
    }

    public void AddAgent(Agent agent)
    {
        _agents[agent.Id] = agent;

        if (agent is ControlSourceAgent csa)
            csa.ForwardToControlDesk = _desk.ApplyFromSource;

        AgentConnected?.Invoke(this, new AgentEventArgs(agent.Id, agent.GetType()));
    }

    /// <summary>
    /// Remove agent with re-entrancy guard. AgentClosing fires before removal so
    /// listeners can read the agent's final state.
    /// </summary>
    public void RemoveAgent(Guid id)
    {
        if (_removing.Contains(id)) return;
        _removing.Add(id);
        try
        {
            if (_agents.TryGetValue(id, out var agent))
            {
                AgentClosing?.Invoke(this, new AgentEventArgs(id, agent.GetType()));
                _agents.TryRemove(id, out _);
                _activeDevices.Remove(id);
                if (_activeSource == id) _activeSource = null;
                agent.Dispose();
                _closeClient?.Invoke(id);
            }
        }
        finally { _removing.Remove(id); }
    }

    public void ActivateDevice(Guid id)
    {
        _activeDevices.Add(id);
        // Immediately sync current ControlDesk state
        if (_agents.TryGetValue(id, out var a) && a is DeviceAgent da)
            da.SendFunc?.Invoke(_desk.ToPulseMessage());
        DeviceActivated?.Invoke(id);
    }

    public void DeactivateDevice(Guid id)
    {
        _activeDevices.Remove(id);
        DeviceDeactivated?.Invoke(id);
    }

    public void SetActiveSource(Guid id)
    {
        _activeSource = id;
        _desk.SetBlocked(true);
        SourceActivated?.Invoke(id);
    }

    public void ClearActiveSource()
    {
        _activeSource = null;
        _desk.SetBlocked(false);
        SourceDeactivated?.Invoke();
    }

    public Agent? Get(Guid id)
    {
        _agents.TryGetValue(id, out var agent);
        return agent;
    }
}
