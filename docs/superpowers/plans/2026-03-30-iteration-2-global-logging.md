# 迭代 2：Global Settings + Logging 配置页面实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 实现全局设置（Global Settings）和日志配置（Logging）两个管理页面，支持读取当前配置并编辑保存。

**架构：** `IGeoServerConnectionService` 已有 `GetGlobalSettingsService()` 和 `GetLoggingService()` 方法，直接使用。为每个页面创建 ViewModel + View，替换 `MainWindowViewModel` 中的 PlaceholderViewModel。

**技术栈：** C# 12, .NET 8, Avalonia UI, CommunityToolkit.Mvvm, GeoServerClient 服务层

---

## 文件清单

### 需先确认的 Model 结构
运行前需查看以下文件，了解 Settings 和 Logging 的模型结构：
- `src/GeoServerDesktop.GeoServerClient/Models/Settings.cs`
- `src/GeoServerDesktop.GeoServerClient/Services/SettingsService.cs`
- `src/GeoServerDesktop.GeoServerClient/Services/LoggingService.cs`
- `src/GeoServerDesktop.GeoServerClient/Models/Logging.cs`（如存在）

### 新建文件
- `src/GeoServerDesktop.App/ViewModels/GlobalSettingsViewModel.cs` — 全局设置视图模型
- `src/GeoServerDesktop.App/ViewModels/LoggingViewModel.cs` — 日志配置视图模型
- `src/GeoServerDesktop.App/Views/GlobalSettingsView.axaml` — 全局设置视图
- `src/GeoServerDesktop.App/Views/GlobalSettingsView.axaml.cs` — 全局设置 code-behind
- `src/GeoServerDesktop.App/Views/LoggingView.axaml` — 日志配置视图
- `src/GeoServerDesktop.App/Views/LoggingView.axaml.cs` — 日志配置 code-behind

### 修改文件
- `src/GeoServerDesktop.App/ViewModels/MainWindowViewModel.cs` — 替换 Global Settings 和 Logging 的 PlaceholderViewModel

---

## 任务 1：探查 Settings 和 Logging 模型

**文件：**
- 读取：`src/GeoServerDesktop.GeoServerClient/Models/Settings.cs`
- 读取：`src/GeoServerDesktop.GeoServerClient/Services/SettingsService.cs`
- 读取：`src/GeoServerDesktop.GeoServerClient/Services/LoggingService.cs`

- [ ] **步骤 1：阅读 Settings 模型，确认可用字段**

重点关注：
- `GeoServerSettings` 或类似的根类型
- 联系信息（Contact）字段
- 代理设置字段
- 在线资源 URL

- [ ] **步骤 2：阅读 Logging 模型，确认可用字段**

重点关注：
- 日志级别（level）字段
- 日志位置（location）字段

---

## 任务 2：实现 GlobalSettingsViewModel

**文件：**
- 创建：`src/GeoServerDesktop.App/ViewModels/GlobalSettingsViewModel.cs`

- [ ] **步骤 1：根据实际模型创建 GlobalSettingsViewModel**

基本结构（根据 Settings.cs 实际字段调整）：

