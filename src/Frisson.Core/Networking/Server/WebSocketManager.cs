using System.Collections.Concurrent;

using Frisson.Core.Networking.Client;
using DeviceBindScheme = Frisson.Core.Scheme.Device.BindScheme;
using DeviceMsgScheme = Frisson.Core.Scheme.Device.MsgScheme;
using RemoteBindScheme = Frisson.Core.Scheme.Remote.BindScheme;
using RemoteErrorScheme = Frisson.Core.Scheme.Remote.ErrorScheme;
using RemoteMsgScheme = Frisson.Core.Scheme.Remote.MsgScheme;
using SchemeParser = Frisson.Core.Scheme.Scheme;

namespace Frisson.Core.Networking.Server;

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
    private readonly WebSocketServer _server = new();
    private readonly ConcurrentDictionary<Guid, WebSocketClient> _clients = new();
    private readonly PeriodicTimer _heartbeatTimer = new(TimeSpan.FromSeconds(30));
    private CancellationTokenSource? _heartbeatCts;

    private const string HeartbeatJson = """{"type":"heartbeat"}""";

    /// <summary>
    /// Per-instance dummy frontend UUID. Represents "Frisson as frontend" for DG-LAB Device binding.
    /// Generated once on construction. All Devices bind to this ID.
    /// </summary>
    public Guid DummyFrontendId { get; } = Guid.NewGuid();

    public WebSocketManager()
    {
        AddEventHandlers();
    }

    /// <summary>
    /// Raised when a client has connected.
    /// </summary>
    public event EventHandler<ClientConnectionEventArgs>? ClientConnected;

    /// <summary>
    /// Raised when a client has disconnected.
    /// </summary>
    public event EventHandler<ClientConnectionEventArgs>? ClientDisconnected;

    /// <summary>
    /// Gets a client by its ID.
    /// </summary>
    public WebSocketClient? GetClient(Guid clientId)
    {
        _clients.TryGetValue(clientId, out var client);
        return client;
    }

    /// <summary>
    /// Disconnects a specific client by ID.
    /// </summary>
    public void DisconnectClient(Guid clientId)
    {
        LoggerService.Instance.Log($"[Manager] Force disconnecting client {clientId}");
        TryUnregister(clientId);
    }

    /// <summary>
    /// Sends a message to a specific client.
    /// </summary>
    public async Task<bool> SendAsync(Guid clientId, string message)
    {
        return await _server.SendAsync(clientId, message);
    }

    public async Task RunAsync(int port)
    {
        _heartbeatCts = new CancellationTokenSource();
        _ = RunHeartbeatLoopAsync(_heartbeatCts.Token);
        await _server.RunAsync(port);
    }

    public void Dispose()
    {
        _heartbeatCts?.Cancel();
        _heartbeatTimer.Dispose();
        _heartbeatCts?.Dispose();
        RemoveEventHandlers();

        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }
        _clients.Clear();

        _server.Dispose();
    }

    private void OnClientConnected(object? _, WebSocketServer.ClientConnectedEventArgs e)
    {
        var client = Register(e.ClientId, e.OnDisposing);
        client.SendFunc = message => SendAsync(e.ClientId, message);
        LoggerService.Instance.Log($"[Manager] Client connected: {e.ClientId}");

        // Send initial bind message (same for ALL new clients)
        var initialBind = new DeviceBindScheme
        {
            ClientId = e.ClientId.ToString(),
            TargetId = string.Empty,
            Message = "targetId"
        };
        _ = SendAsync(e.ClientId, initialBind.ToJson());
    }

    private void OnClientDisconnected(object? _, WebSocketServer.ClientDisconnectedEventArgs e)
    {
        TryUnregister(e.ClientId);
    }

    private void OnClientMessageReceived(object? _, WebSocketServer.ClientMessageReceivedEventArgs e)
    {
        var client = GetClient(e.ClientId);
        if (client == null)
            return;

        // If still base WebSocketClient -> bind reply handling
        if (client is not DeviceWebSocketClient and not RemoteWebSocketClient)
        {
            HandleBindReply(e.ClientId, e.Message, client);
            return;
        }

        // Dispatch by client type
        if (client is DeviceWebSocketClient)
            HandleDeviceMessage(e.ClientId, e.Message);
        else if (client is RemoteWebSocketClient)
            HandleRemoteMessage(e.ClientId, e.Message);
    }

    /// <summary>
    /// Handles bind reply from an unbound client (base WebSocketClient).
    /// Determines whether the client is Device or Remote based on reply format.
    /// </summary>
    private void HandleBindReply(Guid clientId, string json, WebSocketClient client)
    {
        // Try Device bind: has clientId + targetId fields
        var deviceBind = DeviceBindScheme.TryParseDeviceBind(json);
        if (deviceBind != null)
        {
            // clientId should echo back the WS connection ID we sent
            if (deviceBind.ClientId == clientId.ToString())
            {
                UpgradeToDevice(clientId, client);
                var confirm = new DeviceBindScheme
                {
                    ClientId = clientId.ToString(),
                    TargetId = deviceBind.TargetId,
                    Message = "200"
                };
                _ = SendAsync(clientId, confirm.ToJson());
                LoggerService.Instance.Log($"[Manager] Device bound: {clientId}");
                return;
            }
        }

        // Try Remote bind: has only type + id fields (no clientId/targetId)
        var remoteBind = RemoteBindScheme.TryParse(json);
        if (remoteBind != null)
        {
            UpgradeToRemote(clientId, client);
            _ = SendAsync(clientId, remoteBind.ToJson()); // echo back as confirm
            LoggerService.Instance.Log($"[Manager] Remote bound: {clientId}");
            return;
        }

        // Invalid bind reply
        LoggerService.Instance.Log($"[Manager] Invalid bind reply from {clientId}: {json}");
        var error = new RemoteErrorScheme { Message = "Invalid bind reply" };
        _ = SendAsync(clientId, error.ToJson());
    }

    /// <summary>
    /// Handles messages from a bound Device client.
    /// </summary>
    private void HandleDeviceMessage(Guid clientId, string json)
    {
        var msg = DeviceMsgScheme.TryParse(json);
        if (msg == null)
        {
            LoggerService.Instance.Log($"[Manager] Invalid Device message from {clientId}: {json}");
            return;
        }
        LoggerService.Instance.Log($"[Manager] Device msg from {clientId}: {msg.Message}");
        // Parsed data is available on msg object — future: surface to Control/ layer
    }

    /// <summary>
    /// Handles messages from a bound Remote client.
    /// </summary>
    private void HandleRemoteMessage(Guid clientId, string json)
    {
        var scheme = SchemeParser.Parse(json);
        if (scheme == null)
        {
            LoggerService.Instance.Log($"[Manager] Unknown Remote message from {clientId}: {json}");
            var error = new RemoteErrorScheme { Message = "Unknown message type" };
            _ = SendAsync(clientId, error.ToJson());
            return;
        }

        if (scheme is RemoteMsgScheme)
        {
            // Valid but ignored (protocol compatibility)
            return;
        }

        LoggerService.Instance.Log($"[Manager] Remote msg from {clientId}: type={scheme.Type}");
        // Parsed data is available on scheme object — future: surface to Control/ layer
    }

    private async Task RunHeartbeatLoopAsync(CancellationToken ct)
    {
        try
        {
            while (await _heartbeatTimer.WaitForNextTickAsync(ct))
            {
                foreach (var (id, client) in _clients)
                {
                    if (client is RemoteWebSocketClient)
                        _ = SendAsync(id, HeartbeatJson);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private WebSocketClient Register(Guid id, Action? onDisposing)
    {
        var client = new WebSocketClient(onDisposing);
        if (!_clients.TryAdd(id, client))
            throw new ArgumentException($"ID {id} already exists");
        return client;
    }

    /// <summary>
    /// Upgrades a base WebSocketClient to a DeviceWebSocketClient (same Guid).
    /// </summary>
    private void UpgradeToDevice(Guid id, WebSocketClient existing)
    {
        var deviceClient = new DeviceWebSocketClient(existing);
        _clients.TryRemove(id, out _);
        _clients.TryAdd(id, deviceClient);
        existing.Dispose();
    }

    /// <summary>
    /// Upgrades a base WebSocketClient to a RemoteWebSocketClient (same Guid).
    /// </summary>
    private void UpgradeToRemote(Guid id, WebSocketClient existing)
    {
        var remoteClient = new RemoteWebSocketClient(existing);
        _clients.TryRemove(id, out _);
        _clients.TryAdd(id, remoteClient);
        existing.Dispose();
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
