using System.Collections.Concurrent;
using System.Text.Json;

using CoyoteStudio.Core.Networking.Client;
using CoyoteStudio.Core.Networking.Client.Scheme;

namespace CoyoteStudio.Core.Networking.Server;

public sealed class DeviceStateChangedEventArgs : EventArgs
{
    public Guid DeviceId { get; }

    public DeviceStateChangedEventArgs(Guid deviceId)
    {
        DeviceId = deviceId;
    }
}

public sealed class ClientConnectionEventArgs : EventArgs
{
    public Guid ClientId { get; }
    public WebSocketClientKind Kind { get; }
    public WebClientConnectionStatus Status { get; }

    public ClientConnectionEventArgs(Guid clientId, WebSocketClientKind kind, WebClientConnectionStatus status)
    {
        ClientId = clientId;
        Kind = kind;
        Status = status;
    }
}

internal class WebSocketManager : IDisposable
{
    /// <summary>
    /// A placeholder UUID used as a dummy client ID in messages.
    /// </summary>
    public static readonly Guid DummyClientId = Guid.NewGuid();

    private readonly WebSocketServer _server = new();
    private readonly ConcurrentDictionary<Guid, WebSocketClient> _clients = new();
    private readonly CancellationTokenSource _heartbeatCts = new();
    private Task? _heartbeatTask;
    private int _port;

    public WebSocketManager()
    {
        AddEventHandlers();
    }

    /// <summary>
    /// Raised when a connected device's channel state (strength/limit) has changed.
    /// </summary>
    public event EventHandler<DeviceStateChangedEventArgs>? DeviceStateChanged;

    /// <summary>
    /// Raised when a client has successfully bound (identified as Device or Remote).
    /// </summary>
    public event EventHandler<ClientConnectionEventArgs>? ClientConnected;

    /// <summary>
    /// Raised when a client has disconnected.
    /// </summary>
    public event EventHandler<ClientConnectionEventArgs>? ClientDisconnected;

    /// <summary>
    /// Gets a client by its ID.
    /// </summary>
    /// <param name="clientId">The client GUID</param>
    /// <returns>The client instance, or null if not found</returns>
    public WebSocketClient? GetClient(Guid clientId)
    {
        _clients.TryGetValue(clientId, out var client);
        return client;
    }

    private async void OnClientConnected(object? _, WebSocketServer.ClientConnectedEventArgs e)
    {
        var client = Register(e.ClientId, e.OnDisposing);
        client.BindRequested += OnClientBindRequested;

        // Send bind message immediately upon connection
        var bindMessage = CreateBindMessage(client.Id);
        LoggerService.Instance.Log($"[Manager] Sending bind to {client.Id}");
        await SendAsync(client.Id, bindMessage);
    }

    private async void OnClientBindRequested(object? sender, BindRequestedEventArgs e)
    {
        if (sender is WebSocketClient client && _clients.ContainsKey(client.Id))
        {
            LoggerService.Instance.Log($"[Manager] Bind request from {client.Id}: {e.JsonDocument.RootElement}");

            // Determine client type based on message content
            // If the reply matches the bind message format (except message field), it's a Device
            if (IsDeviceBindReply(e.JsonDocument, client.Id))
            {
                LoggerService.Instance.Log($"[Manager] Device bind accepted: {client.Id}");
                UpliftToDeviceClient(client.Id, client);

                // Send bind success confirmation to device
                var ackMessage = CreateBindAckMessage(client.Id);
                await SendAsync(client.Id, ackMessage);
            }
            else
            {
                LoggerService.Instance.Log($"[Manager] Remote bind accepted: {client.Id}");
                // Send bind failure response before uplifting to remote
                var errorMessage = CreateBindErrorMessage(client.Id);
                await SendAsync(client.Id, errorMessage);

                UpliftToRemoteClient(client.Id, client);
            }
        }
    }

    private bool IsDeviceBindReply(JsonDocument jsonDoc, Guid expectedClientId)
    {
        var root = jsonDoc.RootElement;

        // Check if it has the expected fields for a device bind reply
        if (!root.TryGetProperty("type", out var typeElement) || typeElement.GetString() != "bind")
            return false;

        if (!root.TryGetProperty("clientId", out var clientIdElement))
            return false;

        // clientId should be the dummy client ID
        if (!clientIdElement.TryGetGuid(out var clientId) || clientId != DummyClientId)
            return false;

        if (!root.TryGetProperty("targetId", out var targetIdElement))
            return false;

        // targetId should match the client ID we sent the bind message to
        if (!targetIdElement.TryGetGuid(out var targetId) || targetId != expectedClientId)
            return false;

        if (!root.TryGetProperty("message", out _))
            return false;

        // message field can be anything, just need to exist
        return true;
    }

