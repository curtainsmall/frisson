using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

using Frisson.Core.Scheme;
using Frisson.Core.Scheme.Remote;

namespace Frisson.Core;

internal class ControlDesk
{
    public int StrengthA { get; set; }
    public int StrengthB { get; set; }

    int _settingsMaxA = 100;
    int _settingsMaxB = 100;
    int _feedbackMaxA = 100;
    int _feedbackMaxB = 100;
    bool _useActuatorLimits;

    public int MaxA => _useActuatorLimits ? _feedbackMaxA : _settingsMaxA;
    public int MaxB => _useActuatorLimits ? _feedbackMaxB : _settingsMaxB;
    public bool UseActuatorLimits => _useActuatorLimits;

    bool _blocked;

    public event Action? StateChanged;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Apply a parsed Remote scheme. Returns a JsonNode reply (state message)
    /// if state changed, or null if nothing changed. Fires StateChanged on change.
    /// </summary>
    public JsonNode? ApplyFromRemote(Scheme.Scheme scheme)
    {
        JsonNode? result = scheme switch
        {
            SetScheme setMsg => ApplySet(setMsg),
            VaryScheme varyMsg => ApplyVary(varyMsg),
            _ => null
        };

        if (result != null)
            StateChanged?.Invoke();

        return result;
    }

    private JsonNode? ApplySet(SetScheme msg)
    {
        var ch = char.ToUpperInvariant(msg.Channel.Length > 0 ? msg.Channel[0] : '\0');
        if (ch != 'A' && ch != 'B') return null;

        var clamped = Math.Clamp(msg.Value, 0, ch == 'A' ? MaxA : MaxB);

        if (ch == 'A' && StrengthA != clamped) { StrengthA = clamped; return ToRemoteStateNode(); }
        if (ch == 'B' && StrengthB != clamped) { StrengthB = clamped; return ToRemoteStateNode(); }
        return null;
    }

    private JsonNode? ApplyVary(VaryScheme msg)
    {
        var ch = char.ToUpperInvariant(msg.Channel.Length > 0 ? msg.Channel[0] : '\0');
        if (ch != 'A' && ch != 'B') return null;

        var current = ch == 'A' ? StrengthA : StrengthB;
        var max = ch == 'A' ? MaxA : MaxB;
        var clamped = Math.Clamp(current + msg.Value, 0, max);

        if (ch == 'A' && StrengthA != clamped) { StrengthA = clamped; return ToRemoteStateNode(); }
        if (ch == 'B' && StrengthB != clamped) { StrengthB = clamped; return ToRemoteStateNode(); }
        return null;
    }

    public void SetBlocked(bool blocked)
    {
        if (_blocked == blocked) return;
        _blocked = blocked;
        StateChanged?.Invoke();
    }
    public bool IsBlocked => _blocked;

    /// <summary>
    /// Set strength values from local UI controls.
    /// Only allowed when not blocked by active Remote.
    /// Clamps values to [0, MaxA/MaxB] and fires StateChanged.
    /// </summary>
    public void SetLocalStrength(int a, int b)
    {
        if (_blocked)
            return;

        a = Math.Clamp(a, 0, MaxA);
        b = Math.Clamp(b, 0, MaxB);

        bool changed = false;
        if (StrengthA != a) { StrengthA = a; changed = true; }
        if (StrengthB != b) { StrengthB = b; changed = true; }

        if (changed)
            StateChanged?.Invoke();
    }

    /// <summary>
    /// Set max strength limits. If current strengths exceed the new limits,
    /// they are clamped down and StateChanged fires accordingly.
    /// </summary>
    public void SetMax(int a, int b)
    {
        // Protocol limit: [0, 200]
        a = Math.Clamp(a, 0, 200);
        b = Math.Clamp(b, 0, 200);

        bool changed = false;
        if (_settingsMaxA != a) { _settingsMaxA = a; changed = true; }
        if (_settingsMaxB != b) { _settingsMaxB = b; changed = true; }
        // Sync feedback defaults so toggling before any actuator reports doesn't cause a jump
        if (_feedbackMaxA != a) { _feedbackMaxA = a; changed = true; }
        if (_feedbackMaxB != b) { _feedbackMaxB = b; changed = true; }

        // Clamp current strengths if they exceed new limits
        var clampedA = Math.Clamp(StrengthA, 0, MaxA);
        var clampedB = Math.Clamp(StrengthB, 0, MaxB);
        if (StrengthA != clampedA) { StrengthA = clampedA; changed = true; }
        if (StrengthB != clampedB) { StrengthB = clampedB; changed = true; }

        if (changed)
            StateChanged?.Invoke();
    }

    /// <summary>
    /// Apply feedback limits reported by actuators. Takes the minimum across all connected
    /// actuators. null resets to _settingsMaxA/B (e.g. when all actuators disconnect).
    /// </summary>
    public void ApplyFeedbackLimits(int? aMax, int? bMax)
    {
        var newA = aMax ?? _settingsMaxA;
        var newB = bMax ?? _settingsMaxB;

        bool changed = false;
        if (_feedbackMaxA != newA) { _feedbackMaxA = newA; changed = true; }
        if (_feedbackMaxB != newB) { _feedbackMaxB = newB; changed = true; }

        if (changed && _useActuatorLimits)
        {
            var clampedA = Math.Clamp(StrengthA, 0, MaxA);
            var clampedB = Math.Clamp(StrengthB, 0, MaxB);
            if (StrengthA != clampedA) { StrengthA = clampedA; changed = true; }
            if (StrengthB != clampedB) { StrengthB = clampedB; changed = true; }
            StateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Enable or disable actuator-reported limits. When toggled on, clamps
    /// current strengths to the feedback limits if needed.
    /// </summary>
    public void SetUseActuatorLimits(bool use)
    {
        if (_useActuatorLimits == use) return;
        _useActuatorLimits = use;

        var clampedA = Math.Clamp(StrengthA, 0, MaxA);
        var clampedB = Math.Clamp(StrengthB, 0, MaxB);
        if (StrengthA != clampedA) StrengthA = clampedA;
        if (StrengthB != clampedB) StrengthB = clampedB;
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Build current strength state as a JsonNode for Remote replies.
    /// </summary>
    public JsonNode ToRemoteStateNode()
    {
        return new JsonObject
        {
            ["type"] = "state",
            ["a"] = StrengthA,
            ["b"] = StrengthB,
            ["maxA"] = MaxA,
            ["maxB"] = MaxB
        };
    }

    /// <summary>
    /// Serialize a single-channel strength-set message for a specific device.
    /// Format: strength-{channel}+2+{value} (mode 2 = set to value)
    /// channel: 1 = A, 2 = B
    /// </summary>
    public string StrengthMessage(Guid targetId, int channel, int value)
    {
        return JsonSerializer.Serialize(new
        {
            type = "msg",
            clientId = AppCore.DummyFrontendId.ToString(),
            targetId = targetId.ToString(),
            message = $"strength-{channel}+2+{value}"
        }, JsonOptions);
    }

}
