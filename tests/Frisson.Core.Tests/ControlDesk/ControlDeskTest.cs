namespace Frisson.Core.Tests.ControlDesk;

public class ControlDeskTest
{
    // -- SetLocalStrength --

    [Fact]
    public void SetLocalStrength_NormalPath_SetsValues()
    {
        var desk = new Core.ControlDesk();
        desk.SetLocalStrength(30, 40);
        Assert.Equal(30, desk.StrengthA);
        Assert.Equal(40, desk.StrengthB);
    }

    [Fact]
    public void SetLocalStrength_WhenBlocked_DoesNothing()
    {
        var desk = new Core.ControlDesk();
        desk.SetBlocked(true);
        desk.SetLocalStrength(50, 60);
        Assert.Equal(0, desk.StrengthA);
        Assert.Equal(0, desk.StrengthB);
    }

    [Fact]
    public void SetLocalStrength_ClampsToMax()
    {
        var desk = new Core.ControlDesk();
        desk.SetMax(50, 30);
        desk.SetLocalStrength(100, 100);
        Assert.Equal(50, desk.StrengthA);
        Assert.Equal(30, desk.StrengthB);
    }

    [Fact]
    public void SetLocalStrength_ClampsToZero()
    {
        var desk = new Core.ControlDesk();
        desk.SetLocalStrength(-10, -20);
        Assert.Equal(0, desk.StrengthA);
        Assert.Equal(0, desk.StrengthB);
    }

    // -- SetMax --

    [Fact]
    public void SetMax_ProtocolCapAt200()
    {
        var desk = new Core.ControlDesk();
        desk.SetMax(300, 250);
        Assert.Equal(200, desk.MaxA);
        Assert.Equal(200, desk.MaxB);
    }

    [Fact]
    public void SetMax_Negative_ClampsToZero()
    {
        var desk = new Core.ControlDesk();
        desk.SetMax(-10, -20);
        Assert.Equal(0, desk.MaxA);
        Assert.Equal(0, desk.MaxB);
    }

    [Fact]
    public void SetMax_DownClampsCurrentStrength()
    {
        var desk = new Core.ControlDesk();
        desk.SetLocalStrength(80, 90);
        desk.SetMax(50, 60);
        Assert.Equal(50, desk.StrengthA);
        Assert.Equal(60, desk.StrengthB);
    }

    // -- ApplyFromRemote (SetScheme) --

    [Fact]
    public void ApplySet_ValidChannelA_SetsStrength()
    {
        var desk = new Core.ControlDesk();
        var set = new SetScheme { Channel = "A", Value = 75 };
        var result = desk.ApplyFromRemote(set);
        Assert.NotNull(result);
        Assert.Equal(75, desk.StrengthA);
    }

    [Fact]
    public void ApplySet_ValidChannelB_SetsStrength()
    {
        var desk = new Core.ControlDesk();
        var set = new SetScheme { Channel = "B", Value = 60 };
        var result = desk.ApplyFromRemote(set);
        Assert.NotNull(result);
        Assert.Equal(60, desk.StrengthB);
    }

    [Fact]
    public void ApplySet_InvalidChannel_ReturnsNull()
    {
        var desk = new Core.ControlDesk();
        var set = new SetScheme { Channel = "C", Value = 50 };
        var result = desk.ApplyFromRemote(set);
        Assert.Null(result);
        Assert.Equal(0, desk.StrengthA);
        Assert.Equal(0, desk.StrengthB);
    }

    [Fact]
    public void ApplySet_ClampsToMax()
    {
        var desk = new Core.ControlDesk();
        desk.SetMax(30, 40);
        var set = new SetScheme { Channel = "A", Value = 100 };
        desk.ApplyFromRemote(set);
        Assert.Equal(30, desk.StrengthA);
    }

    [Fact]
    public void ApplySet_NoChange_ReturnsNull()
    {
        var desk = new Core.ControlDesk();
        desk.SetLocalStrength(50, 50);
        var set = new SetScheme { Channel = "A", Value = 50 };
        var result = desk.ApplyFromRemote(set);
        Assert.Null(result);
    }

    // -- ApplyFromRemote (VaryScheme) --

