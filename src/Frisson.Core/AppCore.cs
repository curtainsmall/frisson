using Frisson.Core.Error;
using Frisson.Core.Networking.Client;
using Frisson.Core.Networking.Client.Scheme;
using Frisson.Core.Networking.Server;

namespace Frisson.Core;

public class AppCore : IDisposable
{
    private static AppCore? _instance;
    public static AppCore Instance => _instance ??= new AppCore();

    private readonly WebSocketManager _wsManager = new();

    public event Action<string>? ErrorOccurred;
    public event EventHandler<DeviceStateChangedEventArgs>? DeviceStateChanged;
    public event EventHandler<ClientConnectionEventArgs>? ClientConnected;
    public event EventHandler<ClientConnectionEventArgs>? ClientDisconnected;

    public ErrorMessager ErrorMessager { get; private init; } = new();

    private AppCore()
    {
        _wsManager.DeviceStateChanged += (s, e) => DeviceStateChanged?.Invoke(s, e);
        _wsManager.ClientConnected += (s, e) => ClientConnected?.Invoke(s, e);
        _wsManager.ClientDisconnected += (s, e) => ClientDisconnected?.Invoke(s, e);
    }

    public void Startup(int port)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _wsManager.RunAsync(port);
            }
            catch (ProtocolSchemeException e)
            {
                ErrorMessager.Send(new ErrorMessage(ErrorCode.InvalidJson, e.Message));
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                ErrorMessager.Send(new ErrorMessage(ErrorCode.Unknown, e.Message));
            }
        });
    }

    public void Dispose()
    {
        ErrorMessager.Dispose();
        _wsManager.Dispose();
    }

    /// <summary>
    /// Gets the strength value for a specific channel of a device client.
    /// </summary>
    /// <param name="clientId">The device client ID</param>
    /// <param name="channel">Channel identifier: 'A' or 'B'</param>
    /// <returns>Channel strength value, or 0 if client not found or not a device</returns>
    public int GetDeviceChannelStrength(Guid? clientId, char channel)
    {
        if (!clientId.HasValue)
            return 0;

        var client = _wsManager.GetClient(clientId.Value);
        if (client is not DeviceWebSocketClient deviceClient)
            return 0;

        return channel switch
        {
            'A' => deviceClient.ChannelA.Strength,
            'B' => deviceClient.ChannelB.Strength,
            _ => 0
        };
    }

    /// <summary>
    /// Gets the limit value for a specific channel of a device client.
    /// </summary>
    /// <param name="clientId">The device client ID</param>
    /// <param name="channel">Channel identifier: 'A' or 'B'</param>
    /// <returns>Channel limit value, or 100 if client not found or not a device</returns>
    public int GetDeviceChannelLimit(Guid? clientId, char channel)
    {
        if (!clientId.HasValue)
            return 100;

        var client = _wsManager.GetClient(clientId.Value);
        if (client is not DeviceWebSocketClient deviceClient)
            return 100;

        return channel switch
        {
            'A' => deviceClient.ChannelA.Limit,
            'B' => deviceClient.ChannelB.Limit,
            _ => 100
        };
    }

    /// <summary>
    /// Sends a step-mode strength adjustment to the device.
    /// Format: strength-channel+1+delta
    /// </summary>
    public async Task SendStrengthStepAsync(Guid deviceId, int channel, int delta)
    {
        var message = DeviceOutputProtocolScheme.CreateStrengthStep(deviceId, WebSocketManager.DummyClientId, channel, delta);
        await _wsManager.SendAsync(deviceId, message);
    }

    /// <summary>
    /// Sends a direct-set strength command to the device.
    /// Format: strength-channel+2+value
    /// </summary>
    public async Task SendStrengthSetAsync(Guid deviceId, int channel, int value)
    {
        var message = DeviceOutputProtocolScheme.CreateStrengthSet(deviceId, WebSocketManager.DummyClientId, channel, value);
        await _wsManager.SendAsync(deviceId, message);
    }

    /// <summary>
    /// Generates the QR code content string for APP scanning.
    /// </summary>
    public string GetQrCodeContent() => _wsManager.GetQrCodeContent();
}