```csharp
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// GeoServer 全局设置的视图模型
    /// </summary>
    public partial class GlobalSettingsViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>联系人姓名</summary>
        [ObservableProperty]
        private string _contactPerson = string.Empty;

        /// <summary>联系人组织</summary>
        [ObservableProperty]
        private string _contactOrganization = string.Empty;

        /// <summary>联系人邮箱</summary>
        [ObservableProperty]
        private string _contactEmail = string.Empty;

        /// <summary>在线资源 URL</summary>
        [ObservableProperty]
        private string _onlineResource = string.Empty;

        /// <summary>代理基础 URL</summary>
        [ObservableProperty]
        private string _proxyBaseUrl = string.Empty;

        /// <summary>是否启用详细异常信息</summary>
        [ObservableProperty]
        private bool _verboseExceptions;

        /// <summary>是否正在加载</summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>状态消息</summary>
        [ObservableProperty]
        private string _statusMessage = "Ready";

        /// <summary>初始化 GlobalSettingsViewModel 类的新实例</summary>
        public GlobalSettingsViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>加载全局设置</summary>
        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = "Not connected to GeoServer";
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading global settings...";

            try
            {
                var service = _connectionService.GetGlobalSettingsService();
                var settings = await service.GetSettingsAsync();

                // 根据实际模型字段填充属性
                // 注意：执行时需根据 Settings.cs 中的实际属性路径调整
                if (settings != null)
                {
                    // 示例字段映射（根据实际模型调整）
                    // ContactPerson = settings.Global?.Contact?.ContactPerson ?? string.Empty;
                    // ContactOrganization = settings.Global?.Contact?.ContactOrganization ?? string.Empty;
                    // ContactEmail = settings.Global?.Contact?.ContactEmail ?? string.Empty;
                    // OnlineResource = settings.Global?.OnlineResource ?? string.Empty;
                    // ProxyBaseUrl = settings.Global?.ProxyBaseUrl ?? string.Empty;
                    // VerboseExceptions = settings.Global?.VerboseExceptions ?? false;
                }

                StatusMessage = "Global settings loaded";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load settings: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>保存全局设置</summary>
        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = "Not connected to GeoServer";
                return;
            }

            IsLoading = true;
            StatusMessage = "Saving global settings...";

            try
            {
                var service = _connectionService.GetGlobalSettingsService();
                // 根据实际模型构建设置对象并调用 UpdateSettingsAsync
                // await service.UpdateSettingsAsync(settings);
                StatusMessage = "Global settings saved successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to save settings: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
```

**重要：** 执行本任务前，必须先阅读 `Settings.cs` 中的实际模型，将注释掉的示例字段映射替换为真实字段路径。

---

## 任务 3：实现 LoggingViewModel

**文件：**
- 创建：`src/GeoServerDesktop.App/ViewModels/LoggingViewModel.cs`

- [ ] **步骤 1：根据实际 Logging 模型创建 LoggingViewModel**

```csharp
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// GeoServer 日志配置的视图模型
    /// </summary>
    public partial class LoggingViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>当前日志级别</summary>
        [ObservableProperty]
        private string _logLevel = string.Empty;

        /// <summary>日志文件位置</summary>
        [ObservableProperty]
        private string _logLocation = string.Empty;

        /// <summary>是否启用标准输出日志</summary>
        [ObservableProperty]
        private bool _stdOutLogging;

        /// <summary>可用的日志级别选项</summary>
        public ObservableCollection<string> LogLevelOptions { get; } = new()
        {
            "VERBOSE", "DEBUG", "PRODUCTION", "WARNING", "ERROR"
        };

        /// <summary>是否正在加载</summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>状态消息</summary>
        [ObservableProperty]
        private string _statusMessage = "Ready";

        /// <summary>初始化 LoggingViewModel 类的新实例</summary>
        public LoggingViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>加载日志设置</summary>
        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = "Not connected to GeoServer";
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading logging settings...";

            try
            {
                var service = _connectionService.GetLoggingService();
                var settings = await service.GetLoggingAsync();

                // 根据实际 Logging 模型字段调整
                if (settings != null)
                {
                    // LogLevel = settings.Logging?.Level ?? string.Empty;
                    // LogLocation = settings.Logging?.Location ?? string.Empty;
                    // StdOutLogging = settings.Logging?.StdOutLogging ?? false;
                }

                StatusMessage = "Logging settings loaded";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load logging settings: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>保存日志设置</summary>
        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = "Not connected to GeoServer";
                return;
            }

            IsLoading = true;
            StatusMessage = "Saving logging settings...";

            try
            {
                var service = _connectionService.GetLoggingService();
                // await service.UpdateLoggingAsync(settings);
                StatusMessage = "Logging settings saved successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to save logging settings: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
```

---