    private static string CreateBindMessage(Guid clientId)
    {
        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WriteString("type", "bind");
        writer.WriteString("clientId", clientId.ToString());
        writer.WriteString("targetId", "");
        writer.WriteString("message", "targetId");
        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string CreateBindAckMessage(Guid deviceId)
    {
        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WriteString("type", "bind");
        writer.WriteString("clientId", DummyClientId.ToString());
        writer.WriteString("targetId", deviceId.ToString());
        writer.WriteString("message", "200");
        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string CreateHeartbeatMessage(Guid deviceId)
    {
        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WriteString("type", "heartbeat");
        writer.WriteString("clientId", deviceId.ToString());
        writer.WriteString("targetId", DummyClientId.ToString());
        writer.WriteString("message", "200");
        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string CreateBindErrorMessage(Guid clientId)
    {
        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WriteString("type", "bind");
        writer.WriteString("clientId", DummyClientId.ToString());
        writer.WriteString("targetId", clientId.ToString());
        writer.WriteString("message", "400");
        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private void OnClientDisconnected(object? _, WebSocketServer.ClientDisconnectedEventArgs e)
    {
        TryUnregister(e.ClientId);
    }

    private void OnClientMessageReceived(object? _, WebSocketServer.ClientMessageReceivedEventArgs e)
    {
        LoggerService.Instance.Log($"[Manager] Message from {e.ClientId}: {e.Message}");
        if (TryGetClient(e.ClientId, out var client) && client is not null)
        {
            switch (client)
            {
                case DeviceWebSocketClient deviceClient:
                    deviceClient.Setup(e.Message);
                    break;
                case RemoteWebSocketClient remoteClient:
                    remoteClient.Setup(e.Message);
                    break;
                case WebSocketClient baseClient:
                    baseClient.Setup(e.Message);
                    break;
            }
        }
    }

    private void UpliftToRemoteClient(Guid clientId, WebSocketClient existingClient)
    {
        // Unsubscribe from old client's events
        existingClient.BindRequested -= OnClientBindRequested;

        // Create new RemoteWebSocketClient preserving the original OnDisposing action
        var remoteClient = new RemoteWebSocketClient(clientId, existingClient.OnDisposing);
        remoteClient.RemoteMessageReceived += OnRemoteMessageReceived;

        // Replace the client in the dictionary
        _clients[clientId] = remoteClient;

        ClientConnected?.Invoke(this, new ClientConnectionEventArgs(clientId, WebSocketClientKind.Remote, WebClientConnectionStatus.Connected));
    }

    private async void OnRemoteMessageReceived(object? sender, RemoteMessageReceivedEventArgs e)
    {
        if (sender is not RemoteWebSocketClient remoteClient)
            return;

        LoggerService.Instance.Log($"[Manager] Remote message from {remoteClient.Id}: {e.Scheme.Message?.GetType().Name ?? "null"}");

        switch (e.Scheme.Message)
        {
            case RemoteStrengthMessage strengthMsg:
                await ForwardRemoteStrengthAsync(remoteClient, e.Scheme.DeviceIds, strengthMsg);
                break;
            case RemoteConnectionMessage { MessageKind: RemoteConnectionMessageKind.Heartbeat }:
                await SendAsync(remoteClient.Id, "{\"type\":\"heartbeat\",\"message\":\"200\"}");
                break;
        }
    }

    private async Task ForwardRemoteStrengthAsync(RemoteWebSocketClient remoteClient, List<Guid> deviceIds, RemoteStrengthMessage msg)
    {
        int channel = msg.ChannelKind switch
        {
            DeviceChannelKind.A => 1,
            DeviceChannelKind.B => 2,
            _ => 1
        };

        LoggerService.Instance.Log($"[Manager] Forward strength from {remoteClient.Id} to [{string.Join(",", deviceIds)}] ch{channel} op={msg.OperationKind} val={msg.Value}");

        foreach (var deviceId in deviceIds)
        {
            if (!_clients.TryGetValue(deviceId, out var client) || client is not DeviceWebSocketClient)
                continue;

            string? payload = msg.OperationKind switch
            {
                RemoteStrengthOperationKind.VaryValue => DeviceOutputProtocolScheme.CreateStrengthStep(deviceId, DummyClientId, channel, msg.Value),
                RemoteStrengthOperationKind.SetValue => DeviceOutputProtocolScheme.CreateStrengthSet(deviceId, DummyClientId, channel, msg.Value),
                _ => null
            };

            if (payload is not null)
            {
                await SendAsync(deviceId, payload);
            }
        }
    }

    private void UpliftToDeviceClient(Guid clientId, WebSocketClient existingClient)
    {
        // Unsubscribe from old client's events
        existingClient.BindRequested -= OnClientBindRequested;

        // Create new DeviceWebSocketClient preserving the original OnDisposing action
        var deviceClient = new DeviceWebSocketClient(clientId, existingClient.OnDisposing);
        deviceClient.StateChanged += OnDeviceStateChanged;

        // Replace the client in the dictionary
        _clients[clientId] = deviceClient;

        ClientConnected?.Invoke(this, new ClientConnectionEventArgs(clientId, WebSocketClientKind.Device, WebClientConnectionStatus.Connected));
    }

    private void OnDeviceStateChanged(object? sender, EventArgs e)
    {
        if (sender is DeviceWebSocketClient deviceClient)
        {
            DeviceStateChanged?.Invoke(this, new DeviceStateChangedEventArgs(deviceClient.Id));
        }
    }

    public async Task RunAsync(int port)
    {
        _port = port;
        _heartbeatTask = HeartbeatLoopAsync(_heartbeatCts.Token);
        await _server.RunAsync(port);
    }

    /// <summary>
    /// Generates the QR code content string according to DG-LAB protocol.
    /// Format: https://www.dungeon-lab.com/app-download.php#DGLAB-SOCKET#ws://{ip}:{port}/{clientId}
    /// </summary>
    public string GetQrCodeContent()
    {
        var ip = GetLocalIpAddress();
        return $"https://www.dungeon-lab.com/app-download.php#DGLAB-SOCKET#ws://{ip}:{_port}/{DummyClientId}";
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                    continue;
                if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    continue;
                if (ni.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ||
                    ni.Description.Contains("VMware", StringComparison.OrdinalIgnoreCase) ||
                    ni.Description.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase) ||
                    ni.Description.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase) ||
                    ni.Description.Contains("TAP", StringComparison.OrdinalIgnoreCase) ||
                    ni.Description.Contains("Tunnel", StringComparison.OrdinalIgnoreCase) ||
                    ni.Description.Contains("VPN", StringComparison.OrdinalIgnoreCase))
                    continue;

                var props = ni.GetIPProperties();
                foreach (var ip in props.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        // Prefer interfaces with a default gateway (likely the main LAN adapter)
                        if (props.GatewayAddresses.Count > 0)
                            return ip.Address.ToString();
                    }
                }
            }

            // Fallback: any IPv4 address from active non-loopback interfaces
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                    continue;
                if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    continue;

                foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        return ip.Address.ToString();
                }
            }
        }
        catch
        {
            // Fallback if network lookup fails
        }
        return "127.0.0.1";
    }

    private async Task HeartbeatLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            foreach (var client in _clients.Values.OfType<DeviceWebSocketClient>())
            {
                var heartbeatMessage = CreateHeartbeatMessage(client.Id);
                LoggerService.Instance.Log($"[Manager] Sending heartbeat to {client.Id}");
                await SendAsync(client.Id, heartbeatMessage);
            }
        }
    }

    /// <summary>
    /// Sends a message to a specific client.
    /// </summary>
    /// <param name="clientId">The target client ID.</param>
    /// <param name="message">The message to send.</param>
    /// <returns>True if the message was sent successfully.</returns>
    public async Task<bool> SendAsync(Guid clientId, string message)
    {
        return await _server.SendAsync(clientId, message);
    }

    public void Dispose()
    {
        _heartbeatCts.Cancel();
        _heartbeatTask?.Wait(TimeSpan.FromSeconds(5));
        _heartbeatCts.Dispose();

        RemoveEventHandlers();

        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }
        _clients.Clear();

        _server.Dispose();
    }

    private WebSocketClient Register(Guid id, Action? onDisposing)
    {
        var client = new WebSocketClient(id, onDisposing);
        if (!_clients.TryAdd(id, client))
            throw new ArgumentException($"ID {id} already exists");
        return client;
    }

    private void TryUnregister(Guid id)
    {
        if (_clients.TryRemove(id, out var client))
        {
            var kind = client switch
            {
                DeviceWebSocketClient => WebSocketClientKind.Device,
                RemoteWebSocketClient => WebSocketClientKind.Remote,
                _ => WebSocketClientKind.Unknown
            };
            ClientDisconnected?.Invoke(this, new ClientConnectionEventArgs(id, kind, WebClientConnectionStatus.Disconnected));
            client.Dispose();
        }
    }

    private bool TryGetClient(Guid id, out WebSocketClient? client)
    {
        return _clients.TryGetValue(id, out client);
    }

    private void AddEventHandlers()
    {
        _server.ClientConnected += OnClientConnected;
        _server.ClientDisconnected += OnClientDisconnected;
        _server.ClientMessageReceived += OnClientMessageReceived;
    }

    private void RemoveEventHandlers()
    {
        _server.ClientConnected -= OnClientConnected;
        _server.ClientDisconnected -= OnClientDisconnected;
        _server.ClientMessageReceived -= OnClientMessageReceived;
    }
}
