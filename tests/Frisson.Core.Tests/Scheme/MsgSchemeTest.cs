namespace Frisson.Core.Tests;

public class MsgSchemeTest
{
    [Fact]
    public void Parse_StrengthCommand_ExtractsFields()
    {
        var json = """{"type":"msg","clientId":"c1","targetId":"t1","message":"strength-1+2+20"}""";
        var msg = MsgScheme.TryParse(json);
        Assert.NotNull(msg);
        Assert.Equal(MsgKind.StrengthCommand, msg.Kind);
        Assert.Equal(1, msg.Channel);
        Assert.Equal(2, msg.Mode);
        Assert.Equal(20, msg.Value);
    }

    [Fact]
    public void Parse_StrengthCommand_Channel2()
    {
        var json = """{"type":"msg","clientId":"c1","targetId":"t1","message":"strength-2+1+15"}""";
        var msg = MsgScheme.TryParse(json);
        Assert.NotNull(msg);
        Assert.Equal(2, msg.Channel);
        Assert.Equal(1, msg.Mode);
        Assert.Equal(15, msg.Value);
    }

    [Fact]
    public void Parse_StrengthStatus_ExtractsFields()
    {
        var json = """{"type":"msg","clientId":"c1","targetId":"t1","message":"strength-10+20+100+200"}""";
        var msg = MsgScheme.TryParse(json);
        Assert.NotNull(msg);
        Assert.Equal(MsgKind.StrengthStatus, msg.Kind);
        Assert.Equal(10, msg.StrengthA);
        Assert.Equal(20, msg.StrengthB);
        Assert.Equal(100, msg.MaxA);
        Assert.Equal(200, msg.MaxB);
    }

    [Fact]
    public void Parse_PulseCommand_ExtractsFields()
    {
        var json = """{"type":"msg","clientId":"c1","targetId":"t1","message":"pulse-A:[\"aa\",\"bb\"]"}""";
        var msg = MsgScheme.TryParse(json);
        Assert.NotNull(msg);
        Assert.Equal(MsgKind.PulseCommand, msg.Kind);
        Assert.Equal(1, msg.Channel);
        Assert.Equal("""["aa","bb"]""", msg.PulseData);
    }

    [Fact]
    public void Parse_PulseCommand_ChannelB()
    {
        var json = """{"type":"msg","clientId":"c1","targetId":"t1","message":"pulse-B:[\"cc\"]"}""";
        var msg = MsgScheme.TryParse(json);
        Assert.NotNull(msg);
        Assert.Equal(2, msg.Channel);
    }

    [Fact]
    public void Parse_ClearCommand_ExtractsChannel()
    {
        var json = """{"type":"msg","clientId":"c1","targetId":"t1","message":"clear-2"}""";
        var msg = MsgScheme.TryParse(json);
        Assert.NotNull(msg);
        Assert.Equal(MsgKind.ClearCommand, msg.Kind);
        Assert.Equal(2, msg.Channel);
    }

    [Fact]
    public void Parse_Feedback_ExtractsIndex()
    {
        var json = """{"type":"msg","clientId":"c1","targetId":"t1","message":"feedback-3"}""";
        var msg = MsgScheme.TryParse(json);
        Assert.NotNull(msg);
        Assert.Equal(MsgKind.Feedback, msg.Kind);
        Assert.Equal(3, msg.FeedbackIndex);
    }

    [Fact]
    public void TryParse_InvalidJson_ReturnsNull()
    {
        var msg = MsgScheme.TryParse("not json");
        Assert.Null(msg);
    }

    [Theory]
    [InlineData("""{"type":"msg","clientId":"c","targetId":"t","message":"strength-abc"}""", MsgKind.Unknown)]
    [InlineData("""{"type":"msg","clientId":"c","targetId":"t","message":"strength-1+"}""", MsgKind.Unknown)]
    [InlineData("""{"type":"msg","clientId":"c","targetId":"t","message":"pulse-"}""", MsgKind.Unknown)]
    [InlineData("""{"type":"msg","clientId":"c","targetId":"t","message":"feedback-"}""", MsgKind.Unknown)]
    [InlineData("""{"type":"msg","clientId":"c","targetId":"t","message":"clear-abc"}""", MsgKind.Unknown)]
    [InlineData("""{"type":"msg","clientId":"c","targetId":"t","message":"garbage"}""", MsgKind.Unknown)]
    public void TryParse_InvalidContent_ReturnsSchemeWithUnknownKind(string json, MsgKind expectedKind)
    {
        var msg = MsgScheme.TryParse(json);
        Assert.NotNull(msg);
        Assert.Equal(expectedKind, msg.Kind);
    }

    [Fact]
    public void TryParse_PreservesEnvelopeFields()
    {
        var json = """{"type":"msg","clientId":"my-client","targetId":"my-target","message":"strength-1+2+20"}""";
        var msg = MsgScheme.TryParse(json);
        Assert.NotNull(msg);
        Assert.Equal("my-client", msg.ClientId);
        Assert.Equal("my-target", msg.TargetId);
    }

    [Fact]
    public void ToJson_RoundTrip_PreservesEnvelope()
    {
        var original = new MsgScheme
        {
            ClientId = "c1",
            TargetId = "t1",
            Message = "strength-1+2+30"
        };
        var json = original.ToJson();
        var parsed = MsgScheme.TryParse(json);
        Assert.NotNull(parsed);
        Assert.Equal("c1", parsed.ClientId);
        Assert.Equal("t1", parsed.TargetId);
        Assert.Equal("strength-1+2+30", parsed.Message);
    }
}
