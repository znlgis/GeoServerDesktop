using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Models;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Configuration;
using System;
using System.Collections.ObjectModel;
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

    public MainWindowViewModel()
    {
        _connectionService = new GeoServerConnectionService();
        _connectionService.ConnectionStatusChanged += OnConnectionStatusChanged;
        _mapPreviewViewModel = new MapPreviewViewModel();
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
}
