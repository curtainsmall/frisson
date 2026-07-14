namespace Frisson.Core.Tests.Agent;

public class AgentManagerTest
{
    // -- Fixture helper --

    private static (AgentManager mgr, Core.ControlDesk desk, List<string> sent) CreateFixture()
    {
        var desk = new Core.ControlDesk();
        var sent = new List<string>();
        var mgr = new AgentManager(desk, _ => { });
        return (mgr, desk, sent);
    }

    private static ActuatorAgent CreateActuatorSpy(List<string> sent)
    {
        return new ActuatorAgent(Guid.NewGuid())
        {
            SendFunc = msg => { sent.Add(msg); return Task.FromResult(true); }
        };
    }

    // -- AddAgent: ActuatorAgent --

    [Fact]
    public void AddAgent_Actuator_AutoActivatedAndSyncsState()
    {
        var (mgr, desk, sent) = CreateFixture();
        desk.SetLocalStrength(30, 40);

        var agent = CreateActuatorSpy(sent);
        mgr.AddAgent(agent);

        // Should send initial state for both channels
        Assert.Contains(sent, m => m.Contains("strength-1+2+30"));
        Assert.Contains(sent, m => m.Contains("strength-2+2+40"));
    }

    [Fact]
    public void AddAgent_Actuator_FiresAgentConnected()
    {
        var (mgr, _, _) = CreateFixture();
        AgentEventArgs? args = null;
        mgr.AgentConnected += (_, e) => args = e;

        var agent = CreateActuatorSpy(new List<string>());
        mgr.AddAgent(agent);

        Assert.NotNull(args);
        Assert.Equal(agent.Id, args.AgentId);
        Assert.Equal(typeof(ActuatorAgent), args.AgentType);
    }

    [Fact]
    public void AddAgent_Actuator_StateUpdatedFiresActuatorStateUpdated()
    {
        var (mgr, _, _) = CreateFixture();
        Guid? updatedId = null;
        mgr.ActuatorStateUpdated += id => updatedId = id;

        var agent = CreateActuatorSpy(new List<string>());
        mgr.AddAgent(agent);

        // Trigger state update on the agent
        var json = """{"type":"msg","clientId":"c","targetId":"t","message":"strength-10+20+100+200"}""";
        agent.HandleMessage(json);

        Assert.Equal(agent.Id, updatedId);
    }

    // -- AddAgent: RemoteAgent --

    [Fact]
    public void AddAgent_Remote_FiresAgentConnected()
    {
        var (mgr, _, _) = CreateFixture();
        AgentEventArgs? args = null;
        mgr.AgentConnected += (_, e) => args = e;

        var agent = new RemoteAgent(Guid.NewGuid(), "test");
        mgr.AddAgent(agent);

        Assert.NotNull(args);
        Assert.Equal(agent.Id, args.AgentId);
        Assert.Equal(typeof(RemoteAgent), args.AgentType);
    }

    // -- RemoveAgent --

    [Fact]
    public void RemoveAgent_FiresAgentClosing()
    {
        var (mgr, _, _) = CreateFixture();
        var agent = CreateActuatorSpy(new List<string>());
        mgr.AddAgent(agent);

        AgentEventArgs? args = null;
        mgr.AgentClosing += (_, e) => args = e;

        mgr.RemoveAgent(agent.Id);
        Assert.NotNull(args);
        Assert.Equal(agent.Id, args.AgentId);
    }

    [Fact]
    public void RemoveAgent_ReEntrancyGuard_PreventsDoubleRemove()
    {
        var (mgr, _, _) = CreateFixture();
        var agent = CreateActuatorSpy(new List<string>());
        mgr.AddAgent(agent);

        int closeCount = 0;
        mgr.AgentClosing += (_, _) =>
        {
            closeCount++;
            // Attempt re-entrant call from within event handler
            mgr.RemoveAgent(agent.Id);
        };

        mgr.RemoveAgent(agent.Id);
        Assert.Equal(1, closeCount);
    }

    [Fact]
    public void RemoveAgent_Remote_ClearsActiveRemote()
    {
        var (mgr, desk, _) = CreateFixture();
        var remote = new RemoteAgent(Guid.NewGuid(), "test") { SendFunc = _ => Task.FromResult(true) };
        mgr.AddAgent(remote);
        mgr.SetActiveRemote(remote.Id);
        Assert.True(desk.IsBlocked);

        mgr.RemoveAgent(remote.Id);
        Assert.False(desk.IsBlocked);
    }

    // -- ActivateActuator / DeactivateActuator --

