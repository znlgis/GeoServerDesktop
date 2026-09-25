using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Models;
using GeoServerDesktop.App.Services;
using Microsoft.Extensions.DependencyInjection;
using GeoServerDesktop.GeoServerClient.Configuration;

namespace GeoServerDesktop.App.ViewModels;

/// <summary>
/// 主窗口的视图模型
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IGeoServerConnectionService _connectionService;

    /// <summary>应用服务容器（子 VM 惰性解析入口）</summary>
    private readonly System.IServiceProvider? _services;

    /// <summary>
    /// GeoServer 服务器的基础 URL
    /// </summary>
    [ObservableProperty]
    private string _baseUrl = "http://localhost:8080/geoserver";

    /// <summary>
    /// 用户名
    /// </summary>
    [ObservableProperty]
    private string _username = "admin";

    /// <summary>
    /// 密码
    /// </summary>
    [ObservableProperty]
    private string _password = "geoserver";

    /// <summary>
    /// 是否已连接到 GeoServer
    /// </summary>
    [ObservableProperty]
    private bool _isConnected;

    /// <summary>
    /// 状态消息
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Not connected";

    /// <summary>
    /// 切换中英文语言的命令
    /// </summary>
    [RelayCommand]
    private void ToggleLanguage()
    {
        L.ToggleLanguage();
        StatusMessage = L.StatusNotConnected;
    }

    /// <summary>
    /// 资源树集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ResourceTreeNode> _resourceTree = new();

    /// <summary>
    /// 当前选中的节点
    /// </summary>
    [ObservableProperty]
    private ResourceTreeNode? _selectedNode;


    /// <summary>
    /// 欢迎页仪表盘视图模型
    /// </summary>
    private DashboardViewModel? _dashboardViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public DashboardViewModel DashboardViewModel => _dashboardViewModel ??= Resolve<DashboardViewModel>();


    /// <summary>
    /// 地图预览视图模型
    /// </summary>
    private MapPreviewViewModel? _mapPreviewViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public MapPreviewViewModel MapPreviewViewModel => _mapPreviewViewModel ??= Resolve<MapPreviewViewModel>();


    /// <summary>
    /// 工作空间管理视图模型
    /// </summary>
    private WorkspaceManagementViewModel? _workspaceManagementViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public WorkspaceManagementViewModel WorkspaceManagementViewModel => _workspaceManagementViewModel ??= Resolve<WorkspaceManagementViewModel>();


    /// <summary>
    /// 样式管理视图模型
    /// </summary>
    private StyleManagementViewModel? _styleManagementViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public StyleManagementViewModel StyleManagementViewModel => _styleManagementViewModel ??= Resolve<StyleManagementViewModel>();


    /// <summary>
    /// 数据存储管理视图模型
    /// </summary>
    private StoresManagementViewModel? _storesManagementViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public StoresManagementViewModel StoresManagementViewModel => _storesManagementViewModel ??= Resolve<StoresManagementViewModel>();


    /// <summary>
    /// 图层管理视图模型
    /// </summary>
    private LayersManagementViewModel? _layersManagementViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public LayersManagementViewModel LayersManagementViewModel => _layersManagementViewModel ??= Resolve<LayersManagementViewModel>();


    /// <summary>
    /// 图层组管理视图模型
    /// </summary>
    private LayerGroupsManagementViewModel? _layerGroupsManagementViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public LayerGroupsManagementViewModel LayerGroupsManagementViewModel => _layerGroupsManagementViewModel ??= Resolve<LayerGroupsManagementViewModel>();


    /// <summary>
    /// 关于视图模型
    /// </summary>
    private AboutViewModel? _aboutViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public AboutViewModel AboutViewModel => _aboutViewModel ??= Resolve<AboutViewModel>();


    /// <summary>
    /// WMS 服务设置视图模型
    /// </summary>
    private WMSSettingsViewModel? _wmsSettingsViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public WMSSettingsViewModel WmsSettingsViewModel => _wmsSettingsViewModel ??= Resolve<WMSSettingsViewModel>();


    /// <summary>
    /// WFS 服务设置视图模型
    /// </summary>
    private WFSSettingsViewModel? _wfsSettingsViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public WFSSettingsViewModel WfsSettingsViewModel => _wfsSettingsViewModel ??= Resolve<WFSSettingsViewModel>();


    /// <summary>
    /// WCS 服务设置视图模型
    /// </summary>
    private WCSSettingsViewModel? _wcsSettingsViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public WCSSettingsViewModel WcsSettingsViewModel => _wcsSettingsViewModel ??= Resolve<WCSSettingsViewModel>();


    /// <summary>
    /// 全局设置视图模型
    /// </summary>
    private GlobalSettingsViewModel? _globalSettingsViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public GlobalSettingsViewModel GlobalSettingsViewModel => _globalSettingsViewModel ??= Resolve<GlobalSettingsViewModel>();


    /// <summary>
    /// 日志配置视图模型
    /// </summary>
    private LoggingViewModel? _loggingViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public LoggingViewModel LoggingViewModel => _loggingViewModel ??= Resolve<LoggingViewModel>();


    /// <summary>
    /// 缓存默认设置视图模型
    /// </summary>
    private CachingDefaultsViewModel? _cachingDefaultsViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public CachingDefaultsViewModel CachingDefaultsViewModel => _cachingDefaultsViewModel ??= Resolve<CachingDefaultsViewModel>();


    /// <summary>
    /// 格网集管理视图模型
    /// </summary>
    private GridsetsViewModel? _gridsetsViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public GridsetsViewModel GridsetsViewModel => _gridsetsViewModel ??= Resolve<GridsetsViewModel>();


    /// <summary>
    /// 磁盘配额视图模型
    /// </summary>
    private DiskQuotaViewModel? _diskQuotaViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public DiskQuotaViewModel DiskQuotaViewModel => _diskQuotaViewModel ??= Resolve<DiskQuotaViewModel>();


    /// <summary>
    /// 安全设置视图模型
    /// </summary>
    private SecuritySettingsViewModel? _securitySettingsViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public SecuritySettingsViewModel SecuritySettingsViewModel => _securitySettingsViewModel ??= Resolve<SecuritySettingsViewModel>();


    /// <summary>
    /// 用户/组/角色管理视图模型
    /// </summary>
    private UsersGroupsRolesViewModel? _usersGroupsRolesViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public UsersGroupsRolesViewModel UsersGroupsRolesViewModel => _usersGroupsRolesViewModel ??= Resolve<UsersGroupsRolesViewModel>();


    /// <summary>
    /// 数据导入向导视图模型
    /// </summary>
    private ImportWizardViewModel? _importWizardViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public ImportWizardViewModel ImportWizardViewModel => _importWizardViewModel ??= Resolve<ImportWizardViewModel>();


    /// <summary>
    /// SLD 编辑器视图模型（M3）
    /// </summary>
    private SldEditorViewModel? _sldEditorViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public SldEditorViewModel SldEditorViewModel => _sldEditorViewModel ??= Resolve<SldEditorViewModel>();


    /// <summary>
    /// 样式库视图模型（M3）
    /// </summary>
    private StyleLibraryViewModel? _styleLibraryViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public StyleLibraryViewModel StyleLibraryViewModel => _styleLibraryViewModel ??= Resolve<StyleLibraryViewModel>();


    /// <summary>
    /// 批量操作视图模型（M4）
    /// </summary>
    private BatchOperationsViewModel? _batchOperationsViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public BatchOperationsViewModel BatchOperationsViewModel => _batchOperationsViewModel ??= Resolve<BatchOperationsViewModel>();


    /// <summary>
    /// 工作空间迁移视图模型（M4）
    /// </summary>
    private WorkspaceMigrationViewModel? _workspaceMigrationViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public WorkspaceMigrationViewModel WorkspaceMigrationViewModel => _workspaceMigrationViewModel ??= Resolve<WorkspaceMigrationViewModel>();


    /// <summary>
    /// 设置同步视图模型（M4）
    /// </summary>
    private SettingsSyncViewModel? _settingsSyncViewModel;
    /// <summary>惰性解析（M5 DI）：首次访问时从容器取单例，保留跨导航状态。</summary>
    public SettingsSyncViewModel SettingsSyncViewModel => _settingsSyncViewModel ??= Resolve<SettingsSyncViewModel>();

    /// <summary>
    /// 当前显示的视图
    /// </summary>
    [ObservableProperty]
    private ViewModelBase? _currentView;

    /// <summary>
    /// 默认构造：自建组合根容器（设计期与无头测试入口）
    /// </summary>
    public MainWindowViewModel() : this(Composition.AppServices.BuildDefault())
    {
    }

    /// <summary>
    /// 容器构造（M5 DI）：基础服务由容器注入，24 个子 VM 改为首次访问时惰性解析
    /// </summary>
    /// <param name="services">应用服务提供者</param>
    public MainWindowViewModel(System.IServiceProvider services)
    {
        _services = services ?? throw new System.ArgumentNullException(nameof(services));
        _connectionService = services.GetRequiredService<IGeoServerConnectionService>();
        _connectionService.ConnectionStatusChanged += OnConnectionStatusChanged;

        // 设置默认视图为欢迎页面
        _currentView = CreateWelcomeViewModel();
    }

    /// <summary>
    /// 从容器解析子 VM 单例（M5 DI：替代构造期 eager new）
    /// </summary>
    /// <typeparam name="T">视图模型类型</typeparam>
    /// <returns>容器内的单例实例</returns>
    private T Resolve<T>() where T : ViewModelBase
    {
        var services = _services ?? throw new System.InvalidOperationException("服务容器未初始化，无法解析视图模型");
        return services.GetRequiredService<T>();
    }

    /// <summary>
    /// 当选中的节点发生变化时调用：按节点类型展开并延迟加载子级（或预览图层）
    /// </summary>
    partial void OnSelectedNodeChanged(ResourceTreeNode? value)
    {
        if (value == null || !IsConnected)
            return;

        switch (value.Type)
        {
            // 工作空间/数据存储容器：展开并延迟加载数据存储
            case ResourceType.Workspace:
            case ResourceType.DataStoresContainer:
                value.IsExpanded = true;
                _ = LoadDataStoresAsync(value);
                break;

            // 数据存储/图层容器：展开并延迟加载图层
            case ResourceType.DataStore:
            case ResourceType.LayersContainer:
                value.IsExpanded = true;
                _ = LoadLayersAsync(value);
                break;

            // 图层：加载地图预览
            case ResourceType.Layer:
                _ = PreviewLayerAsync(value);
                break;
        }
    }

    /// <summary>
    /// 预览图层
    /// </summary>
    /// <param name="layerNode">图层节点</param>
    private async Task PreviewLayerAsync(ResourceTreeNode layerNode)
    {
        try
        {
            // 从树层次结构中向上查找所属工作空间（图层 → 图层容器 → 数据存储 → 数据存储容器 → 工作空间）
            var workspaceNode = FindAncestor(ResourceTree[0], layerNode, ResourceType.Workspace);
            if (workspaceNode == null) return;

            var workspaceName = workspaceNode.Name;
            var layerName = layerNode.Name;

            await MapPreviewViewModel.LoadWmsLayerAsync(BaseUrl, workspaceName, layerName);
            StatusMessage = string.Format(L.StatusPreviewReady, $"{workspaceName}:{layerName}");
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(L.StatusPreviewFailed, ex.Message);
        }
    }

    /// <summary>
    /// 连接到 GeoServer 的命令
    /// </summary>
    [RelayCommand]
    private async Task ConnectAsync()
    {
        try
        {
            StatusMessage = L.StatusConnecting;

            var options = new GeoServerClientOptions
            {
                BaseUrl = BaseUrl,
                Username = Username,
                Password = Password
            };

            _connectionService.Connect(options);

            // FIXED-E32：Connect 仅保存配置不发请求（库层无探测），这里做真实连通性探测：
            // GET /rest/about/version.json 失败（不可达/非 GeoServer/凭据错误）则回退为未连接并提示错误。
            try
            {
                await _connectionService.GetAboutService().GetVersionAsync();
            }
            catch (Exception ex)
            {
                _connectionService.Disconnect(); // 触发 ConnectionStatusChanged → IsConnected=false
                StatusMessage = string.Format(L.StatusConnectVerifyFailed, ex.Message);
                return;
            }

            StatusMessage = L.StatusConnectedSuccess;
            await LoadResourceTreeAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(L.StatusConnectionFailed, ex.Message);
            IsConnected = false;
        }
    }

    /// <summary>
    /// 断开与 GeoServer 连接的命令
    /// </summary>
    [RelayCommand]
    private void Disconnect()
    {
        _connectionService.Disconnect();
        ResourceTree.Clear();
        CurrentView = CreateWelcomeViewModel();
        StatusMessage = L.StatusDisconnected;
    }

    /// <summary>
    /// 刷新资源的命令
    /// </summary>
    [RelayCommand]
    private async Task RefreshResourcesAsync()
    {
        if (!IsConnected) return;

        try
        {
            StatusMessage = L.StatusRefreshing;
            await LoadResourceTreeAsync();
            StatusMessage = L.StatusRefreshed;
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(L.StatusRefreshFailed, ex.Message);
        }
    }

    /// <summary>
    /// 显示关于页面
    /// </summary>
    [RelayCommand]
    private void ShowAbout()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = AboutViewModel;
        _ = AboutViewModel.LoadSystemInfoCommand.ExecuteAsync(null);
        StatusMessage = L.StatusAbout;
    }

    /// <summary>
    /// 显示图层预览页面
    /// </summary>
    [RelayCommand]
    private void ShowLayerPreview()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = MapPreviewViewModel;
        StatusMessage = L.StatusLayerPreview;
    }

    /// <summary>
    /// 显示工作空间页面
    /// </summary>
    [RelayCommand]
    private async Task ShowWorkspacesAsync()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = WorkspaceManagementViewModel;
        try
        {
            await WorkspaceManagementViewModel.LoadWorkspacesCommand.ExecuteAsync(null);
        }
        catch
        {
            // 错误已在 ViewModel 中处理
        }
        StatusMessage = L.StatusWorkspaces;
    }

    /// <summary>
    /// 显示数据存储页面
    /// </summary>
    [RelayCommand]
    private void ShowStores()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = CreateStoresViewModel();
        StatusMessage = L.StatusDataStores;
    }

    /// <summary>
    /// 显示数据导入向导页面
    /// </summary>
    [RelayCommand]
    private void ShowImportWizard()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = CreateImportWizardViewModel();
        StatusMessage = L.NavImportWizard;
    }

    /// <summary>
    /// 显示图层页面
    /// </summary>
    [RelayCommand]
    private void ShowLayers()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = CreateLayersViewModel();
        StatusMessage = L.StatusLayers;
    }

    /// <summary>
    /// 显示图层组页面
    /// </summary>
    [RelayCommand]
    private void ShowLayerGroups()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = CreateLayerGroupsViewModel();
        StatusMessage = L.StatusLayerGroups;
    }

    /// <summary>
    /// 显示样式页面
    /// </summary>
    [RelayCommand]
    private void ShowStyles()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = StyleManagementViewModel;
        StatusMessage = L.StatusStyles;
    }

    /// <summary>
    /// 显示 SLD 编辑器页面（M3）
    /// </summary>
    [RelayCommand]
    private void ShowSldEditor()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = SldEditorViewModel;
        _ = SldEditorViewModel.LoadStylesCommand.ExecuteAsync(null);
        _ = SldEditorViewModel.LoadPreviewLayersCommand.ExecuteAsync(null);
        StatusMessage = L.NavSldEditor;
    }

    /// <summary>
    /// 显示样式库页面（M3）
    /// </summary>
    [RelayCommand]
    private void ShowStyleLibrary()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = StyleLibraryViewModel;
        _ = StyleLibraryViewModel.LoadUsagesCommand.ExecuteAsync(null);
        StatusMessage = L.NavStyleLibrary;
    }

    /// <summary>
    /// 挂接工作空间迁移的文件对话框（由 MainWindow 代码后台调用；ViewModel 保持无头可测）
    /// </summary>
    /// <param name="window">主窗口</param>
    public void BindFilePickers(Views.MainWindow window)
    {
        WorkspaceMigrationViewModel.SaveFilePicker =
            (description, defaultName) => window.PickSavePathAsync(description, defaultName);
        WorkspaceMigrationViewModel.OpenFilePicker =
            description => window.PickOpenPathAsync(description);
    }

    /// <summary>
    /// 显示批量操作页面（M4）
    /// </summary>
    [RelayCommand]
    private void ShowBatchOperations()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = BatchOperationsViewModel;
        _ = BatchOperationsViewModel.LoadItemsCommand.ExecuteAsync(null);
        StatusMessage = L.NavBatch;
    }

    /// <summary>
    /// 显示工作空间迁移页面（M4）
    /// </summary>
    [RelayCommand]
    private void ShowWorkspaceMigration()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = WorkspaceMigrationViewModel;
        _ = WorkspaceMigrationViewModel.LoadCommand.ExecuteAsync(null);
        StatusMessage = L.NavMigration;
    }

    /// <summary>
    /// 显示设置同步页面（M4）
    /// </summary>
    [RelayCommand]
    private void ShowSettingsSync()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = SettingsSyncViewModel;
        StatusMessage = L.NavSettingsSync;
    }

    /// <summary>
    /// 显示 WMS 设置页面
    /// </summary>
    [RelayCommand]
    private void ShowWMSSettings()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = WmsSettingsViewModel;
        _ = WmsSettingsViewModel.LoadSettingsCommand.ExecuteAsync(null);
        StatusMessage = L.StatusWmsSettings;
    }

    /// <summary>
    /// 显示 WFS 设置页面
    /// </summary>
    [RelayCommand]
    private void ShowWFSSettings()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = WfsSettingsViewModel;
        _ = WfsSettingsViewModel.LoadSettingsCommand.ExecuteAsync(null);
        StatusMessage = L.StatusWfsSettings;
    }

    /// <summary>
    /// 显示 WCS 设置页面
    /// </summary>
    [RelayCommand]
    private void ShowWCSSettings()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = WcsSettingsViewModel;
        _ = WcsSettingsViewModel.LoadSettingsCommand.ExecuteAsync(null);
        StatusMessage = L.StatusWcsSettings;
    }

    /// <summary>
    /// 显示全局设置页面
    /// </summary>
    [RelayCommand]
    private void ShowGlobalSettings()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = GlobalSettingsViewModel;
        _ = GlobalSettingsViewModel.LoadSettingsCommand.ExecuteAsync(null);
        StatusMessage = L.StatusGlobalSettings;
    }

    /// <summary>
    /// 显示日志设置页面
    /// </summary>
    [RelayCommand]
    private void ShowLogging()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = LoggingViewModel;
        _ = LoggingViewModel.LoadSettingsCommand.ExecuteAsync(null);
        StatusMessage = L.StatusLoggingSettings;
    }

    /// <summary>
    /// 显示缓存默认设置页面
    /// </summary>
    [RelayCommand]
    private void ShowCachingDefaults()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = CachingDefaultsViewModel;
        _ = CachingDefaultsViewModel.LoadSettingsCommand.ExecuteAsync(null);
        StatusMessage = L.StatusTileCachingDefaults;
    }

    /// <summary>
    /// 显示 Gridsets 页面
    /// </summary>
    [RelayCommand]
    private void ShowGridsets()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = GridsetsViewModel;
        _ = GridsetsViewModel.LoadGridsetsCommand.ExecuteAsync(null);
        StatusMessage = L.StatusGridsets;
    }

    /// <summary>
    /// 显示磁盘配额页面
    /// </summary>
    [RelayCommand]
    private void ShowDiskQuota()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = DiskQuotaViewModel;
        _ = DiskQuotaViewModel.LoadSettingsCommand.ExecuteAsync(null);
        StatusMessage = L.StatusDiskQuota;
    }

    /// <summary>
    /// 显示安全设置页面
    /// </summary>
    [RelayCommand]
    private void ShowSecuritySettings()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = SecuritySettingsViewModel;
        _ = SecuritySettingsViewModel.LoadSettingsCommand.ExecuteAsync(null);
        StatusMessage = L.StatusSecuritySettings;
    }

    /// <summary>
    /// 显示用户组页面
    /// </summary>
    [RelayCommand]
    private void ShowUsersGroups()
    {
        if (!IsConnected)
        {
            StatusMessage = L.StatusPleaseConnect;
            return;
        }
        CurrentView = UsersGroupsRolesViewModel;
        _ = UsersGroupsRolesViewModel.LoadAllCommand.ExecuteAsync(null);
        StatusMessage = L.StatusUsersGroupsAndRoles;
    }

    /// <summary>
    /// 连接状态变化时的处理程序
    /// </summary>
    private void OnConnectionStatusChanged(object? sender, bool isConnected)
    {
        IsConnected = isConnected;
    }

    /// <summary>
    /// 加载资源树
    /// </summary>
    private async Task LoadResourceTreeAsync()
    {
        try
        {
            ResourceTree.Clear();

            var rootNode = new ResourceTreeNode
            {
                Name = "GeoServer",
                Type = ResourceType.GeoServer,
                IsExpanded = true
            };

            // 添加工作空间容器
            var workspacesContainer = new ResourceTreeNode
            {
                Name = L.TreeWorkspaces,
                Type = ResourceType.WorkspacesContainer,
                IsExpanded = true
            };

            var workspaceService = _connectionService.GetWorkspaceService();
            var workspaces = await workspaceService.GetWorkspacesAsync();

            foreach (var workspace in workspaces)
            {
                var workspaceNode = new ResourceTreeNode
                {
                    Name = workspace.Name,
                    Type = ResourceType.Workspace,
                    Tag = workspace
                };

                // 添加用于延迟加载的占位符节点
                var dataStoresContainer = new ResourceTreeNode
                {
                    Name = L.TreeDataStores,
                    Type = ResourceType.DataStoresContainer,
                    Tag = workspace.Name
                };
                workspaceNode.Children.Add(dataStoresContainer);

                workspacesContainer.Children.Add(workspaceNode);
            }

            rootNode.Children.Add(workspacesContainer);

            // 添加样式容器
            var stylesContainer = new ResourceTreeNode
            {
                Name = L.TreeStyles,
                Type = ResourceType.StylesContainer
            };
            rootNode.Children.Add(stylesContainer);

            // 添加图层组容器
            var layerGroupsContainer = new ResourceTreeNode
            {
                Name = L.TreeLayerGroups,
                Type = ResourceType.LayerGroupsContainer
            };
            rootNode.Children.Add(layerGroupsContainer);

            ResourceTree.Add(rootNode);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(L.StatusResourcesLoadFailed, ex.Message);
        }
    }

    /// <summary>
    /// 加载工作空间的数据存储（延迟加载；支持工作空间节点或其数据存储容器节点；已加载则跳过）
    /// </summary>
    /// <param name="node">工作空间节点或数据存储容器节点</param>
    public async Task LoadDataStoresAsync(ResourceTreeNode node)
    {
        if (ResourceTree.Count == 0)
            return;

        // 解析工作空间节点：直接传入工作空间，或从数据存储容器向上查找
        var workspaceNode = node.Type == ResourceType.Workspace
            ? node
            : FindAncestor(ResourceTree[0], node, ResourceType.Workspace);
        if (workspaceNode == null)
            return;

        var dataStoresContainer = workspaceNode.Children.FirstOrDefault(n => n.Type == ResourceType.DataStoresContainer);
        if (dataStoresContainer == null || dataStoresContainer.IsLoaded)
            return;

        try
        {
            var workspaceName = workspaceNode.Name;
            var dataStoreService = _connectionService.GetDataStoreService();
            var dataStores = await dataStoreService.GetDataStoresAsync(workspaceName);

            dataStoresContainer.Children.Clear();

            foreach (var dataStore in dataStores)
            {
                var dataStoreNode = new ResourceTreeNode
                {
                    Name = dataStore.Name,
                    Type = ResourceType.DataStore,
                    Tag = dataStore
                };

                // 添加图层的占位符
                var layersContainer = new ResourceTreeNode
                {
                    Name = L.TreeLayers,
                    Type = ResourceType.LayersContainer,
                    Tag = new { WorkspaceName = workspaceName, DataStoreName = dataStore.Name }
                };
                dataStoreNode.Children.Add(layersContainer);

                dataStoresContainer.Children.Add(dataStoreNode);
            }

            dataStoresContainer.IsLoaded = true;
            dataStoresContainer.IsExpanded = true;
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(L.StatusDataStoresLoadFailed, ex.Message);
        }
    }

    /// <summary>
    /// 加载数据存储的图层（延迟加载；支持数据存储节点或其图层容器节点；已加载则跳过）
    /// </summary>
    /// <param name="node">数据存储节点或图层容器节点</param>
    public async Task LoadLayersAsync(ResourceTreeNode node)
    {
        if (ResourceTree.Count == 0)
            return;

        // 解析数据存储节点：直接传入数据存储，或从图层容器向上查找
        var dataStoreNode = node.Type == ResourceType.DataStore
            ? node
            : FindAncestor(ResourceTree[0], node, ResourceType.DataStore);
        if (dataStoreNode == null)
            return;

        // 向上查找所属工作空间
        var workspaceNode = FindAncestor(ResourceTree[0], dataStoreNode, ResourceType.Workspace);
        if (workspaceNode == null)
            return;

        var layersContainer = dataStoreNode.Children.FirstOrDefault(n => n.Type == ResourceType.LayersContainer);
        if (layersContainer == null || layersContainer.IsLoaded)
            return;

        try
        {
            var workspaceName = workspaceNode.Name;
            var dataStoreName = dataStoreNode.Name;

            var featureTypeService = _connectionService.GetFeatureTypeService();
            var featureTypes = await featureTypeService.GetFeatureTypesAsync(workspaceName, dataStoreName);

            layersContainer.Children.Clear();

            foreach (var featureType in featureTypes)
            {
                var layerNode = new ResourceTreeNode
                {
                    Name = featureType.Name,
                    Type = ResourceType.Layer,
                    Tag = featureType
                };
                layersContainer.Children.Add(layerNode);
            }

            layersContainer.IsLoaded = true;
            layersContainer.IsExpanded = true;
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(L.StatusLayersLoadFailed, ex.Message);
        }
    }

    /// <summary>
    /// 获取节点的父节点
    /// </summary>
    /// <param name="root">根节点</param>
    /// <param name="target">目标节点</param>
    /// <returns>父节点，如果未找到则返回 null</returns>
    private ResourceTreeNode? GetParentNode(ResourceTreeNode root, ResourceTreeNode target)
    {
        if (root.Children.Contains(target))
            return root;

        foreach (var child in root.Children)
        {
            var parent = GetParentNode(child, target);
            if (parent != null)
                return parent;
        }

        return null;
    }

    /// <summary>
    /// 从指定节点向上查找最近的指定类型祖先节点
    /// </summary>
    /// <param name="root">根节点</param>
    /// <param name="target">起始节点</param>
    /// <param name="type">目标祖先类型</param>
    /// <returns>祖先节点，如果未找到则返回 null</returns>
    private ResourceTreeNode? FindAncestor(ResourceTreeNode root, ResourceTreeNode target, ResourceType type)
    {
        var parent = GetParentNode(root, target);
        while (parent != null)
        {
            if (parent.Type == type)
                return parent;
            parent = GetParentNode(root, parent);
        }

        return null;
    }

    // Factory methods for creating ViewModels
    private ViewModelBase CreateWelcomeViewModel()
    {
        return DashboardViewModel;
    }

    private ViewModelBase CreateStoresViewModel()
    {
        return StoresManagementViewModel;
    }

    private ViewModelBase CreateImportWizardViewModel()
    {
        return ImportWizardViewModel;
    }

    private ViewModelBase CreateLayersViewModel()
    {
        return LayersManagementViewModel;
    }

    private ViewModelBase CreateLayerGroupsViewModel()
    {
        return LayerGroupsManagementViewModel;
    }
}
