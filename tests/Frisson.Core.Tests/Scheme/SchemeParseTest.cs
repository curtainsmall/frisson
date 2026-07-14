namespace Frisson.Core.Tests;

public class SchemeParseTest
{
    [Fact]
    public void Parse_SetType_ReturnsSetScheme()
    {
        var json = """{"type":"set","channel":"A","value":50}""";
        var result = Scheme.Scheme.Parse(json);
        Assert.IsType<SetScheme>(result);
        var set = (SetScheme)result;
        Assert.Equal("A", set.Channel);
        Assert.Equal(50, set.Value);
    }

    [Fact]
    public void Parse_VaryType_ReturnsVaryScheme()
    {
        var json = """{"type":"vary","channel":"B","value":-10}""";
        var result = Scheme.Scheme.Parse(json);
        Assert.IsType<VaryScheme>(result);
        var vary = (VaryScheme)result;
        Assert.Equal("B", vary.Channel);
        Assert.Equal(-10, vary.Value);
    }

    [Fact]
    public void Parse_MsgType_ReturnsMsgScheme()
    {
        var json = """{"type":"msg","clientId":"x","targetId":"y","message":"strength-1+2+20"}""";
        var result = Scheme.Scheme.Parse(json);
        Assert.IsType<MsgScheme>(result);
        var msg = (MsgScheme)result;
        Assert.Equal("x", msg.ClientId);
        Assert.Equal("y", msg.TargetId);
        Assert.Equal("strength-1+2+20", msg.Message);
    }

    [Fact]
    public void Parse_BindWithClientId_ReturnsActuatorBindScheme()
    {
        var json = """{"type":"bind","clientId":"6ad7e768-5449-4c0f-9346-41bee3b13d9e","targetId":"b1d4e768-5449-4c0f-9346-41bee3b13d9e","message":"bind"}""";
        var result = Scheme.Scheme.Parse(json);
        Assert.IsType<Scheme.Actuator.BindScheme>(result);
    }

    [Fact]
    public void Parse_BindWithId_ReturnsRemoteBindScheme()
    {
        var json = """{"type":"bind","id":"6ad7e768-5449-4c0f-9346-41bee3b13d9e","name":"test"}""";
        var result = Scheme.Scheme.Parse(json);
        Assert.IsType<Scheme.Remote.BindScheme>(result);
    }

    [Fact]
    public void Parse_UnknownType_ReturnsNull()
    {
        var json = """{"type":"unknown"}""";
        var result = Scheme.Scheme.Parse(json);
        Assert.Null(result);
    }

    [Fact]
    public void Parse_InvalidJson_ReturnsNull()
    {
        var result = Scheme.Scheme.Parse("not json");
        Assert.Null(result);
    }

    [Fact]
    public void Parse_MissingType_ReturnsNull()
    {
        var json = """{"channel":"A"}""";
        var result = Scheme.Scheme.Parse(json);
        Assert.Null(result);
    }

    [Fact]
    public void SetScheme_RoundTrip_PreservesFields()
    {
        var original = new SetScheme { Channel = "A", Value = 75 };
        var json = original.ToJson();
        var parsed = Scheme.Scheme.Parse(json);
        Assert.IsType<SetScheme>(parsed);
        var set = (SetScheme)parsed;
        Assert.Equal("A", set.Channel);
        Assert.Equal(75, set.Value);
    }

    [Fact]
    public void SetScheme_MissingFields_UsesDefaults()
    {
        var json = """{"type":"set"}""";
        var result = Scheme.Scheme.Parse(json);
        Assert.IsType<SetScheme>(result);
        var set = (SetScheme)result;
        Assert.Equal(string.Empty, set.Channel);
        Assert.Equal(0, set.Value);
    }

    [Fact]
    public void VaryScheme_RoundTrip_PreservesFields()
    {
        var original = new VaryScheme { Channel = "B", Value = -20 };
        var json = original.ToJson();
        var parsed = Scheme.Scheme.Parse(json);
        Assert.IsType<VaryScheme>(parsed);
        var vary = (VaryScheme)parsed;
        Assert.Equal("B", vary.Channel);
        Assert.Equal(-20, vary.Value);
    }

    [Fact]
    public void VaryScheme_MissingFields_UsesDefaults()
    {
        var json = """{"type":"vary"}""";
        var result = Scheme.Scheme.Parse(json);
        Assert.IsType<VaryScheme>(result);
        var vary = (VaryScheme)result;
        Assert.Equal(string.Empty, vary.Channel);
        Assert.Equal(0, vary.Value);
    }
}
