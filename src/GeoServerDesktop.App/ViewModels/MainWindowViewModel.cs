using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Models;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Configuration;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

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
            StatusMessage = $"Preview ready for {workspaceName}:{layerName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to preview layer: {ex.Message}";
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
            StatusMessage = "Connecting...";

            var options = new GeoServerClientOptions
            {
                BaseUrl = BaseUrl,
                Username = Username,
                Password = Password
            };

            _connectionService.Connect(options);

            StatusMessage = "Connected successfully";
            await LoadResourceTreeAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connection failed: {ex.Message}";
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
        StatusMessage = "Disconnected";
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
            StatusMessage = "Refreshing resources...";
            await LoadResourceTreeAsync();
            StatusMessage = "Resources refreshed";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to refresh: {ex.Message}";
        }
    }

    /// <summary>
    /// 显示服务器状态页面
    /// </summary>
    [RelayCommand]
    private void ShowServerStatus()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = CreateServerStatusViewModel();
        StatusMessage = "Server Status";
    }

    /// <summary>
    /// 显示关于页面
    /// </summary>
    [RelayCommand]
    private void ShowAbout()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = CreateAboutViewModel();
        StatusMessage = "About GeoServer";
    }

    /// <summary>
    /// 显示图层预览页面
    /// </summary>
    [RelayCommand]
    private void ShowLayerPreview()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = MapPreviewViewModel;
        StatusMessage = "Layer Preview";
    }

    /// <summary>
    /// 显示工作空间页面
    /// </summary>
    [RelayCommand]
    private void ShowWorkspaces()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = WorkspaceManagementViewModel;
        _ = WorkspaceManagementViewModel.LoadWorkspacesCommand.ExecuteAsync(null);
        StatusMessage = "Workspaces";
    }

    /// <summary>
    /// 显示数据存储页面
    /// </summary>
    [RelayCommand]
    private void ShowStores()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = CreateStoresViewModel();
        StatusMessage = "Data Stores";
    }

    /// <summary>
    /// 显示图层页面
    /// </summary>
    [RelayCommand]
    private void ShowLayers()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = CreateLayersViewModel();
        StatusMessage = "Layers";
    }

    /// <summary>
    /// 显示图层组页面
    /// </summary>
    [RelayCommand]
    private void ShowLayerGroups()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = CreateLayerGroupsViewModel();
        StatusMessage = "Layer Groups";
    }

    /// <summary>
    /// 显示样式页面
    /// </summary>
    [RelayCommand]
    private void ShowStyles()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = StyleManagementViewModel;
        StatusMessage = "Styles";
    }

    /// <summary>
    /// 显示 WMS 设置页面
    /// </summary>
    [RelayCommand]
    private void ShowWMSSettings()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = CreateWMSSettingsViewModel();
        StatusMessage = "WMS Settings";
    }

    /// <summary>
    /// 显示 WFS 设置页面
    /// </summary>
    [RelayCommand]
    private void ShowWFSSettings()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = CreateWFSSettingsViewModel();
        StatusMessage = "WFS Settings";
    }

    /// <summary>
    /// 显示 WCS 设置页面
    /// </summary>
    [RelayCommand]
    private void ShowWCSSettings()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = CreateWCSSettingsViewModel();
        StatusMessage = "WCS Settings";
    }

    /// <summary>
    /// 显示全局设置页面
    /// </summary>
    [RelayCommand]
    private void ShowGlobalSettings()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = CreateGlobalSettingsViewModel();
        StatusMessage = "Global Settings";
    }

    /// <summary>
    /// 显示日志设置页面
    /// </summary>
    [RelayCommand]
    private void ShowLogging()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = CreateLoggingViewModel();
        StatusMessage = "Logging Settings";
    }

    /// <summary>
    /// 显示缓存默认设置页面
    /// </summary>
    [RelayCommand]
    private void ShowCachingDefaults()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = CreateCachingDefaultsViewModel();
        StatusMessage = "Tile Caching Defaults";
    }

    /// <summary>
    /// 显示 Gridsets 页面
    /// </summary>
    [RelayCommand]
    private void ShowGridsets()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = CreateGridsetsViewModel();
        StatusMessage = "Gridsets";
    }

    /// <summary>
    /// 显示磁盘配额页面
    /// </summary>
    [RelayCommand]
    private void ShowDiskQuota()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = CreateDiskQuotaViewModel();
        StatusMessage = "Disk Quota";
    }

    /// <summary>
    /// 显示安全设置页面
    /// </summary>
    [RelayCommand]
    private void ShowSecuritySettings()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = CreateSecuritySettingsViewModel();
        StatusMessage = "Security Settings";
    }

    /// <summary>
    /// 显示用户组页面
    /// </summary>
    [RelayCommand]
    private void ShowUsersGroups()
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect to GeoServer first";
            return;
        }
        CurrentView = CreateUsersGroupsViewModel();
        StatusMessage = "Users, Groups, and Roles";
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
            StatusMessage = $"Failed to load resources: {ex.Message}";
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
            StatusMessage = $"Failed to load data stores: {ex.Message}";
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
            StatusMessage = $"Failed to load layers: {ex.Message}";
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

    private ViewModelBase CreateServerStatusViewModel()
    {
        return new PlaceholderViewModel("Server Status", 
            "Server status information will be displayed here. This includes uptime, resource usage, and active connections.");
    }

    private ViewModelBase CreateAboutViewModel()
    {
        return new PlaceholderViewModel("About GeoServer", 
            "GeoServer version and system information will be displayed here.");
    }

    private ViewModelBase CreateStoresViewModel()
    {
        return new PlaceholderViewModel("Data Stores", 
            "Data store management interface will be displayed here. This allows you to configure connections to PostGIS, Shapefiles, and other data sources.");
    }

    private ViewModelBase CreateLayersViewModel()
    {
        return new PlaceholderViewModel("Layers", 
            "Layer management interface will be displayed here. This allows you to publish and configure vector and raster layers.");
    }

    private ViewModelBase CreateLayerGroupsViewModel()
    {
        return new PlaceholderViewModel("Layer Groups", 
            "Layer group management interface will be displayed here. This allows you to organize layers into logical groups.");
    }

    private ViewModelBase CreateWMSSettingsViewModel()
    {
        return new PlaceholderViewModel("WMS Settings", 
            "Web Map Service (WMS) configuration will be displayed here.");
    }

    private ViewModelBase CreateWFSSettingsViewModel()
    {
        return new PlaceholderViewModel("WFS Settings", 
            "Web Feature Service (WFS) configuration will be displayed here.");
    }

    private ViewModelBase CreateWCSSettingsViewModel()
    {
        return new PlaceholderViewModel("WCS Settings", 
            "Web Coverage Service (WCS) configuration will be displayed here.");
    }

    private ViewModelBase CreateGlobalSettingsViewModel()
    {
        return new PlaceholderViewModel("Global Settings", 
            "Global GeoServer settings will be displayed here. This includes contact information, proxy settings, and other global configurations.");
    }

    private ViewModelBase CreateLoggingViewModel()
    {
        return new PlaceholderViewModel("Logging Settings", 
            "Logging configuration will be displayed here. This allows you to configure log levels and output destinations.");
    }

    private ViewModelBase CreateCachingDefaultsViewModel()
    {
        return new PlaceholderViewModel("Tile Caching Defaults", 
            "GeoWebCache default settings will be displayed here.");
    }

    private ViewModelBase CreateGridsetsViewModel()
    {
        return new PlaceholderViewModel("Gridsets", 
            "Gridset management for tile caching will be displayed here.");
    }

    private ViewModelBase CreateDiskQuotaViewModel()
    {
        return new PlaceholderViewModel("Disk Quota", 
            "Disk quota configuration for tile cache will be displayed here.");
    }

    private ViewModelBase CreateSecuritySettingsViewModel()
    {
        return new PlaceholderViewModel("Security Settings", 
            "Security configuration will be displayed here. This includes authentication and authorization settings.");
    }

    private ViewModelBase CreateUsersGroupsViewModel()
    {
        return new PlaceholderViewModel("Users, Groups, and Roles", 
            "User, group, and role management will be displayed here.");
    }
}
