# 迭代 1：Services 配置页面实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 实现 WMS、WFS、WCS 三个服务配置管理页面，支持读取当前配置并编辑保存。

**架构：** 为每个服务类型创建独立的 ViewModel 和 View，通过 `IGeoServerConnectionService` 获取对应的 Service 实例。`MainWindowViewModel` 中替换现有的 PlaceholderViewModel 工厂方法。

**技术栈：** C# 12, .NET 8, Avalonia UI, CommunityToolkit.Mvvm, GeoServerClient 服务层

---

## 文件清单

### 新建文件
- `src/GeoServerDesktop.App/ViewModels/WMSSettingsViewModel.cs` — WMS 设置视图模型
- `src/GeoServerDesktop.App/ViewModels/WFSSettingsViewModel.cs` — WFS 设置视图模型
- `src/GeoServerDesktop.App/ViewModels/WCSSettingsViewModel.cs` — WCS 设置视图模型
- `src/GeoServerDesktop.App/Views/WMSSettingsView.axaml` — WMS 设置视图
- `src/GeoServerDesktop.App/Views/WMSSettingsView.axaml.cs` — WMS 视图 code-behind
- `src/GeoServerDesktop.App/Views/WFSSettingsView.axaml` — WFS 设置视图
- `src/GeoServerDesktop.App/Views/WFSSettingsView.axaml.cs` — WFS 视图 code-behind
- `src/GeoServerDesktop.App/Views/WCSSettingsView.axaml` — WCS 设置视图
- `src/GeoServerDesktop.App/Views/WCSSettingsView.axaml.cs` — WCS 视图 code-behind

### 修改文件
- `src/GeoServerDesktop.App/Services/IGeoServerConnectionService.cs` — 添加 WMS/WFS/WCS 服务获取方法
- `src/GeoServerDesktop.App/Services/GeoServerConnectionService.cs` — 实现 WMS/WFS/WCS 服务获取方法
- `src/GeoServerDesktop.App/ViewModels/MainWindowViewModel.cs` — 添加 3 个 ViewModel 字段，替换 PlaceholderViewModel 工厂方法

---

## 任务 1：扩展 IGeoServerConnectionService 接口

**文件：**
- 修改：`src/GeoServerDesktop.App/Services/IGeoServerConnectionService.cs`
- 修改：`src/GeoServerDesktop.App/Services/GeoServerConnectionService.cs`

- [ ] **步骤 1：在 IGeoServerConnectionService 接口中添加 3 个服务获取方法**

在 `IGeoServerConnectionService.cs` 中，在 `GetLoggingService()` 方法后追加：

```csharp
/// <summary>
/// 获取 WMS 服务设置服务
/// </summary>
/// <returns>WMSSettingsService 实例</returns>
WMSSettingsService GetWMSSettingsService();

/// <summary>
/// 获取 WFS 服务设置服务
/// </summary>
/// <returns>WFSSettingsService 实例</returns>
WFSSettingsService GetWFSSettingsService();

/// <summary>
/// 获取 WCS 服务设置服务
/// </summary>
/// <returns>WCSSettingsService 实例</returns>
WCSSettingsService GetWCSSettingsService();
```

- [ ] **步骤 2：在 GeoServerConnectionService 中实现这 3 个方法**

在 `GeoServerConnectionService.cs` 中，在 `GetLoggingService()` 方法后追加：

```csharp
/// <summary>
/// 获取 WMS 服务设置服务
/// </summary>
/// <returns>WMSSettingsService 实例</returns>
public WMSSettingsService GetWMSSettingsService()
{
    EnsureConnected();
    return _factory!.CreateWMSSettingsService();
}

/// <summary>
/// 获取 WFS 服务设置服务
/// </summary>
/// <returns>WFSSettingsService 实例</returns>
public WFSSettingsService GetWFSSettingsService()
{
    EnsureConnected();
    return _factory!.CreateWFSSettingsService();
}

/// <summary>
/// 获取 WCS 服务设置服务
/// </summary>
/// <returns>WCSSettingsService 实例</returns>
public WCSSettingsService GetWCSSettingsService()
{
    EnsureConnected();
    return _factory!.CreateWCSSettingsService();
}
```