    [Fact]
    public void DeactivateActuator_FiresEvent()
    {
        var (mgr, _, _) = CreateFixture();
        var agent = CreateActuatorSpy(new List<string>());
        mgr.AddAgent(agent);

        Guid? deactivatedId = null;
        mgr.ActuatorDeactivated += id => deactivatedId = id;

        mgr.DeactivateActuator(agent.Id);
        Assert.Equal(agent.Id, deactivatedId);
    }

    // -- SetActiveRemote / ClearActiveRemote --

    [Fact]
    public void SetActiveRemote_BlocksDeskAndSendsActivated()
    {
        var (mgr, desk, sent) = CreateFixture();
        var remote = new RemoteAgent(Guid.NewGuid(), "test")
        {
            SendFunc = msg => { sent.Add(msg); return Task.FromResult(true); }
        };
        mgr.AddAgent(remote);

        Guid? activatedId = null;
        mgr.RemoteActivated += id => activatedId = id;

        mgr.SetActiveRemote(remote.Id);

        Assert.True(desk.IsBlocked);
        Assert.Contains(sent, m => m.Contains("\"activated\""));
        Assert.Equal(remote.Id, activatedId);
    }

    [Fact]
    public void ClearActiveRemote_UnblocksDeskAndSendsDeactivated()
    {
        var (mgr, desk, sent) = CreateFixture();
        var remote = new RemoteAgent(Guid.NewGuid(), "test")
        {
            SendFunc = msg => { sent.Add(msg); return Task.FromResult(true); }
        };
        mgr.AddAgent(remote);
        mgr.SetActiveRemote(remote.Id);

        bool deactivatedFired = false;
        mgr.RemoteDeactivated += () => deactivatedFired = true;

        mgr.ClearActiveRemote();

        Assert.False(desk.IsBlocked);
        Assert.Contains(sent, m => m.Contains("\"deactivated\""));
        Assert.True(deactivatedFired);
    }

    // -- StateChanged broadcast --

    [Fact]
    public void StateChanged_ChangedA_SendsOnlyChannelA()
    {
        var (mgr, desk, sent) = CreateFixture();
        var agent = CreateActuatorSpy(sent);
        mgr.AddAgent(agent);

        // Initial state: 0, 0. Change only A.
        sent.Clear();
        desk.SetLocalStrength(50, 0);

        Assert.Contains(sent, m => m.Contains("strength-1+2+50"));
        Assert.DoesNotContain(sent, m => m.Contains("strength-2"));
    }

    [Fact]
    public void StateChanged_ChangedB_SendsOnlyChannelB()
    {
        var (mgr, desk, sent) = CreateFixture();
        var agent = CreateActuatorSpy(sent);
        mgr.AddAgent(agent);

        sent.Clear();
        desk.SetLocalStrength(0, 60);

        Assert.DoesNotContain(sent, m => m.Contains("strength-1"));
        Assert.Contains(sent, m => m.Contains("strength-2+2+60"));
    }

    [Fact]
    public void StateChanged_ChangedBoth_SendsBothChannels()
    {
        var (mgr, desk, sent) = CreateFixture();
        var agent = CreateActuatorSpy(sent);
        mgr.AddAgent(agent);

        sent.Clear();
        desk.SetLocalStrength(30, 40);

        Assert.Contains(sent, m => m.Contains("strength-1+2+30"));
        Assert.Contains(sent, m => m.Contains("strength-2+2+40"));
    }

    [Fact]
    public void StateChanged_NoChange_SendsNothing()
    {
        var (mgr, desk, sent) = CreateFixture();
        var agent = CreateActuatorSpy(sent);
        mgr.AddAgent(agent);

        sent.Clear();
        desk.SetLocalStrength(0, 0); // already 0, 0

        Assert.Empty(sent);
    }

    [Fact]
    public void StateChanged_OnlySendsToActiveActuators()
    {
        var (mgr, desk, sent) = CreateFixture();
        var agent = CreateActuatorSpy(sent);
        mgr.AddAgent(agent);

        // Deactivate, then change state
        mgr.DeactivateActuator(agent.Id);
        sent.Clear();
        desk.SetLocalStrength(50, 50);

        Assert.Empty(sent);
    }

    // -- GetActiveRemote --

    [Fact]
    public void GetActiveRemote_ReturnsCorrectId()
    {
        var (mgr, _, _) = CreateFixture();
        var remote = new RemoteAgent(Guid.NewGuid(), "test") { SendFunc = _ => Task.FromResult(true) };
        mgr.AddAgent(remote);
        mgr.SetActiveRemote(remote.Id);

        Assert.Equal(remote.Id, mgr.GetActiveRemoteId());
        Assert.Equal("test", mgr.GetActiveRemoteName());
    }

