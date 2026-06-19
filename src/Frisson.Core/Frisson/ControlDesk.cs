using System.Text.Json;

using Frisson.Core.Agent.Device;
using Frisson.Core.Scheme.Device;

namespace Frisson.Core.Frisson;

internal class ControlDesk
{
    public int StrengthA { get; set; }
    public int StrengthB { get; set; }

    bool _blocked;

    public event Action? StateChanged;

    /// <summary>
    /// Update control state from external source JSON message.
    /// Fires StateChanged after updating.
    /// </summary>
    public void ApplyFromSource(string json)
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
    /// Serialize current control state to a pulse message for devices.
    /// </summary>
    public string ToPulseMessage()
    {
        // TODO: implement waveform/pulse message generation
        return JsonSerializer.Serialize(new
        {
            type = "msg",
            clientId = AppCore.DummyFrontendId.ToString(),
            targetId = "",
            message = $"strength-1+2+{StrengthA}+2+2+{StrengthB}"
        });
    }
}