- [ ] **步骤 3：构建验证**

运行：`dotnet build src/GeoServerDesktop.sln`
预期：BUILD SUCCEEDED，0 错误

- [ ] **步骤 4：Commit**

```bash
git add src/GeoServerDesktop.App/Services/IGeoServerConnectionService.cs
git add src/GeoServerDesktop.App/Services/GeoServerConnectionService.cs
git commit -m "feat: 添加 WMS/WFS/WCS 服务获取方法到连接服务接口"
```

---

## 任务 2：实现 WMSSettingsViewModel

**文件：**
- 创建：`src/GeoServerDesktop.App/ViewModels/WMSSettingsViewModel.cs`

- [ ] **步骤 1：创建 WMSSettingsViewModel.cs**

```csharp
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// WMS 服务设置的视图模型
    /// </summary>
    public partial class WMSSettingsViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>是否启用 WMS 服务</summary>
        [ObservableProperty]
        private bool _isEnabled;

        /// <summary>服务名称</summary>
        [ObservableProperty]
        private string _serviceName = string.Empty;

        /// <summary>服务标题</summary>
        [ObservableProperty]
        private string _title = string.Empty;

        /// <summary>服务摘要</summary>
        [ObservableProperty]
        private string _abstract = string.Empty;

        /// <summary>维护者</summary>
        [ObservableProperty]
        private string _maintainer = string.Empty;

        /// <summary>在线资源 URL</summary>
        [ObservableProperty]
        private string _onlineResource = string.Empty;

        /// <summary>是否符合引用规范</summary>
        [ObservableProperty]
        private bool _citeCompliant;

        /// <summary>是否正在加载</summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>状态消息</summary>
        [ObservableProperty]
        private string _statusMessage = "Ready";

        /// <summary>
        /// 初始化 WMSSettingsViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public WMSSettingsViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// 加载 WMS 设置
        /// </summary>
        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = "Not connected to GeoServer";
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading WMS settings...";

            try
            {
                var service = _connectionService.GetWMSSettingsService();
                var settings = await service.GetWMSSettingsAsync();

                if (settings?.WMS != null)
                {
                    IsEnabled = settings.WMS.Enabled ?? false;
                    ServiceName = settings.WMS.Name ?? string.Empty;
                    Title = settings.WMS.Title ?? string.Empty;
                    Abstract = settings.WMS.Abstract ?? string.Empty;
                    Maintainer = settings.WMS.Maintainer ?? string.Empty;
                    OnlineResource = settings.WMS.OnlineResource ?? string.Empty;
                    CiteCompliant = settings.WMS.CiteCompliant ?? false;
                }

                StatusMessage = "WMS settings loaded";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load WMS settings: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 保存 WMS 设置
        /// </summary>
        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = "Not connected to GeoServer";
                return;
            }

            IsLoading = true;
            StatusMessage = "Saving WMS settings...";

            try
            {
                var service = _connectionService.GetWMSSettingsService();
                var settings = new WMSSettings
                {
                    WMS = new WMSServiceConfig
                    {
                        Enabled = IsEnabled,
                        Name = ServiceName,
                        Title = Title,
                        Abstract = Abstract,
                        Maintainer = Maintainer,
                        OnlineResource = OnlineResource,
                        CiteCompliant = CiteCompliant
                    }
                };

                await service.UpdateWMSSettingsAsync(settings);
                StatusMessage = "WMS settings saved successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to save WMS settings: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
```

- [ ] **步骤 2：构建验证**

运行：`dotnet build src/GeoServerDesktop.sln`
预期：BUILD SUCCEEDED，0 错误

---

## 任务 3：实现 WFSSettingsViewModel 和 WCSSettingsViewModel

**文件：**
- 创建：`src/GeoServerDesktop.App/ViewModels/WFSSettingsViewModel.cs`
- 创建：`src/GeoServerDesktop.App/ViewModels/WCSSettingsViewModel.cs`

- [ ] **步骤 1：创建 WFSSettingsViewModel.cs**

