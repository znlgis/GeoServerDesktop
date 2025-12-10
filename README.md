# GeoServerDesktop

[English](#english) | [中文](#中文)

---

## 中文

一个用于通过友好界面管理 GeoServer 实例的跨平台桌面应用程序。

### 概述

GeoServerDesktop 提供了一个现代化的跨平台桌面工具，通过 REST API 与 GeoServer 进行交互。采用 Avalonia UI 和 .NET 8 构建，可在 Windows、macOS 和 Linux 上运行。

### 功能特性

- **工作空间管理**：创建、查看、更新和删除 GeoServer 工作空间
- **数据存储管理**：管理 PostGIS、Shapefile 和其他数据存储
- **图层管理**：发布、配置和管理图层及要素类型
- **样式管理**：上传、编辑和管理 SLD 样式
- **图层组**：创建和管理图层组
- **地图预览**：使用 Mapsui 内置地图预览 WMS 图层
- **连接管理**：保存和管理多个 GeoServer 连接

### 架构设计

解决方案由两个主要项目组成：

#### 1. GeoServerDesktop.GeoServerClient

一个 .NET Standard 2.0 类库，提供完整的 GeoServer REST API 客户端：

- **HTTP 层**：处理身份验证、请求和错误处理
- **模型**：所有 GeoServer 资源的强类型模型
- **服务**：每种资源类型的高级服务 API
- **预览服务**：生成用于地图可视化的 WMS URL

#### 2. GeoServerDesktop.App

基于 Avalonia 的 .NET 8 桌面应用程序：

- **MVVM 架构**：清晰的关注点分离
- **树视图导航**：分层资源浏览器
- **详细视图**：每种资源类型的丰富编辑器
- **地图集成**：Mapsui 驱动的地图预览
- **跨平台**：可在 Windows、macOS 和 Linux 上运行

### 技术栈

- **语言**：C# 12
- **框架**：.NET 8, .NET Standard 2.0
- **UI**：Avalonia UI
- **MVVM**：CommunityToolkit.Mvvm
- **地图**：Mapsui + Mapsui.Avalonia
- **JSON**：Newtonsoft.Json

### 快速开始

#### 前置要求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 或更高版本
- 运行中的 GeoServer 实例（用于测试）

#### 构建解决方案

```bash
# 克隆仓库
git clone https://github.com/znlgis/GeoServerDesktop.git
cd GeoServerDesktop

# 恢复依赖项
dotnet restore

# 构建解决方案
dotnet build

# 运行应用程序
dotnet run --project src/GeoServerDesktop.App/GeoServerDesktop.App.csproj
```

#### 开发环境

解决方案可以在以下 IDE 中打开：
- Visual Studio 2022 或更高版本
- JetBrains Rider
- Visual Studio Code（需要 C# 扩展）

### 项目结构

```
GeoServerDesktop/
├── src/
│   ├── GeoServerDesktop.sln                # 解决方案文件
│   ├── GeoServerDesktop.GeoServerClient/   # REST API 客户端库
│   │   ├── Configuration/                  # 客户端配置
│   │   ├── Http/                          # HTTP 客户端基础设施
│   │   ├── Models/                        # 数据模型
│   │   └── Services/                      # 服务层
│   └── GeoServerDesktop.App/              # Avalonia 桌面应用程序
│       ├── Views/                         # XAML 视图
│       ├── ViewModels/                    # 视图模型
│       ├── Services/                      # 应用服务
│       ├── Models/                        # UI 模型
│       └── Assets/                        # 资源文件
├── .editorconfig                          # 代码风格配置
├── CHINESE_COMMENTS_PROGRESS.md           # 中文注释进度跟踪
└── README.md                              # 本文件
```

### 使用说明

#### 连接到 GeoServer

1. 启动应用程序
2. 导航到连接设置
3. 输入您的 GeoServer 详细信息：
   - 基础 URL（例如：`http://localhost:8080/geoserver`）
   - 用户名（默认：`admin`）
   - 密码（默认：`geoserver`）
4. 点击连接

#### 管理资源

- **工作空间**：在树视图中浏览和管理工作空间
- **数据存储**：查看和配置数据源
- **图层**：发布新图层，编辑现有图层
- **样式**：上传 SLD 文件或编辑样式
- **预览**：点击任何图层在地图上预览

### API 使用

GeoServerClient 库可以独立用于您自己的项目：

```csharp
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.GeoServerClient.Services;

// 配置连接
var options = new GeoServerClientOptions
{
    BaseUrl = "http://localhost:8080/geoserver",
    Username = "admin",
    Password = "geoserver"
};

// 创建工厂
var factory = new GeoServerClientFactory(options);

// 使用服务
var workspaceService = factory.CreateWorkspaceService();
var workspaces = await workspaceService.GetWorkspacesAsync();

foreach (var workspace in workspaces)
{
    Console.WriteLine($"工作空间：{workspace.Name}");
}

// 清理资源
factory.Dispose();
```

### 配置

#### 应用程序设置

设置存储在本地 JSON 配置文件中：
- Windows：`%APPDATA%\GeoServerDesktop\settings.json`
- macOS：`~/Library/Application Support/GeoServerDesktop/settings.json`
- Linux：`~/.config/GeoServerDesktop/settings.json`

#### 代码风格

项目使用 `.editorconfig` 强制执行一致的编码标准：
- 缩进使用 4 个空格
- Allman 大括号风格
- 私有字段使用 `_` 前缀
- 公共 API 使用 XML 文档注释（中文）

### 贡献指南

欢迎贡献！请遵循以下准则：

1. 遵循 `.editorconfig` 中定义的代码风格
2. 为公共 API 添加 XML 文档注释（中文）
3. 在桌面应用中遵循 MVVM 模式
4. 保持客户端库独立于 UI 关注点
5. 编写有意义的提交消息

### GeoServer 兼容性

此客户端设计用于 GeoServer 2.x REST API。已在以下版本测试：
- GeoServer 2.20+
- GeoServer 2.21+
- GeoServer 2.22+

### 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件。

### 路线图

计划中的功能：
- [ ] 高级图层配置
- [ ] 覆盖范围存储支持
- [ ] WFS 服务配置
- [ ] 安全和身份验证管理
- [ ] 批量操作
- [ ] 导入/导出功能
- [ ] 设置同步
- [ ] 插件系统

### 支持

如有问题、疑问或贡献：
- **问题反馈**：[GitHub Issues](https://github.com/znlgis/GeoServerDesktop/issues)
- **讨论交流**：[GitHub Discussions](https://github.com/znlgis/GeoServerDesktop/discussions)

### 致谢

- [GeoServer](http://geoserver.org/) - 用于共享地理空间数据的开源服务器
- [Avalonia UI](https://avaloniaui.net/) - 跨平台 XAML 框架
- [Mapsui](https://mapsui.com/) - .NET 应用的地图组件

### 资源链接

- [GeoServer REST API 文档](https://docs.geoserver.org/latest/en/user/rest/)
- [Avalonia UI 文档](https://docs.avaloniaui.net/)
- [Mapsui 文档](https://mapsui.com/documentation/)

---

## English

A cross-platform desktop application for managing GeoServer instances through a user-friendly interface.

### Overview

GeoServerDesktop provides a modern, cross-platform desktop tool for interacting with GeoServer via its REST API. Built with Avalonia UI and .NET 8, it runs on Windows, macOS, and Linux.

### Features

- **Workspace Management**: Create, view, update, and delete GeoServer workspaces
- **Data Store Management**: Manage PostGIS, Shapefile, and other data stores
- **Layer Management**: Publish, configure, and manage layers and feature types
- **Style Management**: Upload, edit, and manage SLD styles
- **Layer Groups**: Create and manage layer groups
- **Map Preview**: Built-in map preview using Mapsui for WMS layers
- **Connection Management**: Save and manage multiple GeoServer connections

### Architecture

The solution consists of two main projects:

#### 1. GeoServerDesktop.GeoServerClient

A .NET Standard 2.0 class library that provides a complete REST API client for GeoServer:

- **HTTP Layer**: Handles authentication, requests, and error handling
- **Models**: Strongly-typed models for all GeoServer resources
- **Services**: High-level service APIs for each resource type
- **Preview Service**: Generates WMS URLs for map visualization

#### 2. GeoServerDesktop.App

An Avalonia-based .NET 8 desktop application:

- **MVVM Architecture**: Clean separation of concerns
- **Tree View Navigation**: Hierarchical resource browser
- **Detail Views**: Rich editors for each resource type
- **Map Integration**: Mapsui-powered map preview
- **Cross-Platform**: Runs on Windows, macOS, and Linux

### Technology Stack

- **Language**: C# 12
- **Frameworks**: .NET 8, .NET Standard 2.0
- **UI**: Avalonia UI
- **MVVM**: CommunityToolkit.Mvvm
- **Maps**: Mapsui + Mapsui.Avalonia
- **JSON**: Newtonsoft.Json

### Getting Started

#### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- A running GeoServer instance (for testing)

#### Building the Solution

```bash
# Clone the repository
git clone https://github.com/znlgis/GeoServerDesktop.git
cd GeoServerDesktop

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the application
dotnet run --project src/GeoServerDesktop.App/GeoServerDesktop.App.csproj
```

#### Development

The solution can be opened in:
- Visual Studio 2022 or later
- JetBrains Rider
- Visual Studio Code (with C# extension)

### Project Structure

```
GeoServerDesktop/
├── src/
│   ├── GeoServerDesktop.sln                # Solution file
│   ├── GeoServerDesktop.GeoServerClient/   # REST API client library
│   │   ├── Configuration/                  # Client configuration
│   │   ├── Http/                          # HTTP client infrastructure
│   │   ├── Models/                        # Data models
│   │   └── Services/                      # Service layer
│   └── GeoServerDesktop.App/              # Avalonia desktop application
│       ├── Views/                         # XAML views
│       ├── ViewModels/                    # View models
│       ├── Services/                      # Application services
│       ├── Models/                        # UI models
│       └── Assets/                        # Resources
├── .editorconfig                          # Code style configuration
├── CHINESE_COMMENTS_PROGRESS.md           # Chinese comments progress tracker
└── README.md                              # This file
```

### Usage

#### Connecting to GeoServer

1. Launch the application
2. Navigate to Connection settings
3. Enter your GeoServer details:
   - Base URL (e.g., `http://localhost:8080/geoserver`)
   - Username (default: `admin`)
   - Password (default: `geoserver`)
4. Click Connect

#### Managing Resources

- **Workspaces**: Browse and manage workspaces in the tree view
- **Data Stores**: View and configure data sources
- **Layers**: Publish new layers, edit existing ones
- **Styles**: Upload SLD files or edit styles
- **Preview**: Click any layer to preview it on the map

### API Usage

The GeoServerClient library can be used independently in your own projects:

```csharp
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.GeoServerClient.Services;

// Configure connection
var options = new GeoServerClientOptions
{
    BaseUrl = "http://localhost:8080/geoserver",
    Username = "admin",
    Password = "geoserver"
};

// Create factory
var factory = new GeoServerClientFactory(options);

// Use services
var workspaceService = factory.CreateWorkspaceService();
var workspaces = await workspaceService.GetWorkspacesAsync();

foreach (var workspace in workspaces)
{
    Console.WriteLine($"Workspace: {workspace.Name}");
}

// Clean up
factory.Dispose();
```

### Configuration

#### Application Settings

Settings are stored in a local JSON configuration file:
- Windows: `%APPDATA%\GeoServerDesktop\settings.json`
- macOS: `~/Library/Application Support/GeoServerDesktop/settings.json`
- Linux: `~/.config/GeoServerDesktop/settings.json`

#### Code Style

The project uses `.editorconfig` to enforce consistent coding standards:
- 4 spaces for indentation
- Allman brace style
- Private fields with `_` prefix
- XML documentation for public APIs (in Chinese)

### Contributing

Contributions are welcome! Please follow these guidelines:

1. Follow the code style defined in `.editorconfig`
2. Add XML documentation for public APIs (in Chinese)
3. Follow the MVVM pattern in the desktop app
4. Keep the client library independent of UI concerns
5. Write meaningful commit messages

### GeoServer Compatibility

This client is designed to work with GeoServer 2.x REST API. It has been tested with:
- GeoServer 2.20+
- GeoServer 2.21+
- GeoServer 2.22+

### License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

### Roadmap

Planned features:
- [ ] Advanced layer configuration
- [ ] Coverage store support
- [ ] WFS service configuration
- [ ] Security and authentication management
- [ ] Bulk operations
- [ ] Import/export functionality
- [ ] Settings synchronization
- [ ] Plugin system

### Support

For issues, questions, or contributions:
- **Issues**: [GitHub Issues](https://github.com/znlgis/GeoServerDesktop/issues)
- **Discussions**: [GitHub Discussions](https://github.com/znlgis/GeoServerDesktop/discussions)

### Acknowledgments

- [GeoServer](http://geoserver.org/) - Open source server for sharing geospatial data
- [Avalonia UI](https://avaloniaui.net/) - Cross-platform XAML framework
- [Mapsui](https://mapsui.com/) - Map component for .NET applications

### Resources

- [GeoServer REST API Documentation](https://docs.geoserver.org/latest/en/user/rest/)
- [Avalonia UI Documentation](https://docs.avaloniaui.net/)
- [Mapsui Documentation](https://mapsui.com/documentation/)
