using System.Text.Json;

namespace CoyoteStudio.Core.Networking.Client.Scheme;

/// <summary>
/// Protocol scheme for creating device protocol messages to be sent to devices.
/// </summary>
internal static class DeviceOutputProtocolScheme
{
    /// <summary>
    /// Creates a strength message JSON string.
    /// Format: "strength-N+N+N+N" where N is a number.
    /// </summary>
    public static string CreateStrength(Guid deviceId, Guid remoteId, int strengthA, int strengthB, int limitA, int limitB)
    {
        var message = $"strength-{strengthA}+{strengthB}+{limitA}+{limitB}";
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