结构与 WMSSettingsViewModel 完全一致，差异点：
- 类名：`WFSSettingsViewModel`
- 调用 `_connectionService.GetWFSSettingsService()`
- 使用 `WFSSettings` / `WFSServiceConfig` 模型
- 额外字段：`MaxFeatures`（`int`）、`ServiceLevel`（`string`）、`FeatureBounding`（`bool`）

```csharp
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// WFS 服务设置的视图模型
    /// </summary>
    public partial class WFSSettingsViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        [ObservableProperty] private bool _isEnabled;
        [ObservableProperty] private string _serviceName = string.Empty;
        [ObservableProperty] private string _title = string.Empty;
        [ObservableProperty] private string _abstract = string.Empty;
        [ObservableProperty] private string _maintainer = string.Empty;
        [ObservableProperty] private string _onlineResource = string.Empty;
        [ObservableProperty] private bool _citeCompliant;
        [ObservableProperty] private int _maxFeatures;
        [ObservableProperty] private string _serviceLevel = string.Empty;
        [ObservableProperty] private bool _featureBounding;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _statusMessage = "Ready";

        /// <summary>初始化 WFSSettingsViewModel 类的新实例</summary>
        public WFSSettingsViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            if (!_connectionService.IsConnected) { StatusMessage = "Not connected to GeoServer"; return; }
            IsLoading = true;
            StatusMessage = "Loading WFS settings...";
            try
            {
                var service = _connectionService.GetWFSSettingsService();
                var settings = await service.GetWFSSettingsAsync();
                if (settings?.WFS != null)
                {
                    IsEnabled = settings.WFS.Enabled ?? false;
                    ServiceName = settings.WFS.Name ?? string.Empty;
                    Title = settings.WFS.Title ?? string.Empty;
                    Abstract = settings.WFS.Abstract ?? string.Empty;
                    Maintainer = settings.WFS.Maintainer ?? string.Empty;
                    OnlineResource = settings.WFS.OnlineResource ?? string.Empty;
                    CiteCompliant = settings.WFS.CiteCompliant ?? false;
                    MaxFeatures = settings.WFS.MaxFeatures ?? 0;
                    ServiceLevel = settings.WFS.ServiceLevel ?? string.Empty;
                    FeatureBounding = settings.WFS.FeatureBounding ?? false;
                }
                StatusMessage = "WFS settings loaded";
            }
            catch (Exception ex) { StatusMessage = $"Failed to load WFS settings: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            if (!_connectionService.IsConnected) { StatusMessage = "Not connected to GeoServer"; return; }
            IsLoading = true;
            StatusMessage = "Saving WFS settings...";
            try
            {
                var service = _connectionService.GetWFSSettingsService();
                var settings = new WFSSettings
                {
                    WFS = new WFSServiceConfig
                    {
                        Enabled = IsEnabled,
                        Name = ServiceName,
                        Title = Title,
                        Abstract = Abstract,
                        Maintainer = Maintainer,
                        OnlineResource = OnlineResource,
                        CiteCompliant = CiteCompliant,
                        MaxFeatures = MaxFeatures,
                        ServiceLevel = ServiceLevel,
                        FeatureBounding = FeatureBounding
                    }
                };
                await service.UpdateWFSSettingsAsync(settings);
                StatusMessage = "WFS settings saved successfully";
            }
            catch (Exception ex) { StatusMessage = $"Failed to save WFS settings: {ex.Message}"; }
            finally { IsLoading = false; }
        }
    }
}
```

- [ ] **步骤 2：创建 WCSSettingsViewModel.cs**

结构与上面一致，差异点：
- 类名：`WCSSettingsViewModel`
- 调用 `_connectionService.GetWCSSettingsService()`
- 使用 `WCSSettings` / `WCSServiceConfig` 模型
- 额外字段：`MaxInputMemory`（`int`）、`MaxOutputMemory`（`int`）、`SubsamplingEnabled`（`bool`）

