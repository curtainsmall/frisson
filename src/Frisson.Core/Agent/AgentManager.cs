using System.Collections.Concurrent;

using Frisson.Core.Networking.WebSocket;

namespace Frisson.Core.Agent;

public sealed class AgentConnectionEventArgs : EventArgs
{
    public Guid AgentId { get; }
    public AgentConnectionStatus Status { get; }

    public AgentConnectionEventArgs(Guid agentId, AgentConnectionStatus status)
    {
        AgentId = agentId;
        Status = status;
    }
}

internal class AgentManager
{
    private readonly ConcurrentDictionary<Guid, Agent> _agents = new();

    public event EventHandler<AgentConnectionEventArgs>? AgentConnected;
    public event EventHandler<AgentConnectionEventArgs>? AgentDisconnected;

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

    public void RemoveAgent(Guid id)
    {
        if (_agents.TryRemove(id, out var agent))
        {
            AgentDisconnected?.Invoke(this, new AgentConnectionEventArgs(id, AgentConnectionStatus.Disconnected));
            agent.Dispose();
        }
    }

    private void UpgradeAgent(Guid id, Agent upgraded)
    {
        _agents.TryRemove(id, out _);
        _agents.TryAdd(id, upgraded);
        AgentConnected?.Invoke(this, new AgentConnectionEventArgs(id, AgentConnectionStatus.Connected));
    }
}
