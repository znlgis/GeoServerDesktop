# 迭代 3：Tile Caching 管理页面实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 实现 Tile Caching Defaults（缓存默认配置）、Gridsets（格网集管理，CRUD）、Disk Quota（磁盘配额）三个管理页面。

**架构：** 需要扩展 `IGeoServerConnectionService` 接口，添加 GeoWebCache 相关服务获取方法（DiskQuotaService、GridsetService）。每个页面独立 ViewModel + View。

**技术栈：** C# 12, .NET 8, Avalonia UI, CommunityToolkit.Mvvm, GeoServerClient 服务层

---

## 文件清单

### 需先确认的 Model/Service 结构
运行前需查看以下文件，了解 GeoWebCache 相关模型：
- `src/GeoServerDesktop.GeoServerClient/Models/GeoWebCache.cs`
- `src/GeoServerDesktop.GeoServerClient/Services/DiskQuotaService.cs`
- `src/GeoServerDesktop.GeoServerClient/Services/GridsetService.cs`
- `src/GeoServerDesktop.GeoServerClient/Services/GWCLayerService.cs`
- `src/GeoServerDesktop.GeoServerClient/Configuration/GeoServerClientFactory.cs`（确认工厂方法名称）

### 新建文件
- `src/GeoServerDesktop.App/ViewModels/CachingDefaultsViewModel.cs`
- `src/GeoServerDesktop.App/ViewModels/GridsetsViewModel.cs`
- `src/GeoServerDesktop.App/ViewModels/DiskQuotaViewModel.cs`
- `src/GeoServerDesktop.App/Views/CachingDefaultsView.axaml`
- `src/GeoServerDesktop.App/Views/CachingDefaultsView.axaml.cs`
- `src/GeoServerDesktop.App/Views/GridsetsView.axaml`
- `src/GeoServerDesktop.App/Views/GridsetsView.axaml.cs`
- `src/GeoServerDesktop.App/Views/DiskQuotaView.axaml`
- `src/GeoServerDesktop.App/Views/DiskQuotaView.axaml.cs`

### 修改文件
- `src/GeoServerDesktop.App/Services/IGeoServerConnectionService.cs` — 添加 DiskQuotaService、GridsetService 获取方法
- `src/GeoServerDesktop.App/Services/GeoServerConnectionService.cs` — 实现上述方法
- `src/GeoServerDesktop.App/ViewModels/MainWindowViewModel.cs` — 替换 3 个 PlaceholderViewModel

---

## 任务 1：探查 GeoWebCache 模型和服务

- [ ] **步骤 1：阅读相关模型文件**

重点查看 `GeoWebCache.cs`，确认：
- 磁盘配额（DiskQuota）的字段：`enabled`、`diskBlockSize`、`cacheCleanUpFrequency`、`globalQuota`
- Gridset 的字段：`name`、`srs`、`extent`、`alignTopLeft`、`tileWidth`、`tileHeight`、`resolutions`

- [ ] **步骤 2：阅读 GridsetService.cs，确认方法签名**

关注：`GetGridsetsAsync()`、`GetGridsetAsync(name)`、`CreateGridsetAsync()`、`UpdateGridsetAsync()`、`DeleteGridsetAsync()`

---

## 任务 2：扩展 IGeoServerConnectionService

**文件：**
- 修改：`src/GeoServerDesktop.App/Services/IGeoServerConnectionService.cs`
- 修改：`src/GeoServerDesktop.App/Services/GeoServerConnectionService.cs`

- [ ] **步骤 1：添加接口方法**

```csharp
/// <summary>获取磁盘配额服务</summary>
DiskQuotaService GetDiskQuotaService();

/// <summary>获取格网集服务</summary>
GridsetService GetGridsetService();

/// <summary>获取 GWC 图层服务</summary>
GWCLayerService GetGWCLayerService();
```

- [ ] **步骤 2：实现服务方法（在 GeoServerConnectionService.cs 中）**

```csharp
public DiskQuotaService GetDiskQuotaService() { EnsureConnected(); return _factory!.CreateDiskQuotaService(); }
public GridsetService GetGridsetService() { EnsureConnected(); return _factory!.CreateGridsetService(); }
public GWCLayerService GetGWCLayerService() { EnsureConnected(); return _factory!.CreateGWCLayerService(); }
```

---

## 任务 3：实现 DiskQuotaViewModel

**文件：**
- 创建：`src/GeoServerDesktop.App/ViewModels/DiskQuotaViewModel.cs`

- [ ] **步骤 1：根据实际模型创建 ViewModel**

字段（根据 GeoWebCache.cs 实际结构调整）：
- `IsEnabled`（bool）—— 磁盘配额是否启用
- `DiskBlockSize`（int）—— 磁盘块大小
- `CacheCleanUpFrequency`（int）—— 清理频率
- `GlobalQuotaValue`（double）—— 配额大小数值
- `GlobalQuotaUnit`（string）—— 配额单位（GiB/MiB 等）