    [Fact]
    public void ApplyVary_PositiveDelta_Increases()
    {
        var desk = new Core.ControlDesk();
        desk.SetLocalStrength(30, 0);
        desk.ApplyFromRemote(new VaryScheme { Channel = "A", Value = 20 });
        Assert.Equal(50, desk.StrengthA);
    }

    [Fact]
    public void ApplyVary_NegativeDelta_Decreases()
    {
        var desk = new Core.ControlDesk();
        desk.SetLocalStrength(50, 0);
        desk.ApplyFromRemote(new VaryScheme { Channel = "A", Value = -30 });
        Assert.Equal(20, desk.StrengthA);
    }

    [Fact]
    public void ApplyVary_ClampsToMax()
    {
        var desk = new Core.ControlDesk();
        desk.SetMax(50, 200);
        desk.SetLocalStrength(40, 0);
        desk.ApplyFromRemote(new VaryScheme { Channel = "A", Value = 20 });
        Assert.Equal(50, desk.StrengthA);
    }

    [Fact]
    public void ApplyVary_ClampsToZero()
    {
        var desk = new Core.ControlDesk();
        desk.SetLocalStrength(10, 0);
        desk.ApplyFromRemote(new VaryScheme { Channel = "A", Value = -20 });
        Assert.Equal(0, desk.StrengthA);
    }

    // -- StateChanged event --

    [Fact]
    public void StateChanged_FiresOnChange()
    {
        var desk = new Core.ControlDesk();
        int count = 0;
        desk.StateChanged += () => count++;
        desk.SetLocalStrength(30, 40);
        Assert.Equal(1, count);
    }

    [Fact]
    public void StateChanged_DoesNotFireOnNoOp()
    {
        var desk = new Core.ControlDesk();
        int count = 0;
        desk.StateChanged += () => count++;
        desk.SetLocalStrength(0, 0);
        Assert.Equal(0, count);
    }

    [Fact]
    public void ApplyFromRemote_ChangedState_FiresEvent()
    {
        var desk = new Core.ControlDesk();
        int count = 0;
        desk.StateChanged += () => count++;
        desk.ApplyFromRemote(new SetScheme { Channel = "A", Value = 50 });
        Assert.Equal(1, count);
    }

    [Fact]
    public void ApplyFromRemote_NoChange_DoesNotFireEvent()
    {
        var desk = new Core.ControlDesk();
        desk.SetLocalStrength(50, 0);
        int count = 0;
        desk.StateChanged += () => count++;
        desk.ApplyFromRemote(new SetScheme { Channel = "A", Value = 50 });
        Assert.Equal(0, count);
    }

    // -- UseActuatorLimits / FeedbackLimits --

    [Fact]
    public void ApplyFeedbackLimits_WhenDisabled_DoesNotClampOrFire()
    {
        var desk = new Core.ControlDesk();
        desk.SetLocalStrength(80, 80);
        int count = 0;
        desk.StateChanged += () => count++;
        desk.ApplyFeedbackLimits(50, 50);
        Assert.Equal(80, desk.StrengthA);
        Assert.Equal(0, count);
    }

    [Fact]
    public void ApplyFeedbackLimits_WhenEnabled_ClampsAndFires()
    {
        var desk = new Core.ControlDesk();
        desk.SetUseActuatorLimits(true);
        desk.SetLocalStrength(80, 80);
        int count = 0;
        desk.StateChanged += () => count++;
        desk.ApplyFeedbackLimits(50, 60);
        Assert.Equal(50, desk.StrengthA);
        Assert.Equal(60, desk.StrengthB);
        Assert.Equal(1, count);
    }

    [Fact]
    public void ApplyFeedbackLimits_Null_ResetsToSettingsMax()
    {
        var desk = new Core.ControlDesk();
        desk.SetMax(100, 100);
        desk.SetUseActuatorLimits(true);
        desk.ApplyFeedbackLimits(50, 50);
        Assert.Equal(50, desk.MaxA);
        desk.ApplyFeedbackLimits(null, null);
        Assert.Equal(100, desk.MaxA);
    }

