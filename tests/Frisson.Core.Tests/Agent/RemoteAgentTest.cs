namespace Frisson.Core.Tests;

public class RemoteAgentTest
{
    [Fact]
    public void HandleMessage_NullParse_SendsError()
    {
        string? sent = null;
        var agent = new RemoteAgent(Guid.NewGuid(), "test")
        {
            SendFunc = msg => { sent = msg; return Task.FromResult(true); }
        };
        agent.HandleMessage("not json");
        Assert.NotNull(sent);
        Assert.Contains("\"error\"", sent);
    }

    [Fact]
    public void HandleMessage_ValidScheme_ForwardsToControlDesk()
    {
        Scheme.Scheme? forwarded = null;
        var agent = new RemoteAgent(Guid.NewGuid(), "test")
        {
            ForwardToControlDesk = scheme => forwarded = scheme,
            SendFunc = _ => Task.FromResult(true)
        };
        var json = """{"type":"set","channel":"A","value":50}""";
        agent.HandleMessage(json);
        Assert.NotNull(forwarded);
        Assert.IsType<SetScheme>(forwarded);
        Assert.Equal("A", ((SetScheme)forwarded).Channel);
    }

    [Fact]
    public async Task HandleMessage_ForwardToControlDeskNull_NoCrash()
    {
        var agent = new RemoteAgent(Guid.NewGuid(), "test")
        {
            SendFunc = _ => Task.FromResult(true)
        };
        var json = """{"type":"set","channel":"A","value":50}""";
        var ex = await Record.ExceptionAsync(() => agent.HandleMessage(json));
        Assert.Null(ex);
    }

    [Fact]
    public async Task HandleMessage_SendFuncNull_NoCrash()
    {
        var agent = new RemoteAgent(Guid.NewGuid(), "test");
        var ex = await Record.ExceptionAsync(() => agent.HandleMessage("not json"));
        Assert.Null(ex);
    }

    [Fact]
    public void Constructor_SetsName()
    {
        var agent = new RemoteAgent(Guid.NewGuid(), "Player 1");
        Assert.Equal("Player 1", agent.Name);
    }

    [Fact]
    public void Constructor_SetsId()
    {
        var id = Guid.NewGuid();
        var agent = new RemoteAgent(id, "test");
        Assert.Equal(id, agent.Id);
    }
}
