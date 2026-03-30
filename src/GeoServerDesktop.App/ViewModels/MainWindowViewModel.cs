using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Models;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Configuration;

namespace GeoServerDesktop.App.ViewModels;

/// <summary>
/// 主窗口的视图模型
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IGeoServerConnectionService _connectionService;

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
    /// 地图预览视图模型
    /// </summary>
    [ObservableProperty]
    private MapPreviewViewModel _mapPreviewViewModel;

    /// <summary>
    /// 工作空间管理视图模型
    /// </summary>
    [ObservableProperty]
    private WorkspaceManagementViewModel _workspaceManagementViewModel;

    /// <summary>
    /// 样式管理视图模型
    /// </summary>
    [ObservableProperty]
    private StyleManagementViewModel _styleManagementViewModel;

    /// <summary>
    /// 数据存储管理视图模型
    /// </summary>
    [ObservableProperty]
    private StoresManagementViewModel _storesManagementViewModel;

    /// <summary>
    /// 图层管理视图模型
    /// </summary>
    [ObservableProperty]
    private LayersManagementViewModel _layersManagementViewModel;

    /// <summary>
    /// 图层组管理视图模型
    /// </summary>
    [ObservableProperty]
    private LayerGroupsManagementViewModel _layerGroupsManagementViewModel;

    /// <summary>
    /// 关于视图模型
    /// </summary>
    [ObservableProperty]
    private AboutViewModel _aboutViewModel;

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

    /// <summary>
    /// 全局设置视图模型
    /// </summary>
    [ObservableProperty]
    private GlobalSettingsViewModel _globalSettingsViewModel;

    /// <summary>
    /// 日志配置视图模型
    /// </summary>
    [ObservableProperty]
    private LoggingViewModel _loggingViewModel;

    /// <summary>
    /// 缓存默认设置视图模型
    /// </summary>
    [ObservableProperty]
    private CachingDefaultsViewModel _cachingDefaultsViewModel;

    /// <summary>
    /// 格网集管理视图模型
    /// </summary>
    [ObservableProperty]
    private GridsetsViewModel _gridsetsViewModel;

    /// <summary>
    /// 磁盘配额视图模型
    /// </summary>
    [ObservableProperty]
    private DiskQuotaViewModel _diskQuotaViewModel;

    /// <summary>
    /// 安全设置视图模型
    /// </summary>
    [ObservableProperty]
    private SecuritySettingsViewModel _securitySettingsViewModel;

    /// <summary>
    /// 用户/组/角色管理视图模型
    /// </summary>
    [ObservableProperty]
    private UsersGroupsRolesViewModel _usersGroupsRolesViewModel;

    /// <summary>
    /// 当前显示的视图
    /// </summary>
    [ObservableProperty]
    private ViewModelBase? _currentView;

    /// <summary>
    /// 初始化 MainWindowViewModel 类的新实例
    /// </summary>
    public MainWindowViewModel()
    {
        _connectionService = new GeoServerConnectionService();
        _connectionService.ConnectionStatusChanged += OnConnectionStatusChanged;
        _mapPreviewViewModel = new MapPreviewViewModel();
        _workspaceManagementViewModel = new WorkspaceManagementViewModel(_connectionService);
        _styleManagementViewModel = new StyleManagementViewModel(_connectionService);
        _storesManagementViewModel = new StoresManagementViewModel(_connectionService);
        _layersManagementViewModel = new LayersManagementViewModel(_connectionService);
        _layerGroupsManagementViewModel = new LayerGroupsManagementViewModel(_connectionService);
        _aboutViewModel = new AboutViewModel(_connectionService);
        _wmsSettingsViewModel = new WMSSettingsViewModel(_connectionService);
        _wfsSettingsViewModel = new WFSSettingsViewModel(_connectionService);
        _wcsSettingsViewModel = new WCSSettingsViewModel(_connectionService);
        _globalSettingsViewModel = new GlobalSettingsViewModel(_connectionService);
        _loggingViewModel = new LoggingViewModel(_connectionService);
        _cachingDefaultsViewModel = new CachingDefaultsViewModel(_connectionService);
        _gridsetsViewModel = new GridsetsViewModel(_connectionService);
        _diskQuotaViewModel = new DiskQuotaViewModel(_connectionService);
        _securitySettingsViewModel = new SecuritySettingsViewModel(_connectionService);
        _usersGroupsRolesViewModel = new UsersGroupsRolesViewModel(_connectionService);

        // 设置默认视图为欢迎页面
        _currentView = CreateWelcomeViewModel();
    }

    /// <summary>
    /// 当选中的节点发生变化时调用
    /// </summary>
    partial void OnSelectedNodeChanged(ResourceTreeNode? value)
    {
        // 当选中图层时，预览它
        if (value?.Type == ResourceType.Layer && IsConnected)
        {
            _ = PreviewLayerAsync(value);
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
            // 从树层次结构中查找工作空间名称
            var dataStoreNode = GetParentNode(ResourceTree[0], layerNode);
            if (dataStoreNode == null) return;

            var workspaceNode = GetParentNode(ResourceTree[0], dataStoreNode);
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
                Name = "Workspaces",
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
                    Name = "Data Stores",
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
                Name = "Styles",
                Type = ResourceType.StylesContainer
            };
            rootNode.Children.Add(stylesContainer);

            // 添加图层组容器
            var layerGroupsContainer = new ResourceTreeNode
            {
                Name = "Layer Groups",
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
    /// 加载工作空间的数据存储
    /// </summary>
    /// <param name="workspaceNode">工作空间节点</param>
    public async Task LoadDataStoresAsync(ResourceTreeNode workspaceNode)
    {
        if (workspaceNode.Type != ResourceType.Workspace)
            return;

        try
        {
            var workspaceName = workspaceNode.Name;
            var dataStoreService = _connectionService.GetDataStoreService();
            var dataStores = await dataStoreService.GetDataStoresAsync(workspaceName);

            var dataStoresContainer = workspaceNode.Children.FirstOrDefault(n => n.Type == ResourceType.DataStoresContainer);
            if (dataStoresContainer != null)
            {
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
                        Name = "Layers",
                        Type = ResourceType.LayersContainer,
                        Tag = new { WorkspaceName = workspaceName, DataStoreName = dataStore.Name }
                    };
                    dataStoreNode.Children.Add(layersContainer);

                    dataStoresContainer.Children.Add(dataStoreNode);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(L.StatusDataStoresLoadFailed, ex.Message);
        }
    }

    /// <summary>
    /// 加载数据存储的图层
    /// </summary>
    /// <param name="dataStoreNode">数据存储节点</param>
    public async Task LoadLayersAsync(ResourceTreeNode dataStoreNode)
    {
        if (dataStoreNode.Type != ResourceType.DataStore)
            return;

        try
        {
            // 从父节点获取工作空间名称
            var workspaceNode = GetParentNode(ResourceTree[0], dataStoreNode);
            if (workspaceNode == null || workspaceNode.Type != ResourceType.Workspace)
                return;

            var workspaceName = workspaceNode.Name;
            var dataStoreName = dataStoreNode.Name;

            var featureTypeService = _connectionService.GetFeatureTypeService();
            var featureTypes = await featureTypeService.GetFeatureTypesAsync(workspaceName, dataStoreName);

            var layersContainer = dataStoreNode.Children.FirstOrDefault(n => n.Type == ResourceType.LayersContainer);
            if (layersContainer != null)
            {
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
            }
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

    // Factory methods for creating ViewModels
    private ViewModelBase CreateWelcomeViewModel()
    {
        return new PlaceholderViewModel("Welcome to GeoServer Desktop",
            "Connect to a GeoServer instance using the login form above to begin managing your spatial data services.");
    }

    private ViewModelBase CreateStoresViewModel()
    {
        return StoresManagementViewModel;
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