```csharp
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// WCS 服务设置的视图模型
    /// </summary>
    public partial class WCSSettingsViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        [ObservableProperty] private bool _isEnabled;
        [ObservableProperty] private string _serviceName = string.Empty;
        [ObservableProperty] private string _title = string.Empty;
        [ObservableProperty] private string _abstract = string.Empty;
        [ObservableProperty] private string _maintainer = string.Empty;
        [ObservableProperty] private string _onlineResource = string.Empty;
        [ObservableProperty] private bool _citeCompliant;
        [ObservableProperty] private int _maxInputMemory;
        [ObservableProperty] private int _maxOutputMemory;
        [ObservableProperty] private bool _subsamplingEnabled;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _statusMessage = "Ready";

        /// <summary>初始化 WCSSettingsViewModel 类的新实例</summary>
        public WCSSettingsViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            if (!_connectionService.IsConnected) { StatusMessage = "Not connected to GeoServer"; return; }
            IsLoading = true;
            StatusMessage = "Loading WCS settings...";
            try
            {
                var service = _connectionService.GetWCSSettingsService();
                var settings = await service.GetWCSSettingsAsync();
                if (settings?.WCS != null)
                {
                    IsEnabled = settings.WCS.Enabled ?? false;
                    ServiceName = settings.WCS.Name ?? string.Empty;
                    Title = settings.WCS.Title ?? string.Empty;
                    Abstract = settings.WCS.Abstract ?? string.Empty;
                    Maintainer = settings.WCS.Maintainer ?? string.Empty;
                    OnlineResource = settings.WCS.OnlineResource ?? string.Empty;
                    CiteCompliant = settings.WCS.CiteCompliant ?? false;
                    MaxInputMemory = settings.WCS.MaxInputMemory ?? 0;
                    MaxOutputMemory = settings.WCS.MaxOutputMemory ?? 0;
                    SubsamplingEnabled = settings.WCS.SubsamplingEnabled ?? false;
                }
                StatusMessage = "WCS settings loaded";
            }
            catch (Exception ex) { StatusMessage = $"Failed to load WCS settings: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            if (!_connectionService.IsConnected) { StatusMessage = "Not connected to GeoServer"; return; }
            IsLoading = true;
            StatusMessage = "Saving WCS settings...";
            try
            {
                var service = _connectionService.GetWCSSettingsService();
                var settings = new WCSSettings
                {
                    WCS = new WCSServiceConfig
                    {
                        Enabled = IsEnabled,
                        Name = ServiceName,
                        Title = Title,
                        Abstract = Abstract,
                        Maintainer = Maintainer,
                        OnlineResource = OnlineResource,
                        CiteCompliant = CiteCompliant,
                        MaxInputMemory = MaxInputMemory,
                        MaxOutputMemory = MaxOutputMemory,
                        SubsamplingEnabled = SubsamplingEnabled
                    }
                };
                await service.UpdateWCSSettingsAsync(settings);
                StatusMessage = "WCS settings saved successfully";
            }
            catch (Exception ex) { StatusMessage = $"Failed to save WCS settings: {ex.Message}"; }
            finally { IsLoading = false; }
        }
    }
}
```

- [ ] **步骤 3：构建验证**

运行：`dotnet build src/GeoServerDesktop.sln`
预期：BUILD SUCCEEDED，0 错误

---

## 任务 4：创建 WMSSettingsView

**文件：**
- 创建：`src/GeoServerDesktop.App/Views/WMSSettingsView.axaml`
- 创建：`src/GeoServerDesktop.App/Views/WMSSettingsView.axaml.cs`

- [ ] **步骤 1：创建 WMSSettingsView.axaml**