包含 `LoadSettingsAsync` 和 `SaveSettingsAsync` 命令，模式与迭代1相同。

---

## 任务 4：实现 GridsetsViewModel（完整 CRUD）

**文件：**
- 创建：`src/GeoServerDesktop.App/ViewModels/GridsetsViewModel.cs`

- [ ] **步骤 1：创建 ViewModel 支持列表 + 增删查**

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
    /// GeoWebCache 格网集管理的视图模型
    /// </summary>
    public partial class GridsetsViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>格网集名称列表</summary>
        [ObservableProperty]
        private ObservableCollection<string> _gridsets = new();

        /// <summary>选中的格网集名称</summary>
        [ObservableProperty]
        private string? _selectedGridset;

        /// <summary>新建格网集名称输入</summary>
        [ObservableProperty]
        private string _newGridsetName = string.Empty;

        /// <summary>是否正在加载</summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>状态消息</summary>
        [ObservableProperty]
        private string _statusMessage = "Ready";

        public GridsetsViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>加载格网集列表</summary>
        [RelayCommand]
        private async Task LoadGridsetsAsync()
        {
            if (!_connectionService.IsConnected) { StatusMessage = "Not connected to GeoServer"; return; }
            IsLoading = true;
            StatusMessage = "Loading gridsets...";
            try
            {
                var service = _connectionService.GetGridsetService();
                var list = await service.GetGridsetsAsync();
                Gridsets.Clear();
                foreach (var item in list)
                    Gridsets.Add(item.Name ?? item.ToString());
                StatusMessage = $"Loaded {Gridsets.Count} gridsets";
            }
            catch (Exception ex) { StatusMessage = $"Failed to load gridsets: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        /// <summary>删除选中的格网集</summary>
        [RelayCommand]
        private async Task DeleteGridsetAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedGridset)) { StatusMessage = "No gridset selected"; return; }
            IsLoading = true;
            StatusMessage = $"Deleting gridset '{SelectedGridset}'...";
            try
            {
                var service = _connectionService.GetGridsetService();
                await service.DeleteGridsetAsync(SelectedGridset);
                StatusMessage = $"Gridset '{SelectedGridset}' deleted";
                SelectedGridset = null;
                await LoadGridsetsAsync();
            }
            catch (Exception ex) { StatusMessage = $"Failed to delete gridset: {ex.Message}"; }
            finally { IsLoading = false; }
        }
    }
}
```

---

## 任务 5：实现 CachingDefaultsViewModel

**文件：**
- 创建：`src/GeoServerDesktop.App/ViewModels/CachingDefaultsViewModel.cs`

- [ ] **步骤 1：创建 ViewModel**

主要用于展示 GWC 的默认缓存设置，包含：
- 是否启用缓存（IsEnabled）
- 默认过期时间（DefaultExpireCache）
- 默认内存过期时间（DefaultExpireClients）
- 保存按钮

---

## 任务 6：创建三个 View 文件

- [ ] **步骤 1：创建 DiskQuotaView.axaml**

布局：Header + 表单区（IsEnabled CheckBox、数值输入、单位选择）+ 保存按钮 + StatusBar

- [ ] **步骤 2：创建 GridsetsView.axaml**

布局：Header + 列表（ListBox）+ 右侧操作面板（删除按钮）+ StatusBar  
参考 WorkspaceManagementView.axaml 的 "列表+操作面板" 三列布局。

- [ ] **步骤 3：创建 CachingDefaultsView.axaml**

布局：Header + 表单 + 保存按钮 + StatusBar

- [ ] **步骤 4：创建三个对应的 code-behind 文件**

---

## 任务 7：集成到 MainWindowViewModel

- [ ] **步骤 1：添加字段并在构造函数中初始化**

```csharp
[ObservableProperty] private CachingDefaultsViewModel _cachingDefaultsViewModel;
[ObservableProperty] private GridsetsViewModel _gridsetsViewModel;
[ObservableProperty] private DiskQuotaViewModel _diskQuotaViewModel;
```

- [ ] **步骤 2：替换 ShowCachingDefaults、ShowGridsets、ShowDiskQuota 方法**

模式与迭代1相同，触发加载命令。

- [ ] **步骤 3：构建验证并 Commit**

```bash
git commit -m "feat: 实现 Tile Caching 管理页面（迭代3）"
```

---

## 验收标准

- [ ] Tile Caching 下三个菜单项均显示实际内容
- [ ] Disk Quota 支持读取和保存配额配置
- [ ] Gridsets 显示现有格网集列表，支持删除操作
- [ ] Caching Defaults 支持读取和保存默认缓存配置
- [ ] `dotnet build` 0 错误 0 警告
