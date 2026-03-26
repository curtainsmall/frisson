using System;

using CommunityToolkit.Mvvm.Messaging;

using CoyoteStudio.Shared.Message;

namespace CoyoteStudio.App.Message;

internal class Messager : IMessager
{
    void IMessager.Send<T>(T message)
    {
        WeakReferenceMessenger.Default.Send(message);
    }
}