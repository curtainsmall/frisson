using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

using Frisson.Core.Agent.Remote;
using Frisson.Core.Agent.Actuator;

using Frisson.Core.Scheme;

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
    private static readonly string AckJson = JsonSerializer.Serialize(new { type = "ack" });
    private static readonly string InactiveErrorJson = JsonSerializer.Serialize(new { type = "error", message = "Inactive" });
    private static readonly string ActiveJson = JsonSerializer.Serialize(new { type = "activated" });
    private static readonly string DeactiveJson = JsonSerializer.Serialize(new { type = "deactivated" });

    ControlDesk _desk;
    Action<Guid> _closeClient;
    HashSet<Guid> _removing = new();
    ConcurrentDictionary<Guid, Agent> _agents = new();
    HashSet<Guid> _activeActuators = new();
    Guid? _activeRemote;

    public event EventHandler<AgentEventArgs>? AgentConnected;
    public event EventHandler<AgentEventArgs>? AgentClosing;
    public event Action<Guid>? ActuatorActivated;
    public event Action<Guid>? ActuatorDeactivated;
    public event Action<Guid>? RemoteActivated;
    public event Action? RemoteDeactivated;
    public event Action<Guid>? ActuatorStateUpdated;

    public AgentManager(ControlDesk desk, Action<Guid> closeClient)
    {
        _desk = desk;
        _closeClient = closeClient;

        // Subscribe to ControlDesk state changes — broadcast to all active devices
        _desk.StateChanged += () =>
        {
            var msg = _desk.ToStrengthMessage();
            foreach (var id in _activeActuators)
                if (_agents.TryGetValue(id, out var a) && a is ActuatorAgent)
                    a.SendFunc?.Invoke(msg);
        };
    }

    public void AddAgent(Agent agent)
    {
        _agents[agent.Id] = agent;

        if (agent is RemoteAgent remote)
            remote.ForwardToControlDesk = scheme =>
            {
                // 1. Only the active Remote may write; all others get Inactive error
                if (_activeRemote != remote.Id)
                {
                    remote.SendFunc?.Invoke(InactiveErrorJson);
                    return;
                }

                // 2. Apply write; reply state if changed, ack if alwaysReply, else silence
                var reply = _desk.ApplyFromRemote(scheme);
                if (reply != null)
                {
                    remote.SendFunc?.Invoke(reply.ToJsonString());
                }
                else if (remote.AlwaysReply)
                {
                    remote.SendFunc?.Invoke(AckJson);
                }
            };

        if (agent is ActuatorAgent da)
        {
            da.StateUpdated += () => ActuatorStateUpdated?.Invoke(da.Id);
            // Auto-activate: add to active set and sync current ControlDesk state
            _activeActuators.Add(da.Id);
            da.SendFunc?.Invoke(_desk.ToStrengthMessage());
        }

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
                if (_activeRemote == id) ClearActiveRemote();
                _agents.TryRemove(id, out _);
                _activeActuators.Remove(id);
                agent.Dispose();
                _closeClient?.Invoke(id);
            }
        }
        finally { _removing.Remove(id); }
    }

    public void ActivateActuator(Guid id)
    {
        _activeActuators.Add(id);
        // Immediately sync current ControlDesk state
        if (_agents.TryGetValue(id, out var a) && a is ActuatorAgent da)
            da.SendFunc?.Invoke(_desk.ToStrengthMessage());
        ActuatorActivated?.Invoke(id);
    }

    public void DeactivateActuator(Guid id)
    {
        _activeActuators.Remove(id);
        ActuatorDeactivated?.Invoke(id);
    }

    public void SetActiveRemote(Guid id)
    {
        _activeRemote = id;
        _desk.SetBlocked(true);
        SendToRemote(id, ActiveJson);
        RemoteActivated?.Invoke(id);
    }

    public void ClearActiveRemote()
    {
        var prevId = _activeRemote;
        _activeRemote = null;
        _desk.SetBlocked(false);
        if (prevId != null)
            SendToRemote(prevId.Value, DeactiveJson);
        RemoteDeactivated?.Invoke();
    }

    private void SendToRemote(Guid id, string json)
    {
        if (_agents.TryGetValue(id, out var a) && a is RemoteAgent r)
            r.SendFunc?.Invoke(json);
    }

    public Agent? GetAgent(Guid id)
    {
        _agents.TryGetValue(id, out var agent);
        return agent;
    }

    public string? GetActiveRemoteName()
    {
        if (_activeRemote == null) return null;
        if (_agents.TryGetValue(_activeRemote.Value, out var a) && a is RemoteAgent r)
            return r.Name;
        return null;
    }

    public Guid? GetActiveRemoteId() => _activeRemote;
}
