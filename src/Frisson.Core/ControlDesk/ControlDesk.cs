using System;
using System.Text.Json;

using Frisson.Core.Agent.Actuator;
using Frisson.Core.Scheme.Actuator;

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
    /// Fires StateChanged after updating.
    /// </summary>
    public void ApplyFromRemote(string json)
    {
        var scheme = Scheme.Scheme.Parse(json);
        if (scheme is MsgScheme msg)
        {
            // Parse message field for strength/pulse commands
        }
        StateChanged?.Invoke();
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