参考 `WorkspaceManagementView.axaml` 的结构，采用三行布局（Header / Content / StatusBar）：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:vm="using:GeoServerDesktop.App.ViewModels"
             mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="600"
             x:Class="GeoServerDesktop.App.Views.WMSSettingsView"
             x:DataType="vm:WMSSettingsViewModel">

    <Design.DataContext>
        <vm:WMSSettingsViewModel/>
    </Design.DataContext>

    <Grid RowDefinitions="Auto,*,Auto">
        <!-- Header -->
        <Border Grid.Row="0" Background="#F5F5F5" Padding="10" BorderBrush="#CCCCCC" BorderThickness="0,0,0,1">
            <StackPanel Orientation="Horizontal" Spacing="10">
                <TextBlock Text="WMS Settings" FontSize="18" FontWeight="Bold" VerticalAlignment="Center"/>
                <Button Content="🔄 Load" Command="{Binding LoadSettingsCommand}" ToolTip.Tip="Load current WMS settings"/>
            </StackPanel>
        </Border>

        <!-- Content -->
        <ScrollViewer Grid.Row="1" Padding="20">
            <StackPanel Spacing="15" MaxWidth="600">
                <!-- Service Enable -->
                <Border Background="#F5F5F5" Padding="15" BorderBrush="#CCCCCC" BorderThickness="1" CornerRadius="4">
                    <StackPanel Spacing="10">
                        <TextBlock Text="Service Configuration" FontWeight="Bold" FontSize="14"/>
                        <StackPanel Orientation="Horizontal" Spacing="10">
                            <CheckBox Content="Enable WMS Service" IsChecked="{Binding IsEnabled}"/>
                        </StackPanel>
                        <StackPanel Orientation="Horizontal" Spacing="10">
                            <CheckBox Content="CITE Compliant" IsChecked="{Binding CiteCompliant}"/>
                        </StackPanel>
                    </StackPanel>
                </Border>

                <!-- Service Info -->
                <Border Background="#F5F5F5" Padding="15" BorderBrush="#CCCCCC" BorderThickness="1" CornerRadius="4">
                    <StackPanel Spacing="10">
                        <TextBlock Text="Service Information" FontWeight="Bold" FontSize="14"/>
                        <Grid ColumnDefinitions="120,*" RowDefinitions="Auto,Auto,Auto,Auto,Auto" HorizontalAlignment="Stretch">
                            <TextBlock Grid.Row="0" Grid.Column="0" Text="Name:" VerticalAlignment="Center"/>
                            <TextBox Grid.Row="0" Grid.Column="1" Text="{Binding ServiceName}" Watermark="Service name"/>

                            <TextBlock Grid.Row="1" Grid.Column="0" Text="Title:" VerticalAlignment="Center" Margin="0,8,0,0"/>
                            <TextBox Grid.Row="1" Grid.Column="1" Text="{Binding Title}" Watermark="Service title" Margin="0,8,0,0"/>

                            <TextBlock Grid.Row="2" Grid.Column="0" Text="Abstract:" VerticalAlignment="Top" Margin="0,8,0,0"/>
                            <TextBox Grid.Row="2" Grid.Column="1" Text="{Binding Abstract}" Watermark="Service abstract"
                                     AcceptsReturn="True" Height="80" TextWrapping="Wrap" Margin="0,8,0,0"/>

                            <TextBlock Grid.Row="3" Grid.Column="0" Text="Maintainer:" VerticalAlignment="Center" Margin="0,8,0,0"/>
                            <TextBox Grid.Row="3" Grid.Column="1" Text="{Binding Maintainer}" Watermark="Maintainer" Margin="0,8,0,0"/>

                            <TextBlock Grid.Row="4" Grid.Column="0" Text="Online Resource:" VerticalAlignment="Center" Margin="0,8,0,0"/>
                            <TextBox Grid.Row="4" Grid.Column="1" Text="{Binding OnlineResource}" Watermark="Online resource URL" Margin="0,8,0,0"/>
                        </Grid>
                    </StackPanel>
                </Border>

                <!-- Save Button -->
                <Button Content="💾 Save Settings"
                        Command="{Binding SaveSettingsCommand}"
                        Background="#4CAF50"
                        Foreground="White"
                        HorizontalAlignment="Left"
                        Padding="15,8"/>

                <!-- Loading Indicator -->
                <StackPanel Spacing="10" IsVisible="{Binding IsLoading}">
                    <TextBlock Text="Processing..." HorizontalAlignment="Center"/>
                    <ProgressBar IsIndeterminate="True"/>
                </StackPanel>
            </StackPanel>
        </ScrollViewer>

        <!-- Status Bar -->
        <Border Grid.Row="2" Background="#F5F5F5" Padding="10" BorderBrush="#CCCCCC" BorderThickness="0,1,0,0">
            <TextBlock Text="{Binding StatusMessage}" VerticalAlignment="Center"/>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **步骤 2：创建 WMSSettingsView.axaml.cs**

