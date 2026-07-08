using System;
using System.Text.Json;

using Frisson.Core.Agent.Actuator;
using Frisson.Core.Scheme;
using Frisson.Core.Scheme.Actuator;
using Frisson.Core.Scheme.Remote;

namespace Frisson.Core;

internal class ControlDesk
{
    public int StrengthA { get; set; }
    public int StrengthB { get; set; }
    public int MaxA { get; set; } = 200;
    public int MaxB { get; set; } = 200;

    bool _blocked;

    public event Action? StateChanged;

    /// <summary>
    /// Update control state from external Remote JSON message.
    /// Supports SetScheme (absolute set) and VaryScheme (delta).
    /// Fires StateChanged after updating.
    /// Returns true if any strength value changed.
    /// </summary>
    public bool ApplyFromRemote(string json)
    {
        var scheme = Scheme.Scheme.Parse(json);

        bool changed = scheme switch
        {
            SetScheme setMsg => ApplySet(setMsg),
            VaryScheme varyMsg => ApplyVary(varyMsg),
            _ => false
        };

        if (changed)
            StateChanged?.Invoke();

        return changed;
    }

    private bool ApplySet(SetScheme msg)
    {
        var ch = char.ToUpperInvariant(msg.Channel.Length > 0 ? msg.Channel[0] : '\0');
        if (ch != 'A' && ch != 'B') return false;

        var clamped = Math.Clamp(msg.Value, 0, ch == 'A' ? MaxA : MaxB);

        if (ch == 'A' && StrengthA != clamped) { StrengthA = clamped; return true; }
        if (ch == 'B' && StrengthB != clamped) { StrengthB = clamped; return true; }
        return false;
    }

    private bool ApplyVary(VaryScheme msg)
    {
        var ch = char.ToUpperInvariant(msg.Channel.Length > 0 ? msg.Channel[0] : '\0');
        if (ch != 'A' && ch != 'B') return false;

        var current = ch == 'A' ? StrengthA : StrengthB;
        var max = ch == 'A' ? MaxA : MaxB;
        var clamped = Math.Clamp(current + msg.Value, 0, max);

        if (ch == 'A' && StrengthA != clamped) { StrengthA = clamped; return true; }
        if (ch == 'B' && StrengthB != clamped) { StrengthB = clamped; return true; }
        return false;
    }

    public void SetBlocked(bool blocked) => _blocked = blocked;
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
    /// Serialize current strength state to a state message for Remote.
    /// </summary>
    public string ToRemoteStateMessage()
    {
        return JsonSerializer.Serialize(new
        {
            type = "state",
            a = StrengthA,
            b = StrengthB,
            maxA = MaxA,
            maxB = MaxB
        });
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

    /// <summary>
    /// Serialize current pulse/waveform state to a pulse message for devices.
    /// </summary>
    public string ToPulseMessage()
    {
        // TODO: implement waveform/pulse message generation
        return string.Empty;
    }
}
