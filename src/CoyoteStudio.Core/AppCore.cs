using CoyoteStudio.Core.Error;
using CoyoteStudio.Core.Networking.Client;
using CoyoteStudio.Core.Networking.Client.Scheme;
using CoyoteStudio.Core.Networking.Server;

namespace CoyoteStudio.Core;

public class AppCore : IDisposable
{
    private static AppCore? _instance;
    public static AppCore Instance => _instance ??= new AppCore();

    private readonly WebSocketManager _wsManager = new();

    public event Action<string>? ErrorOccurred;

    public ErrorMessager ErrorMessager { get; private init; } = new();

    private AppCore()
    {
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
}