```csharp
using Avalonia.Controls;

namespace GeoServerDesktop.App.Views
{
    /// <summary>
    /// WMS 服务设置视图
    /// </summary>
    public partial class WMSSettingsView : UserControl
    {
        /// <summary>
        /// 初始化 WMSSettingsView 类的新实例
        /// </summary>
        public WMSSettingsView()
        {
            InitializeComponent();
        }
    }
}
```

---

## 任务 5：创建 WFSSettingsView 和 WCSSettingsView

**文件：**
- 创建：`src/GeoServerDesktop.App/Views/WFSSettingsView.axaml`
- 创建：`src/GeoServerDesktop.App/Views/WFSSettingsView.axaml.cs`
- 创建：`src/GeoServerDesktop.App/Views/WCSSettingsView.axaml`
- 创建：`src/GeoServerDesktop.App/Views/WCSSettingsView.axaml.cs`

- [ ] **步骤 1：创建 WFSSettingsView.axaml**

结构与 WMSSettingsView 一致，差异：
- `x:DataType="vm:WFSSettingsViewModel"`
- 标题改为 "WFS Settings"
- 在 Service Information 区块下方添加 "WFS Parameters" 区块，包含：
  - `MaxFeatures`（`NumericUpDown` 绑定）
  - `ServiceLevel`（`TextBox` 绑定）
  - `FeatureBounding`（`CheckBox` 绑定）

参考 WMSSettingsView.axaml 结构创建，`ServiceLevel` 使用 `TextBox`，`MaxFeatures` 使用 `NumericUpDown`：

```xml
<!-- WFS Parameters -->
<Border Background="#F5F5F5" Padding="15" BorderBrush="#CCCCCC" BorderThickness="1" CornerRadius="4">
    <StackPanel Spacing="10">
        <TextBlock Text="WFS Parameters" FontWeight="Bold" FontSize="14"/>
        <Grid ColumnDefinitions="140,*" RowDefinitions="Auto,Auto,Auto" HorizontalAlignment="Stretch">
            <TextBlock Grid.Row="0" Grid.Column="0" Text="Max Features:" VerticalAlignment="Center"/>
            <NumericUpDown Grid.Row="0" Grid.Column="1" Value="{Binding MaxFeatures}" Minimum="0"/>

            <TextBlock Grid.Row="1" Grid.Column="0" Text="Service Level:" VerticalAlignment="Center" Margin="0,8,0,0"/>
            <TextBox Grid.Row="1" Grid.Column="1" Text="{Binding ServiceLevel}" Watermark="e.g. COMPLETE" Margin="0,8,0,0"/>

            <TextBlock Grid.Row="2" Grid.Column="0" Text="" Margin="0,8,0,0"/>
            <CheckBox Grid.Row="2" Grid.Column="1" Content="Feature Bounding" IsChecked="{Binding FeatureBounding}" Margin="0,8,0,0"/>
        </Grid>
    </StackPanel>
</Border>
```

- [ ] **步骤 2：创建 WCSSettingsView.axaml**

结构与 WMSSettingsView 一致，差异：
- `x:DataType="vm:WCSSettingsViewModel"`
- 标题改为 "WCS Settings"
- 添加 "WCS Parameters" 区块，包含：
  - `MaxInputMemory`（`NumericUpDown`）
  - `MaxOutputMemory`（`NumericUpDown`）
  - `SubsamplingEnabled`（`CheckBox`）

- [ ] **步骤 3：创建两个 code-behind 文件**（格式同 WMSSettingsView.axaml.cs，仅类名不同）

---

## 任务 6：集成到 MainWindowViewModel

**文件：**
- 修改：`src/GeoServerDesktop.App/ViewModels/MainWindowViewModel.cs`

- [ ] **步骤 1：添加 3 个 ViewModel 字段**

在现有 `_aboutViewModel` 字段后添加：

