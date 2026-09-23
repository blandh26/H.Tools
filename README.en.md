<div align="center">

<img src="src/HTools.App/Assets/toolbox.svg" width="96" alt="H.Tools" />

# H.Tools

[简体中文](README.md) | **English** | [日本語](README.ja.md) | [한국어](README.ko.md)

A lightweight Windows desktop toolbox that gathers everyday productivity and developer utilities into a single window.

![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D6)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![Avalonia](https://img.shields.io/badge/Avalonia-12-8B44AC)

</div>

## ✨ Features

### Utilities

| Tool | Description |
| --- | --- |
| 📋 **Multi Clipboard** | Save 10 frequently used snippets (text / images) and paste them instantly with global hotkeys `Ctrl+1` ~ `Ctrl+0` |
| 🖱 **Mouse Effect** | Shake the mouse to leave a plum-blossom trail; toggle it with one click from the App Center |
| ✂ **Screenshot** | Drag to select any region or hover to pick a window; annotate with rectangles, ellipses (outline / filled), lines, arrows, freehand, text and mosaic; copy, save as PNG or pin to screen; works across multiple monitors with mixed DPI scaling |
| 📊 **System Monitor** | Live CPU, memory and network charts, CPU / GPU temperatures, plus processor, motherboard, graphics, memory, storage and network adapter details |

### Dev Tools

| Tool | Description |
| --- | --- |
| 📮 **Mock Client** | A Postman-style request workspace — build HTTP requests by hand and inspect responses; keeps the last 100 requests |
| **Mock Server** | Start a server that returns one fixed response and logs every request |
| **Mock API Server** | Define rules by HTTP method + path that return preset status codes, Content-Type and bodies |
| **Static File Server** | Share a local folder over HTTP |
| **File Upload Server** | Serves an upload web form (also works with `curl -F`) and saves files to a chosen folder |
| **Webhook Receiver** | Receive and log incoming webhook requests (headers + body) |
| **Reverse Proxy** | Forward requests to a target URL and log the traffic |
| **Latency Simulator** | Add artificial delay before forwarding requests, for weak-network testing |

### App highlights

- **App Center**: every tool as a card, with search, pinning and drag-to-reorder
- **Custom tools**: add local programs (.exe) or URLs and manage them alongside the built-in tools
- **Multilingual**: 简体中文 / English / 日本語 / 한국어, switched instantly without restarting
- **Dark / light theme**: one-click toggle in the title bar
- **Always on top**, **start with Windows**, **minimize to system tray**
- **Persistent settings**: stored in a LiteDB database at `%LOCALAPPDATA%\HTools\settings.db`

## 🖥 Requirements

- Windows 10 / 11 (x64)
- Building from source requires the [.NET 10 SDK](https://dotnet.microsoft.com/download)

> Some hardware temperature sensors can only be read when running as administrator, or require motherboard / GPU vendor drivers.

## 🚀 Build & run

```bash
git clone https://github.com/blandh26/H.Tools.git
cd H.Tools
dotnet run --project src/HTools.App
```

Run the unit tests:

```bash
dotnet test
```

Publish a self-contained executable:

```bash
dotnet publish src/HTools.App -c Release -r win-x64 --self-contained
```

## 📁 Project layout

```
src/
├─ HTools.App       Avalonia UI (views, view models, app services)
├─ HTools.Core      Models, localization service & language files, LiteDB settings store
├─ HTools.Windows   Win32 interop: global hotkeys, screenshot overlay, clipboard, SVG rendering
└─ HTools.Server    Kestrel-based local server modules
tests/
└─ HTools.Core.Tests  xUnit unit tests
```

## 🧩 Tech stack

- [Avalonia UI 12](https://avaloniaui.net/) + Fluent theme
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- ASP.NET Core Kestrel (local server modules)
- [LiteDB](https://www.litedb.org/) (settings storage)
- [Svg.NET](https://github.com/svg-net/SVG) (icon rendering)
- [LibreHardwareMonitorLib](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor), System.Management (hardware info)

See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for third-party license information.

## 🌐 Adding a language

Language files live in `src/HTools.Core/Resources/Lang/` (JSON, embedded as resources). To add a language, copy an existing file, translate every value, and register it in `SupportedLanguages` in `LocalizationService`.

## 📮 Contact

- Email: [blandh26@gmail.com](mailto:blandh26@gmail.com)
- Website: [www.kimchicoder.com](https://www.kimchicoder.com)
