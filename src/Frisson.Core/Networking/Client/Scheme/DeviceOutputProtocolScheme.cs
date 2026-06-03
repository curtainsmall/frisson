using System.Text.Json;

namespace Frisson.Core.Networking.Client.Scheme;

/// <summary>
/// Protocol scheme for creating device protocol messages to be sent to devices.
/// </summary>
internal static class DeviceOutputProtocolScheme
{
    [Obsolete("Legacy 4-value strength format is deprecated. Use CreateStrengthStep or CreateStrengthSet instead.")]
    public static string CreateStrength(Guid deviceId, Guid remoteId, int strengthA, int strengthB, int limitA, int limitB)
    {
        var message = $"strength-{strengthA}+{strengthB}+{limitA}+{limitB}";
        return SerializeToJson(deviceId, remoteId, message);
    }

    /// <summary>
    /// Creates a step-mode strength message JSON string.
    /// Format: "strength-channel+mode+delta" where:
    ///   - channel: 1 (A) or 2 (B)
    ///   - mode: 0 (decrease) for negative delta, 1 (increase) for positive delta
    ///   - delta: absolute value of strength change
    /// </summary>
    public static string CreateStrengthStep(Guid deviceId, Guid remoteId, int channel, int delta)
    {
        if (channel is not 1 and not 2)
            throw new ArgumentOutOfRangeException(nameof(channel), "Channel must be 1 (A) or 2 (B).");

        // Mode 0: decrease (delta < 0), Mode 1: increase (delta > 0)
        int mode = delta < 0 ? 0 : 1;
        int absDelta = Math.Abs(delta);
        
        var message = $"strength-{channel}+{mode}+{absDelta}";
        return SerializeToJson(deviceId, remoteId, message);
    }

    /// <summary>
    /// Creates a direct-set strength message JSON string.
    /// Format: "strength-channel+2+value" where channel is 1 (A) or 2 (B).
    /// </summary>
    public static string CreateStrengthSet(Guid deviceId, Guid remoteId, int channel, int value)
    {
        if (channel is not 1 and not 2)
            throw new ArgumentOutOfRangeException(nameof(channel), "Channel must be 1 (A) or 2 (B).");

        var message = $"strength-{channel}+2+{value}";
        return SerializeToJson(deviceId, remoteId, message);
    }

    /// <summary>
    /// Creates a pulse message JSON string.
    /// Format: "pulse-C:[Hex,...]" where C is A or B and each Hex is a 16-character hex string (8 bytes).
    /// </summary>
    /// <param name="deviceId">Target device ID.</param>
    /// <param name="remoteId">Source remote ID.</param>
    /// <param name="channel">Channel identifier, either 'A' or 'B'.</param>
    /// <param name="hexValues">Array of 16-character hex strings representing 8-byte values.</param>
    public static string CreatePulse(Guid deviceId, Guid remoteId, char channel, string[] hexValues)
    {
        // Validate that each hex value is exactly 16 characters (8 bytes)
        foreach (var hex in hexValues)
        {
            if (hex.Length != 16)
                throw new ArgumentException($"Hex value must be 16 characters (8 bytes), got: {hex}", nameof(hexValues));
        }

        var hexList = string.Join(",", hexValues);
        var message = $"pulse-{channel}:[{hexList}]";
        return SerializeToJson(deviceId, remoteId, message);
    }

    /// <summary>
    /// Creates a clear channel message JSON string.
    /// Format: "clear-N" where N is 1 (Channel A) or 2 (Channel B).
    /// </summary>
    /// <param name="deviceId">Target device ID.</param>
    /// <param name="remoteId">Source remote ID.</param>
    /// <param name="channel">Channel number, 1 for A or 2 for B.</param>
    public static string CreateClear(Guid deviceId, Guid remoteId, int channel)
    {
        if (channel is not 1 and not 2)
            throw new ArgumentOutOfRangeException(nameof(channel), "Channel must be 1 or 2.");

        var message = $"clear-{channel}";
        return SerializeToJson(deviceId, remoteId, message);
    }

    private static string SerializeToJson(Guid deviceId, Guid remoteId, string message)
    {
        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WriteString("type", "msg");
        writer.WriteString("targetId", deviceId.ToString());
        writer.WriteString("clientId", remoteId.ToString());
        writer.WriteString("message", message);
        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}
