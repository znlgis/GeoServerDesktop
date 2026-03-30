# GeoServerDesktop

[English](#english) | [中文](#中文)

---

## 中文

一个用于通过友好界面管理 GeoServer 实例的跨平台桌面应用程序。

### 概述

GeoServerDesktop 提供了一个现代化的跨平台桌面工具，通过 REST API 与 GeoServer 进行交互。采用 Avalonia UI 和 .NET 8 构建，可在 Windows、macOS 和 Linux 上运行。应用程序支持中英文界面切换。

### 功能特性

#### 数据管理
- **图层预览**：使用 Mapsui 内置地图预览 WMS 图层
- **工作空间管理**：创建、查看、更新和删除 GeoServer 工作空间
- **数据存储管理**：管理 PostGIS、Shapefile 和其他矢量数据存储
- **覆盖范围存储管理**：管理栅格数据源（GeoTIFF、ImageMosaic 等）
- **WMS/WMTS 存储管理**：管理级联 WMS 和 WMTS 远程数据源
- **图层管理**：发布、配置和管理图层及要素类型
- **样式管理**：上传、编辑和管理 SLD 样式
- **图层组**：创建和管理图层组

#### 服务配置
- **WMS 配置**：查看和编辑 Web Map Service 设置
- **WFS 配置**：查看和编辑 Web Feature Service 设置
- **WCS 配置**：查看和编辑 Web Coverage Service 设置

#### 系统管理
- **关于和状态**：查看 GeoServer 版本信息和系统状态
- **全局设置**：管理 GeoServer 全局配置
- **日志配置**：查看和配置服务器日志级别
- **切片缓存（GeoWebCache）**：
  - 缓存默认值：配置全局缓存策略
  - 格网集：管理切片格网定义
  - 磁盘配额：配置磁盘使用限制

#### 安全管理
- **安全设置**：配置安全策略
- **用户、组和角色管理**：管理访问控制

#### 其他
- **连接管理**：保存和管理多个 GeoServer 连接配置
- **国际化**：支持中英文界面一键切换

### 架构设计

解决方案由两个主要项目组成：

#### 1. GeoServerDesktop.GeoServerClient

一个 .NET Standard 2.0 类库，提供 **100% 覆盖** GeoServer REST API 的完整客户端（45 个服务类，195 个 REST 操作）：

- **HTTP 层**：处理身份验证、请求和错误处理
- **模型**：所有 GeoServer 资源的强类型模型（18 个模型文件）
- **核心资源服务**：工作空间、命名空间、数据存储、覆盖范围存储、WMS/WMTS 存储、要素类型、覆盖范围、图层、图层组、样式等
- **系统服务**：关于信息、全局设置、日志、重新加载/重置
- **服务配置**：WMS、WFS、WCS、WMTS 服务设置
- **安全服务**：安全 ACL、用户/组/角色、认证过滤器、密码管理
- **GeoWebCache 服务**：GWC 图层、磁盘配额、格网集、Blob 存储
- **扩展服务**：资源管理、字体、模板、导入器、监控、URL 检查、WPS、CSW
- **预览服务**：生成用于地图可视化的 WMS URL

#### 2. GeoServerDesktop.App

基于 Avalonia 的 .NET 8 桌面应用程序：

- **MVVM 架构**：基于 CommunityToolkit.Mvvm 的清晰关注点分离
- **侧边栏导航**：分层资源浏览器，按功能分组
- **详细视图**：每种资源类型的专属管理界面
- **地图集成**：Mapsui 驱动的地图预览
- **国际化**：LocalizationService 支持中英文动态切换
- **跨平台**：可在 Windows、macOS 和 Linux 上运行

### 技术栈

- **语言**：C# 12
- **框架**：.NET 8, .NET Standard 2.0
- **UI**：Avalonia UI 11.3.9
- **MVVM**：CommunityToolkit.Mvvm 8.4.0
- **地图**：Mapsui 5.0.2 + Mapsui.Avalonia
- **JSON**：Newtonsoft.Json 13.0.4

### 快速开始

#### 前置要求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 或更高版本
- 运行中的 GeoServer 实例（用于测试）

#### 构建解决方案

```bash
# 克隆仓库
git clone https://github.com/znlgis/GeoServerDesktop.git
cd GeoServerDesktop

# 使用构建脚本（推荐）
# Linux/macOS
cd src && bash build.sh

# Windows (PowerShell)
cd src && .\build.ps1
```

或手动执行：

```bash
cd src

# 恢复依赖项
dotnet restore

# 构建解决方案
dotnet build --configuration Release

# 运行应用程序
dotnet run --project GeoServerDesktop.App/GeoServerDesktop.App.csproj
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
│   ├── GeoServerDesktop.sln                    # 解决方案文件
│   ├── build.sh                                # Linux/macOS 构建脚本
│   ├── build.ps1                               # Windows 构建脚本
│   ├── GeoServerDesktop.GeoServerClient/       # REST API 客户端库
│   │   ├── Configuration/                      # 客户端配置和工厂类
│   │   ├── Http/                               # HTTP 客户端基础设施
│   │   ├── Models/                             # 数据模型（18 个模型文件）
│   │   ├── Services/                           # 服务层（45 个服务类）
│   │   └── README.md                           # 客户端库文档
│   └── GeoServerDesktop.App/                   # Avalonia 桌面应用程序
│       ├── Views/                              # XAML 视图
│       ├── ViewModels/                         # 视图模型
│       ├── Services/                           # 应用服务（连接、设置、本地化）
│       ├── Models/                             # UI 模型
│       └── Assets/                             # 资源文件
├── docs/                                       # 项目文档
├── .editorconfig                               # 代码风格配置
└── README.md                                   # 本文件
```

### 使用说明

#### 连接到 GeoServer

1. 启动应用程序
2. 在顶部工具栏输入 GeoServer 连接信息：
   - URL（例如：`http://localhost:8080/geoserver`）
   - 用户名（默认：`admin`）
   - 密码（默认：`geoserver`）
3. 点击"Login"登录
4. 连接成功后左侧导航栏将显示所有功能模块

#### 应用程序导航

连接到 GeoServer 后，左侧导航栏提供以下功能：

| 导航项 | 功能 |
|--------|------|
| **About & Status** → About GeoServer | 查看 GeoServer 版本和系统信息 |
| **Data** → Layer Preview | 地图预览（WMS 图层可视化） |
| **Data** → Workspaces | 工作空间管理 |
| **Data** → Stores | 数据存储管理 |
| **Data** → Layers | 图层管理 |
| **Data** → Layer Groups | 图层组管理 |
| **Data** → Styles | 样式管理 |
| **Services** → WMS / WFS / WCS | 服务配置 |
| **Settings** → Global | 全局设置 |
| **Settings** → Logging | 日志配置 |
| **Tile Caching** → Caching Defaults | 缓存默认设置 |
| **Tile Caching** → Gridsets | 格网集管理 |
| **Tile Caching** → Disk Quota | 磁盘配额 |
| **Security** → Settings | 安全设置 |
| **Security** → Users, Groups, Roles | 用户权限管理 |

#### 切换语言

点击右上角的语言切换按钮（"中文" / "English"）可在中英文界面之间切换。

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

// 使用服务 - 工作空间
var workspaceService = factory.CreateWorkspaceService();
var workspaces = await workspaceService.GetWorkspacesAsync();

foreach (var workspace in workspaces)
{
    Console.WriteLine($"工作空间：{workspace.Name}");
}

// 使用服务 - 图层
var layerService = factory.CreateLayerService();
var layers = await layerService.GetLayersAsync();

// 使用服务 - 样式
var styleService = factory.CreateStyleService();
var styles = await styleService.GetStylesAsync();

// 使用服务 - 安全（用户管理）
var userGroupService = factory.CreateUserGroupService();

// 清理资源
factory.Dispose();
```

完整的 API 覆盖情况请参阅 [客户端库文档](src/GeoServerDesktop.GeoServerClient/README.md)。

### 配置

#### 应用程序设置

连接信息和界面设置存储在本地 JSON 配置文件中：
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
6. 新增 UI 文本时同步更新 `LocalizationService` 的中英文翻译

### GeoServer 兼容性

此客户端基于 GeoServer 2.28.x REST API 规范构建，向后兼容主要 2.x 版本：
- GeoServer 2.20+
- GeoServer 2.24+
- GeoServer 2.28+（推荐）

### 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件。

### 路线图

- [x] 工作空间、数据存储、图层、样式、图层组管理
- [x] 覆盖范围存储支持
- [x] WMS/WFS/WCS 服务配置
- [x] 安全和身份验证管理（用户、组、角色）
- [x] 全局设置和日志配置
- [x] 切片缓存（GeoWebCache）管理
- [x] 中英文国际化支持
- [ ] 高级图层配置（SLD 在线编辑器）
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
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM 工具包

### 资源链接

- [GeoServer REST API 文档](https://docs.geoserver.org/latest/en/user/rest/)
- [Avalonia UI 文档](https://docs.avaloniaui.net/)
- [Mapsui 文档](https://mapsui.com/documentation/)
- [客户端库详细文档](src/GeoServerDesktop.GeoServerClient/README.md)

---

## English

A cross-platform desktop application for managing GeoServer instances through a user-friendly interface.

### Overview

GeoServerDesktop provides a modern, cross-platform desktop tool for interacting with GeoServer via its REST API. Built with Avalonia UI and .NET 8, it runs on Windows, macOS, and Linux. The application supports both English and Chinese UI languages.

### Features

#### Data Management
- **Layer Preview**: Built-in map preview using Mapsui for WMS layers
- **Workspace Management**: Create, view, update, and delete GeoServer workspaces
- **Data Store Management**: Manage PostGIS, Shapefile, and other vector data stores
- **Coverage Store Management**: Manage raster data sources (GeoTIFF, ImageMosaic, etc.)
- **WMS/WMTS Store Management**: Manage cascaded WMS and WMTS remote data sources
- **Layer Management**: Publish, configure, and manage layers and feature types
- **Style Management**: Upload, edit, and manage SLD styles
- **Layer Groups**: Create and manage layer groups

#### Service Configuration
- **WMS Configuration**: View and edit Web Map Service settings
- **WFS Configuration**: View and edit Web Feature Service settings
- **WCS Configuration**: View and edit Web Coverage Service settings

#### System Administration
- **About & Status**: View GeoServer version info and system status
- **Global Settings**: Manage GeoServer global configuration
- **Logging**: View and configure server log levels
- **Tile Caching (GeoWebCache)**:
  - Caching Defaults: Configure global caching policies
  - Gridsets: Manage tile grid definitions
  - Disk Quota: Configure disk usage limits

#### Security Management
- **Security Settings**: Configure security policies
- **Users, Groups, and Roles**: Manage access control

#### Other
- **Connection Management**: Save and manage GeoServer connection configurations
- **Internationalization**: One-click toggle between English and Chinese UI

### Architecture

The solution consists of two main projects:

#### 1. GeoServerDesktop.GeoServerClient

A .NET Standard 2.0 class library that provides **100% coverage** of the GeoServer REST API (45 service classes, 195 REST operations):

- **HTTP Layer**: Handles authentication, requests, and error handling
- **Models**: Strongly-typed models for all GeoServer resources (18 model files)
- **Core Resource Services**: Workspace, namespace, data store, coverage store, WMS/WMTS stores, feature types, coverages, layers, layer groups, styles
- **System Services**: About info, global settings, logging, reload/reset
- **Service Configuration**: WMS, WFS, WCS, WMTS service settings
- **Security Services**: Security ACL, users/groups/roles, authentication filters, password management
- **GeoWebCache Services**: GWC layers, disk quota, gridsets, blob stores
- **Extension Services**: Resources, fonts, templates, importer, monitoring, URL check, WPS, CSW
- **Preview Service**: Generates WMS URLs for map visualization

#### 2. GeoServerDesktop.App

An Avalonia-based .NET 8 desktop application:

- **MVVM Architecture**: Clean separation of concerns using CommunityToolkit.Mvvm
- **Sidebar Navigation**: Hierarchical resource browser grouped by function
- **Detail Views**: Dedicated management UI for each resource type
- **Map Integration**: Mapsui-powered map preview
- **Internationalization**: LocalizationService with dynamic English/Chinese switching
- **Cross-Platform**: Runs on Windows, macOS, and Linux

### Technology Stack

- **Language**: C# 12
- **Frameworks**: .NET 8, .NET Standard 2.0
- **UI**: Avalonia UI 11.3.9
- **MVVM**: CommunityToolkit.Mvvm 8.4.0
- **Maps**: Mapsui 5.0.2 + Mapsui.Avalonia
- **JSON**: Newtonsoft.Json 13.0.4

### Getting Started

#### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- A running GeoServer instance (for testing)

#### Building the Solution

```bash
# Clone the repository
git clone https://github.com/znlgis/GeoServerDesktop.git
cd GeoServerDesktop

# Using the build script (recommended)
# Linux/macOS
cd src && bash build.sh

# Windows (PowerShell)
cd src && .\build.ps1
```

Or build manually:

```bash
cd src

# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run the application
dotnet run --project GeoServerDesktop.App/GeoServerDesktop.App.csproj
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
│   ├── GeoServerDesktop.sln                    # Solution file
│   ├── build.sh                                # Linux/macOS build script
│   ├── build.ps1                               # Windows build script
│   ├── GeoServerDesktop.GeoServerClient/       # REST API client library
│   │   ├── Configuration/                      # Client configuration and factory
│   │   ├── Http/                               # HTTP client infrastructure
│   │   ├── Models/                             # Data models (18 model files)
│   │   ├── Services/                           # Service layer (45 service classes)
│   │   └── README.md                           # Client library documentation
│   └── GeoServerDesktop.App/                   # Avalonia desktop application
│       ├── Views/                              # XAML views
│       ├── ViewModels/                         # View models
│       ├── Services/                           # App services (connection, settings, i18n)
│       ├── Models/                             # UI models
│       └── Assets/                             # Resources
├── docs/                                       # Project documentation
├── .editorconfig                               # Code style configuration
└── README.md                                   # This file
```

### Usage

#### Connecting to GeoServer

1. Launch the application
2. Enter your GeoServer connection details in the top toolbar:
   - URL (e.g., `http://localhost:8080/geoserver`)
   - Username (default: `admin`)
   - Password (default: `geoserver`)
3. Click "Login"
4. Once connected, the left sidebar shows all available feature modules

#### Application Navigation

After connecting to GeoServer, the left sidebar provides the following features:

| Navigation | Function |
|------------|----------|
| **About & Status** → About GeoServer | View GeoServer version and system info |
| **Data** → Layer Preview | Map preview (WMS layer visualization) |
| **Data** → Workspaces | Workspace management |
| **Data** → Stores | Data store management |
| **Data** → Layers | Layer management |
| **Data** → Layer Groups | Layer group management |
| **Data** → Styles | Style management |
| **Services** → WMS / WFS / WCS | Service configuration |
| **Settings** → Global | Global settings |
| **Settings** → Logging | Logging configuration |
| **Tile Caching** → Caching Defaults | Cache default settings |
| **Tile Caching** → Gridsets | Gridset management |
| **Tile Caching** → Disk Quota | Disk quota settings |
| **Security** → Settings | Security settings |
| **Security** → Users, Groups, Roles | User and access management |

#### Switching Languages

Click the language toggle button in the top-right corner ("中文" / "English") to switch between Chinese and English UI.

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

// Use services - Workspaces
var workspaceService = factory.CreateWorkspaceService();
var workspaces = await workspaceService.GetWorkspacesAsync();

foreach (var workspace in workspaces)
{
    Console.WriteLine($"Workspace: {workspace.Name}");
}

// Use services - Layers
var layerService = factory.CreateLayerService();
var layers = await layerService.GetLayersAsync();

// Use services - Styles
var styleService = factory.CreateStyleService();
var styles = await styleService.GetStylesAsync();

// Use services - Security (user management)
var userGroupService = factory.CreateUserGroupService();

// Clean up
factory.Dispose();
```

For full API coverage details, see the [client library documentation](src/GeoServerDesktop.GeoServerClient/README.md).

### Configuration

#### Application Settings

Connection info and UI preferences are stored in a local JSON configuration file:
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
6. When adding new UI text, update both English and Chinese strings in `LocalizationService`

### GeoServer Compatibility

This client is built against the GeoServer 2.28.x REST API specification and is backward compatible with major 2.x releases:
- GeoServer 2.20+
- GeoServer 2.24+
- GeoServer 2.28+ (recommended)

### License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

### Roadmap

- [x] Workspace, data store, layer, style, and layer group management
- [x] Coverage store support
- [x] WMS/WFS/WCS service configuration
- [x] Security and authentication management (users, groups, roles)
- [x] Global settings and logging configuration
- [x] Tile caching (GeoWebCache) management
- [x] Chinese/English internationalization
- [ ] Advanced layer configuration (online SLD editor)
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
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM toolkit

### Resources

- [GeoServer REST API Documentation](https://docs.geoserver.org/latest/en/user/rest/)
- [Avalonia UI Documentation](https://docs.avaloniaui.net/)
- [Mapsui Documentation](https://mapsui.com/documentation/)
- [Client Library Documentation](src/GeoServerDesktop.GeoServerClient/README.md)