```csharp
/// <summary>
/// WMS 服务设置视图模型
/// </summary>
[ObservableProperty]
private WMSSettingsViewModel _wmsSettingsViewModel;

/// <summary>
/// WFS 服务设置视图模型
/// </summary>
[ObservableProperty]
private WFSSettingsViewModel _wfsSettingsViewModel;

/// <summary>
/// WCS 服务设置视图模型
/// </summary>
[ObservableProperty]
private WCSSettingsViewModel _wcsSettingsViewModel;
```

- [ ] **步骤 2：在构造函数中初始化**

在 `_aboutViewModel = new AboutViewModel(_connectionService);` 后追加：

```csharp
_wmsSettingsViewModel = new WMSSettingsViewModel(_connectionService);
_wfsSettingsViewModel = new WFSSettingsViewModel(_connectionService);
_wcsSettingsViewModel = new WCSSettingsViewModel(_connectionService);
```

- [ ] **步骤 3：替换 3 个工厂方法为真实 ViewModel**

将 `ShowWMSSettings`、`ShowWFSSettings`、`ShowWCSSettings` 方法中的 `PlaceholderViewModel` 替换为实际 ViewModel，并在显示时触发加载：

```csharp
[RelayCommand]
private async Task ShowWMSSettingsAsync()
{
    if (!IsConnected)
    {
        StatusMessage = "Please connect to GeoServer first";
        return;
    }
    CurrentView = WmsSettingsViewModel;
    try { await WmsSettingsViewModel.LoadSettingsCommand.ExecuteAsync(null); }
    catch { }
    StatusMessage = "WMS Settings";
}
```

对 WFS 和 WCS 做相同处理。

**注意**：需将方法签名改为 `async Task` 并重命名方法（`ShowWMSSettings` → `ShowWMSSettingsAsync`），同时更新 XAML 绑定中的命令名称：`ShowWMSSettingsCommand` → `ShowWMSSettingsAsyncCommand`（CommunityToolkit.Mvvm 会自动生成 `ShowWMSSettingsAsyncCommand`）。

**或者**：保留同步方法名，在其中使用 `_ = LoadSettingsAsync();` 异步触发，不等待结果。推荐使用此方式保持 XAML 绑定不变：

```csharp
[RelayCommand]
private void ShowWMSSettings()
{
    if (!IsConnected) { StatusMessage = "Please connect to GeoServer first"; return; }
    CurrentView = WmsSettingsViewModel;
    _ = WmsSettingsViewModel.LoadSettingsCommand.ExecuteAsync(null);
    StatusMessage = "WMS Settings";
}
```

- [ ] **步骤 4：更新工厂方法**

删除 `CreateWMSSettingsViewModel`、`CreateWFSSettingsViewModel`、`CreateWCSSettingsViewModel` 方法（已不再需要）。

- [ ] **步骤 5：构建验证**

运行：`dotnet build src/GeoServerDesktop.sln`
预期：BUILD SUCCEEDED，0 错误

- [ ] **步骤 6：Commit**

```bash
git add src/GeoServerDesktop.App/ViewModels/
git add src/GeoServerDesktop.App/Views/WMSSettingsView.axaml
git add src/GeoServerDesktop.App/Views/WMSSettingsView.axaml.cs
git add src/GeoServerDesktop.App/Views/WFSSettingsView.axaml
git add src/GeoServerDesktop.App/Views/WFSSettingsView.axaml.cs
git add src/GeoServerDesktop.App/Views/WCSSettingsView.axaml
git add src/GeoServerDesktop.App/Views/WCSSettingsView.axaml.cs
git add src/GeoServerDesktop.App/Services/
git commit -m "feat: 实现 WMS/WFS/WCS 服务配置管理页面（迭代1）"
```

---

## 验收标准

- [ ] 点击左侧菜单 "WMS"，右侧显示 WMS 设置表单（不再是 placeholder）
- [ ] 点击 "Load" 按钮，表单填充当前 GeoServer 的 WMS 配置
- [ ] 修改配置后点击 "Save Settings"，能成功保存到 GeoServer
- [ ] WFS 和 WCS 页面具备相同功能
- [ ] 未连接时点击菜单项，状态栏提示 "Please connect to GeoServer first"
- [ ] `dotnet build` 0 错误 0 警告
