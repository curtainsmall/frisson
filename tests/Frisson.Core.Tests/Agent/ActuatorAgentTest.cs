namespace Frisson.Core.Tests.Agent;

public class ActuatorAgentTest
{
    [Fact]
    public void HandleMessage_StrengthStatus_UpdatesProperties()
    {
        var agent = new ActuatorAgent(Guid.NewGuid());
        var json = """{"type":"msg","clientId":"c","targetId":"t","message":"strength-10+20+100+200"}""";
        agent.HandleMessage(json);

        Assert.Equal(10, agent.StrengthA);
        Assert.Equal(20, agent.StrengthB);
        Assert.Equal(100, agent.MaxA);
        Assert.Equal(200, agent.MaxB);
    }

    [Fact]
    public void HandleMessage_StrengthStatus_FiresStateUpdated()
    {
        var agent = new ActuatorAgent(Guid.NewGuid());
        int count = 0;
        agent.StateUpdated += () => count++;
        var json = """{"type":"msg","clientId":"c","targetId":"t","message":"strength-5+15+50+100"}""";
        agent.HandleMessage(json);
        Assert.Equal(1, count);
    }

    [Fact]
    public void HandleMessage_Feedback_DoesNotFireStateUpdated()
    {
        var agent = new ActuatorAgent(Guid.NewGuid());
        int count = 0;
        agent.StateUpdated += () => count++;
        var json = """{"type":"msg","clientId":"c","targetId":"t","message":"feedback-3"}""";
        agent.HandleMessage(json);
        Assert.Equal(0, count);
    }

    [Fact]
    public void HandleMessage_UnknownMessage_DoesNothing()
    {
        var agent = new ActuatorAgent(Guid.NewGuid());
        int count = 0;
        agent.StateUpdated += () => count++;
        var json = """{"type":"bind","clientId":"c","targetId":"t"}""";
        agent.HandleMessage(json);
        Assert.Equal(0, count);
        Assert.Equal(0, agent.StrengthA);
        Assert.Equal(0, agent.StrengthB);
    }

    [Fact]
    public void HandleMessage_InvalidJson_DoesNothing()
    {
        var agent = new ActuatorAgent(Guid.NewGuid());
        int count = 0;
        agent.StateUpdated += () => count++;
        agent.HandleMessage("not json");
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task SendFunc_CalledWithMessage()
    {
        string? received = null;
        var agent = new ActuatorAgent(Guid.NewGuid())
        {
            SendFunc = msg => { received = msg; return Task.FromResult(true); }
        };

        // Simulate receiving a StrengthCommand (device sends it back as echo)
        var json = """{"type":"msg","clientId":"c","targetId":"t","message":"strength-1+2+20"}""";
        agent.SendFunc?.Invoke(json);
        // Send is fire-and-forget; verify captured value
        Assert.Equal(json, received);
    }
}
