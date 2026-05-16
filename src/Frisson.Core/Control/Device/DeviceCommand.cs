using System;
using System.Collections.Generic;
using System.Text;

namespace Frisson.Core.Control.Device;

internal enum DeviceCommandChannel
{
    A,
    B,
};

internal enum DeviceCommandCommand
{
    Strength,
    Pulse,
}

internal enum DeviceCommandMode
{
    Decrease,
    Increase,
    Set,
}

internal record DeviceCommand(DeviceCommandChannel Channel, DeviceCommandCommand Command, DeviceCommandMode Mode, int Value);

