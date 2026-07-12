using System.Text.Json;
using System.Text.Json.Nodes;

using Frisson.Core.Scheme;
using Frisson.Core.Scheme.Remote;

namespace Frisson.Core;

internal class ControlDesk
{
    public int StrengthA { get; set; }
    public int StrengthB { get; set; }
    public int MaxA { get; set; } = 100;
    public int MaxB { get; set; } = 100;

    bool _blocked;

    public event Action? StateChanged;

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
        if (MaxA != a) { MaxA = a; changed = true; }
        if (MaxB != b) { MaxB = b; changed = true; }

        // Clamp current strengths if they exceed new limits
        var clampedA = Math.Clamp(StrengthA, 0, MaxA);
        var clampedB = Math.Clamp(StrengthB, 0, MaxB);
        if (StrengthA != clampedA) { StrengthA = clampedA; changed = true; }
        if (StrengthB != clampedB) { StrengthB = clampedB; changed = true; }

        if (changed)
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
    /// Serialize current strength state to a strength-set message for devices.
    /// </summary>
    public string ToStrengthMessage()
    {
        return JsonSerializer.Serialize(new
        {
            type = "msg",
            clientId = AppCore.DummyFrontendId.ToString(),
            targetId = "",
            message = $"strength-1+2+{StrengthA}+2+2+{StrengthB}"
        });
    }

}
