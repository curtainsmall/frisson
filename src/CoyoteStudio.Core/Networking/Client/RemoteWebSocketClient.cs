using System;
using System.Collections.Generic;
using System.Text;

namespace CoyoteStudio.Core.Networking.Client;

internal class RemoteWebSocketClient : WebSocketClient
{
    public RemoteWebSocketClient(Guid id, Action? onDisposing) : base(id, onDisposing)
    {
    }
}