## 任务 4：创建 GlobalSettingsView

**文件：**
- 创建：`src/GeoServerDesktop.App/Views/GlobalSettingsView.axaml`
- 创建：`src/GeoServerDesktop.App/Views/GlobalSettingsView.axaml.cs`

- [ ] **步骤 1：创建视图文件**

采用与 WMSSettingsView 相同的三行布局（Header / ScrollViewer Content / StatusBar）：
- Header：标题 "Global Settings" + Refresh 按钮
- Content：
  - "Contact Information" 区块：ContactPerson、ContactOrganization、ContactEmail
  - "Server Configuration" 区块：OnlineResource、ProxyBaseUrl、VerboseExceptions（CheckBox）
  - 保存按钮

---

## 任务 5：创建 LoggingView

**文件：**
- 创建：`src/GeoServerDesktop.App/Views/LoggingView.axaml`
- 创建：`src/GeoServerDesktop.App/Views/LoggingView.axaml.cs`

- [ ] **步骤 1：创建视图文件**

- Header：标题 "Logging Settings" + Refresh 按钮
- Content：
  - `ComboBox` 绑定 `LogLevel`，`ItemsSource="{Binding LogLevelOptions}"`
  - `TextBox` 绑定 `LogLocation`
  - `CheckBox` 绑定 `StdOutLogging`
  - 保存按钮

---

## 任务 6：集成到 MainWindowViewModel

**文件：**
- 修改：`src/GeoServerDesktop.App/ViewModels/MainWindowViewModel.cs`

- [ ] **步骤 1：添加 2 个字段，在构造函数中初始化**

```csharp
[ObservableProperty]
private GlobalSettingsViewModel _globalSettingsViewModel;

[ObservableProperty]
private LoggingViewModel _loggingViewModel;
```

构造函数：
```csharp
_globalSettingsViewModel = new GlobalSettingsViewModel(_connectionService);
_loggingViewModel = new LoggingViewModel(_connectionService);
```

- [ ] **步骤 2：替换 ShowGlobalSettings 和 ShowLogging 方法**

```csharp
[RelayCommand]
private void ShowGlobalSettings()
{
    if (!IsConnected) { StatusMessage = "Please connect to GeoServer first"; return; }
    CurrentView = GlobalSettingsViewModel;
    _ = GlobalSettingsViewModel.LoadSettingsCommand.ExecuteAsync(null);
    StatusMessage = "Global Settings";
}

[RelayCommand]
private void ShowLogging()
{
    if (!IsConnected) { StatusMessage = "Please connect to GeoServer first"; return; }
    CurrentView = LoggingViewModel;
    _ = LoggingViewModel.LoadSettingsCommand.ExecuteAsync(null);
    StatusMessage = "Logging Settings";
}
```

- [ ] **步骤 3：构建验证**

运行：`dotnet build src/GeoServerDesktop.sln`
预期：BUILD SUCCEEDED，0 错误

- [ ] **步骤 4：Commit**

```bash
git add src/GeoServerDesktop.App/ViewModels/GlobalSettingsViewModel.cs
git add src/GeoServerDesktop.App/ViewModels/LoggingViewModel.cs
git add src/GeoServerDesktop.App/Views/GlobalSettingsView.axaml
git add src/GeoServerDesktop.App/Views/GlobalSettingsView.axaml.cs
git add src/GeoServerDesktop.App/Views/LoggingView.axaml
git add src/GeoServerDesktop.App/Views/LoggingView.axaml.cs
git add src/GeoServerDesktop.App/ViewModels/MainWindowViewModel.cs
git commit -m "feat: 实现 Global Settings 和 Logging 配置页面（迭代2）"
```

---

## 验收标准

- [ ] 点击左侧 "Global"，显示全局设置表单（联系信息、代理等）
- [ ] 点击 "Logging"，显示日志级别选择和日志路径
- [ ] 两个页面均支持读取和保存操作
- [ ] `dotnet build` 0 错误 0 警告
