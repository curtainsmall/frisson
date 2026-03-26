using System;
using System.Collections.Generic;
using System.Text;

using CoyoteStudio.Core.Network;
using CoyoteStudio.Shared;

using Microsoft.Extensions.DependencyInjection;

namespace CoyoteStudio.Core;

public static class CoreServiceExtensions
{
    public static void AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<WebSocketServer>();
        services.AddSingleton<IAppCore, AppCore>();
    }
}
