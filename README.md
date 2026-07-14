# Frisson

> 基于 WebSocket 的远程控制桌面客户端

Frisson 是一款使用 Avalonia UI 构建的跨平台桌面应用，通过 WebSocket 实现与远程设备的安全通信和控制。

## 功能

- **设备配对** — 通过二维码扫描快速配对远程设备（DG-LAB）
- **双通道控制** — 独立的 A/B 通道强度实时调节
- **远程控制** — Control Source 远程连接与状态同步
- **安全连接** — 连接前确认弹窗，防止未授权访问
- **多语言支持** — 简体中文、繁体中文、英文、日文
- **防火墙自动化** — 安装时自动配置 Windows 防火墙规则
- **日志系统** — WebSocket 通信日志记录与持久化

## 截图

（待添加）

## 下载

从 [Releases](https://github.com/curtainsmall/frisson/releases) 下载最新安装包。

提供两种构建变体：

| 变体 | 说明 |
|------|------|
| `Frisson-Setup-X.Y.Z.exe` | 标准版，依赖 .NET 运行时 |
| `Frisson-Setup-X.Y.Z-SelfContained.exe` | 自包含版，无需安装 .NET 运行时 |

## 构建

### 前置要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Inno Setup](https://jrsoftware.org/isdl.php)（用于生成安装包）
- Python 3.x（用于发布脚本）

### 构建并运行

```bash
# 构建
dotnet build src/Frisson.Desktop/Frisson.Desktop.csproj

# 运行（开发模式）
dotnet run --project src/Frisson.Desktop/Frisson.Desktop.csproj
```

### 生成安装包

```bash
# 使用发布脚本（版本号从 Git tag 自动读取）
python publish.py

# 或使用测试模式指定版本
python publish.py --version 0.1.0-beta
```

## 测试

```bash
dotnet test tests/Frisson.Core.Tests/Frisson.Core.Tests.csproj
```

## 技术栈

- **UI 框架**: [Avalonia UI](https://www.avaloniaui.net/) 11.3
- **MVVM**: CommunityToolkit.Mvvm
- **WebSocket**: Fleck
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **图标**: FontAwesome (Projektanker)
- **二维码**: QRCoder
- **安装包**: Inno Setup

## 协议

本项目基于 MIT 许可证开源。详见 [LICENSE](LICENSE) 文件。

---

# Frisson

> WebSocket-based remote control desktop client

Frisson is a cross-platform desktop application built with Avalonia UI, enabling secure communication and control with remote devices via WebSocket.

## Features

- **Device Pairing** — Quick QR code scanning for remote device pairing (DG-LAB)
- **Dual Channel Control** — Independent A/B channel intensity adjustment
- **Remote Control** — Control Source remote connection with state synchronization
- **Secure Connection** — Pre-connection confirmation dialog preventing unauthorized access
- **Multi-language** — Simplified Chinese, Traditional Chinese, English, Japanese
- **Firewall Automation** — Automatic Windows firewall rule configuration during installation
- **Logging** — WebSocket communication log recording and persistence

## Screenshots

(Coming soon)

## Download

Download the latest installer from [Releases](https://github.com/curtainsmall/frisson/releases).

Two build variants are available:

| Variant | Description |
|---------|-------------|
| `Frisson-Setup-X.Y.Z.exe` | Standard, requires .NET runtime |
| `Frisson-Setup-X.Y.Z-SelfContained.exe` | Self-contained, no .NET runtime needed |

## Build

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Inno Setup](https://jrsoftware.org/isdl.php) (for installer packaging)
- Python 3.x (for release script)

### Build and Run

```bash
# Build
dotnet build src/Frisson.Desktop/Frisson.Desktop.csproj

# Run (development mode)
dotnet run --project src/Frisson.Desktop/Frisson.Desktop.csproj
```

### Package Installer

```bash
# Using release script (version auto-detected from Git tag)
python publish.py

# Or test mode with explicit version
python publish.py --version 0.1.0-beta
```

## Tests

```bash
dotnet test tests/Frisson.Core.Tests/Frisson.Core.Tests.csproj
```

## Tech Stack

- **UI Framework**: [Avalonia UI](https://www.avaloniaui.net/) 11.3
- **MVVM**: CommunityToolkit.Mvvm
- **WebSocket**: Fleck
- **DI**: Microsoft.Extensions.DependencyInjection
- **Icons**: FontAwesome (Projektanker)
- **QR Code**: QRCoder
- **Installer**: Inno Setup

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
