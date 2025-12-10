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

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IGeoServerConnectionService _connectionService;

    [ObservableProperty]
    private string _baseUrl = "http://localhost:8080/geoserver";

    [ObservableProperty]
    private string _username = "admin";

    [ObservableProperty]
    private string _password = "geoserver";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusMessage = "Not connected";

    [ObservableProperty]
    private ObservableCollection<ResourceTreeNode> _resourceTree = new();

    [ObservableProperty]
    private ResourceTreeNode? _selectedNode;

    [ObservableProperty]
    private MapPreviewViewModel _mapPreviewViewModel;

    [ObservableProperty]
    private WorkspaceManagementViewModel _workspaceManagementViewModel;

    [ObservableProperty]
    private StyleManagementViewModel _styleManagementViewModel;

    public MainWindowViewModel()
    {
        _connectionService = new GeoServerConnectionService();
        _connectionService.ConnectionStatusChanged += OnConnectionStatusChanged;
        _mapPreviewViewModel = new MapPreviewViewModel();
        _workspaceManagementViewModel = new WorkspaceManagementViewModel(_connectionService);
        _styleManagementViewModel = new StyleManagementViewModel(_connectionService);
    }

    partial void OnSelectedNodeChanged(ResourceTreeNode? value)
    {
        // When a layer is selected, preview it
        if (value?.Type == ResourceType.Layer && IsConnected)
        {
            _ = PreviewLayerAsync(value);
        }
    }

    private async Task PreviewLayerAsync(ResourceTreeNode layerNode)
    {
        try
        {
            // Find workspace name from tree hierarchy
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

    [RelayCommand]
    private void Disconnect()
    {
        _connectionService.Disconnect();
        ResourceTree.Clear();
        StatusMessage = "Disconnected";
    }

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

    private void OnConnectionStatusChanged(object? sender, bool isConnected)
    {
        IsConnected = isConnected;
    }

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

            // Add Workspaces
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
                
                // Add placeholder nodes for lazy loading
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

            // Add Styles container
            var stylesContainer = new ResourceTreeNode
            {
                Name = "Styles",
                Type = ResourceType.StylesContainer
            };
            rootNode.Children.Add(stylesContainer);

            // Add Layer Groups container
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
    /// Loads data stores for a workspace
    /// </summary>
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
                    
                    // Add placeholder for layers
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
    /// Loads layers for a data store
    /// </summary>
    public async Task LoadLayersAsync(ResourceTreeNode dataStoreNode)
    {
        if (dataStoreNode.Type != ResourceType.DataStore)
            return;

        try
        {
            // Get workspace name from parent
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
}