    [Fact]
    public void SetUseActuatorLimits_On_ClampsStrength()
    {
        var desk = new Core.ControlDesk();
        desk.SetLocalStrength(80, 80);
        desk.ApplyFeedbackLimits(50, 50);
        desk.SetUseActuatorLimits(true);
        Assert.Equal(50, desk.StrengthA);
        Assert.Equal(50, desk.StrengthB);
    }

    [Fact]
    public void SetUseActuatorLimits_Off_DoesNotClamp()
    {
        var desk = new Core.ControlDesk();
        desk.SetUseActuatorLimits(true);
        desk.ApplyFeedbackLimits(50, 50);
        desk.SetUseActuatorLimits(false);
        desk.SetLocalStrength(80, 80);
        Assert.Equal(80, desk.StrengthA);
    }

    [Fact]
    public void SetUseActuatorLimits_NoChange_DoesNotFire()
    {
        var desk = new Core.ControlDesk();
        int count = 0;
        desk.StateChanged += () => count++;
        desk.SetUseActuatorLimits(false);
        Assert.Equal(0, count);
    }

    // -- StrengthMessage serialization --

    [Fact]
    public void StrengthMessage_ContainsCorrectFields()
    {
        var desk = new Core.ControlDesk();
        var id = Guid.Parse("6ad7e768-5449-4c0f-9346-41bee3b13d9e");
        var json = desk.StrengthMessage(id, 1, 20);
        Assert.Contains("\"type\":\"msg\"", json);
        Assert.Contains("\"targetId\":\"6ad7e768-5449-4c0f-9346-41bee3b13d9e\"", json);
        Assert.Contains("\"message\":\"strength-1+2+20\"", json);
    }

    [Fact]
    public void StrengthMessage_PlusSign_NotEscaped()
    {
        var desk = new Core.ControlDesk();
        var json = desk.StrengthMessage(Guid.NewGuid(), 1, 20);
        Assert.DoesNotContain("\\u002B", json);
        Assert.Contains("+2+20", json);
    }

    // -- ToRemoteStateNode --

    [Fact]
    public void ToRemoteStateNode_ContainsCorrectValues()
    {
        var desk = new Core.ControlDesk();
        desk.SetLocalStrength(30, 40);
        desk.SetMax(100, 200);
        var node = desk.ToRemoteStateNode();
        Assert.Equal("state", node["type"]?.GetValue<string>());
        Assert.Equal(30, node["a"]?.GetValue<int>());
        Assert.Equal(40, node["b"]?.GetValue<int>());
        Assert.Equal(100, node["maxA"]?.GetValue<int>());
        Assert.Equal(200, node["maxB"]?.GetValue<int>());
    }

    // -- Parameterized tests --

    [Theory]
    [InlineData("A", 50, 50, 0)]
    [InlineData("B", 60, 0, 60)]
    [InlineData("A", 0, 0, 0)]
    public void ApplySet_ChannelAndValue_UpdatesCorrectStrength(
        string channel, int value,
        int expectedA, int expectedB)
    {
        var desk = new Core.ControlDesk();
        desk.ApplyFromRemote(new SetScheme { Channel = channel, Value = value });
        Assert.Equal(expectedA, desk.StrengthA);
        Assert.Equal(expectedB, desk.StrengthB);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(30, 40, false)]
    public void SetLocalStrength_OnlyFiresOnChange(int a, int b, bool expectFire)
    {
        var desk = new Core.ControlDesk();
        desk.SetLocalStrength(30, 40);
        int count = 0;
        desk.StateChanged += () => count++;
        desk.SetLocalStrength(a, b);
        Assert.Equal(expectFire ? 1 : 0, count);
    }

    // -- SetBlocked / IsBlocked --

    [Fact]
    public void SetBlocked_TogglesCorrectly()
    {
        var desk = new Core.ControlDesk();
        Assert.False(desk.IsBlocked);
        desk.SetBlocked(true);
        Assert.True(desk.IsBlocked);
        desk.SetBlocked(false);
        Assert.False(desk.IsBlocked);
    }

    [Fact]
    public void SetBlocked_NoChange_DoesNotFire()
    {
        var desk = new Core.ControlDesk();
        int count = 0;
        desk.StateChanged += () => count++;
        desk.SetBlocked(false);
        Assert.Equal(0, count);
    }
}
