# Frisson

>[English](doc/README_EN.md)

Frisson能够连接至郊狼3.0设备，控制设备强度与上限。同时支持通过JSON协议接收外部程序的控制消息。

## 功能

- **连接 DG-LAB 设备** — 通过WebSocket与DG-LAB App建立连接
- **强度与上下限控制** — 调节与控制各通道强度及上下限
- **外部程序远程控制** — 通过JSON协议接收来自外部程序的控制消息

## 下载

从 [Releases](https://github.com/curtainsmall/frisson/releases) 下载最新安装包。

| 变体 | 说明 | 系统要求 |
|------|------|----------|
| `Frisson-Setup-X.Y.Z.exe` | 标准版 | Windows 10/11（64位），需 [.NET 10 运行时](https://dotnet.microsoft.com/download) |
| `Frisson-Setup-X.Y.Z-SelfContained.exe` | 自包含版 | Windows 10/11（64位），无需额外安装 |

[常见问题](doc/FAQ.md)。

## 协议

本项目基于 GNU Affero General Public License v3.0 开源。详见 [LICENSE](LICENSE) 文件。