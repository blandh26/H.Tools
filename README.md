<div align="center">

<img src="src/HTools.App/Assets/toolbox.svg" width="96" alt="H.Tools" />

# H.Tools 工具箱

**简体中文** | [English](README.en.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

一款面向 Windows 的轻量桌面工具箱，把日常办公和开发调试常用的小工具集中到一个窗口里。

![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D6)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![Avalonia](https://img.shields.io/badge/Avalonia-12-8B44AC)

</div>

## ✨ 功能一览

### 常用工具

| 工具 | 说明 |
| --- | --- |
| 📋 **多剪贴板** | 保存 10 条常用内容（文字 / 图片），通过全局快捷键 `Ctrl+1` ~ `Ctrl+0` 快速粘贴 |
| 🖱 **鼠标特效** | 快速晃动鼠标时出现梅花残影，可在应用中心一键开关 |
| ✂ **截图工具** | 框选任意区域或悬停点选窗口；支持矩形、椭圆（空心 / 填充）、直线、箭头、画笔、文字、马赛克标注；可复制、保存为 PNG、钉在屏幕上；支持多显示器与不同 DPI 缩放 |
| 📊 **系统监控** | 实时查看 CPU、内存、网络上下行曲线，CPU / GPU 温度，以及处理器、主板、显卡、内存、硬盘、网卡等硬件信息 |

### 开发工具

| 工具 | 说明 |
| --- | --- |
| 📮 **Mock 客户端** | 类 Postman 的请求工作区，手动构造 HTTP 请求并查看响应，保留最近 100 条历史 |
| **Mock 服务端** | 启动一个返回固定响应的服务器，记录收到的每个请求 |
| **Mock API 服务器** | 按 HTTP 方法 + 路径定义规则，返回预设的状态码、Content-Type 与响应体 |
| **静态文件服务器** | 把本地文件夹通过 HTTP 共享出去 |
| **文件上传服务器** | 提供上传网页表单，也支持 `curl -F` 上传，文件保存到指定目录 |
| **Webhook 接收器** | 接收并记录外部系统推送的 Webhook 请求（请求头 + 请求体） |
| **反向代理** | 将请求转发到目标地址，并记录往返内容 |
| **延迟模拟** | 在转发前人为增加延迟，用于测试弱网场景 |

### 应用特性

- **应用中心**：卡片式展示全部工具，支持搜索、置顶、拖动排序
- **自定义工具**：可添加本地程序（.exe）或网址，与内置工具一起管理
- **多语言**：简体中文 / English / 日本語 / 한국어，切换后界面即时刷新
- **深色 / 浅色主题**：标题栏一键切换
- **窗口置顶**、**开机自启**、**最小化到系统托盘**
- **设置持久化**：所有设置保存在 LiteDB 数据库 `%LOCALAPPDATA%\HTools\settings.db`

## 🖥 运行环境

- Windows 10 / 11（x64）
- 从源码构建需要 [.NET 10 SDK](https://dotnet.microsoft.com/download)

> 部分硬件温度传感器需要以管理员身份运行，或依赖主板 / 显卡厂商驱动才能读取。

## 🚀 构建与运行

```bash
git clone https://github.com/blandh26/H.Tools.git
cd H.Tools
dotnet run --project src/HTools.App
```

运行单元测试：

```bash
dotnet test
```

发布为独立可执行程序：

```bash
dotnet publish src/HTools.App -c Release -r win-x64 --self-contained
```

## 📁 项目结构

```
src/
├─ HTools.App       Avalonia 界面（视图、视图模型、应用服务）
├─ HTools.Core      模型、多语言服务与语言文件、LiteDB 设置存储
├─ HTools.Windows   Win32 互操作：全局热键、截图覆盖层、剪贴板、SVG 渲染
└─ HTools.Server    基于 Kestrel 的各类本地服务器模块
tests/
└─ HTools.Core.Tests  xUnit 单元测试
```

## 🧩 技术栈

- [Avalonia UI 12](https://avaloniaui.net/) + Fluent 主题
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- ASP.NET Core Kestrel（本地服务器模块）
- [LiteDB](https://www.litedb.org/)（设置存储）
- [Svg.NET](https://github.com/svg-net/SVG)（图标渲染）
- [LibreHardwareMonitorLib](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)、System.Management（硬件信息）

第三方组件许可说明见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。

## 🌐 添加新语言

语言文件位于 `src/HTools.Core/Resources/Lang/`（JSON 格式，作为嵌入资源打包）。新增语言时复制一份现有文件翻译全部键值，并在 `LocalizationService` 的 `SupportedLanguages` 中注册即可。

## 📮 联系作者

- 邮箱：[blandh26@gmail.com](mailto:blandh26@gmail.com)
- 网站：[www.kimchicoder.com](https://www.kimchicoder.com)
