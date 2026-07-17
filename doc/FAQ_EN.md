# FAQ

> [简体中文](FAQ.md)

## Can't connect to DG-LAB App?

- Make sure your phone and Frisson are on the same local network
- Check that port 6969 is not blocked by the firewall (the installer automatically configures firewall rules)

## What's the difference between Standard and Self-contained?

- **Standard**: Requires .NET 10 Runtime, smaller installer size
- **Self-contained**: No additional runtime needed, ready to run, but larger installer

## How does an external program control via JSON protocol?

External programs connect to Frisson via WebSocket and send JSON-formatted messages to control device strength and other parameters. Refer to the `demo/tetris.py` example script in the `demo/` directory.

## Can't adjust strength after connecting?

- Make sure the device is successfully paired via the DG-LAB App.
- Frisson allows you to set a different upper limit than the one set in DG-LAB App. If a new strength exceeds the DG-LAB App's upper limit, it ignores the control message, making it impossible to adjust strength.
> Go to Settings → Actuator page and check "Use Actuator Limits" to keep the limits in sync.

## Which devices are supported?

Currently supports DG-LAB Coyote 3.0 devices, connected via WebSocket through the DG-LAB App.

## How to view communication logs?

Go to Settings → About in the app to view WebSocket communication logs.

---

[Back to Home](../README.md)