    [Fact]
    public void NoActiveRemote_ReturnsNull()
    {
        var (mgr, _, _) = CreateFixture();
        Assert.Null(mgr.GetActiveRemoteId());
        Assert.Null(mgr.GetActiveRemoteName());
    }

    // -- Feedback limits recalculation --

    [Fact]
    public void RecalculateFeedbackLimits_TakesMinimumAcrossActuators()
    {
        var (mgr, desk, _) = CreateFixture();
        desk.SetUseActuatorLimits(true);

        var agent1 = new ActuatorAgent(Guid.NewGuid())
        {
            MaxA = 100, MaxB = 80,
            SendFunc = _ => Task.FromResult(true)
        };
        var agent2 = new ActuatorAgent(Guid.NewGuid())
        {
            MaxA = 50, MaxB = 120,
            SendFunc = _ => Task.FromResult(true)
        };
        mgr.AddAgent(agent1);
        mgr.AddAgent(agent2);

        // Trigger state updates to fire RecalculateFeedbackLimits
        var status1 = """{"type":"msg","clientId":"c","targetId":"t","message":"strength-10+20+100+80"}""";
        var status2 = """{"type":"msg","clientId":"c","targetId":"t","message":"strength-0+0+50+120"}""";
        agent1.HandleMessage(status1);
        agent2.HandleMessage(status2);

        // After both agents report, RecalculateFeedbackLimits should have been called
        // with min(100,50) = 50, min(80,120) = 80
        Assert.Equal(50, desk.MaxA);
        Assert.Equal(80, desk.MaxB);
    }

    [Fact]
    public void RemoveActuator_RecalculatesLimits()
    {
        var (mgr, desk, _) = CreateFixture();
        desk.SetUseActuatorLimits(true);

        var agent1 = new ActuatorAgent(Guid.NewGuid())
        {
            MaxA = 100, MaxB = 100,
            SendFunc = _ => Task.FromResult(true)
        };
        var agent2 = new ActuatorAgent(Guid.NewGuid())
        {
            MaxA = 50, MaxB = 50,
            SendFunc = _ => Task.FromResult(true)
        };
        mgr.AddAgent(agent1);
        mgr.AddAgent(agent2);

        // Trigger state updates to fire RecalculateFeedbackLimits
        var status1 = """{"type":"msg","clientId":"c","targetId":"t","message":"strength-10+20+100+100"}""";
        var status2 = """{"type":"msg","clientId":"c","targetId":"t","message":"strength-10+20+50+50"}""";
        agent1.HandleMessage(status1);
        agent2.HandleMessage(status2);

        Assert.Equal(50, desk.MaxA);
        Assert.Equal(50, desk.MaxB);

        // Remove the limiting actuator
        mgr.DeactivateActuator(agent2.Id);
        Assert.Equal(100, desk.MaxA);
        Assert.Equal(100, desk.MaxB);
    }

    [Fact]
    public void ForwardToControlDesk_NonActiveRemote_GetsInactiveError()
    {
        var (mgr, _, sent) = CreateFixture();

        var active = new RemoteAgent(Guid.NewGuid(), "active")
        {
            SendFunc = _ => Task.FromResult(true),
            ForwardToControlDesk = _ => { }
        };
        var inactive = new RemoteAgent(Guid.NewGuid(), "inactive")
        {
            SendFunc = msg => { sent.Add(msg); return Task.FromResult(true); },
            ForwardToControlDesk = _ => { }
        };

        mgr.AddAgent(active);
        mgr.AddAgent(inactive);
        mgr.SetActiveRemote(active.Id);

        // inactive sends a set command via its HandleMessage
        var json = """{"type":"set","channel":"A","value":50}""";
        inactive.HandleMessage(json);

        // inactive should get an "Inactive" error response
        Assert.Contains(sent, m => m.Contains("Inactive"));
    }

    [Fact]
    public void ActiveRemote_Write_ProducesStateReply()
    {
        var (mgr, _, sent) = CreateFixture();

        var active = new RemoteAgent(Guid.NewGuid(), "active")
        {
            SendFunc = msg => { sent.Add(msg); return Task.FromResult(true); },
            ForwardToControlDesk = null // will be set by AddAgent
        };
        mgr.AddAgent(active);
        mgr.SetActiveRemote(active.Id);

        var json = """{"type":"set","channel":"A","value":75}""";
        active.HandleMessage(json);

        // active should get state reply after applying
        Assert.Contains(sent, m => m.Contains("\"state\""));
        Assert.Contains(sent, m => m.Contains("\"a\":75"));
    }
}
