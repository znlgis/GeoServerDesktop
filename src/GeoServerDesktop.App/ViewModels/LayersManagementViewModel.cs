using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// 图层管理的视图模型
    /// </summary>
    public partial class LayersManagementViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>
        /// 工作空间列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _workspaces = new();

        /// <summary>
        /// 选中的工作空间
        /// </summary>
        [ObservableProperty]
        private string? _selectedWorkspace;

        /// <summary>
        /// 图层列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Layer> _layers = new();

        /// <summary>
        /// 选中的图层
        /// </summary>
        [ObservableProperty]
        private Layer? _selectedLayer;

        /// <summary>
        /// 状态消息
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = "Ready";

        /// <summary>
        /// 是否正在加载
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// 初始化 LayersManagementViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public LayersManagementViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// 加载工作空间列表
        /// </summary>
        [RelayCommand]
        private async Task LoadWorkspacesAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = "Not connected to GeoServer";
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading workspaces...";

            try
            {
                var workspaceService = _connectionService.GetWorkspaceService();
                var workspaceList = await workspaceService.GetWorkspacesAsync();

                Workspaces.Clear();
                Workspaces.Add("All Workspaces"); // 添加"所有工作空间"选项
                
                foreach (var workspace in workspaceList)
                {
                    Workspaces.Add(workspace.Name);
                }

                StatusMessage = $"Loaded {Workspaces.Count - 1} workspaces";
                
                // 默认选择"所有工作空间"
                if (Workspaces.Count > 0)
                {
                    SelectedWorkspace = Workspaces[0];
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load workspaces: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 当选中的工作空间变化时
        /// </summary>
        partial void OnSelectedWorkspaceChanged(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _ = LoadLayersAsync();
            }
        }

        /// <summary>
        /// 加载图层列表
        /// </summary>
        [RelayCommand]
        private async Task LoadLayersAsync()
        {
            if (string.IsNullOrEmpty(SelectedWorkspace))
            {
                StatusMessage = "No workspace selected";
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading layers...";

            try
            {
                var layerService = _connectionService.GetLayerService();
                Layer[] layerArray;

                if (SelectedWorkspace == "All Workspaces")
                {
                    // 加载所有图层
                    layerArray = await layerService.GetLayersAsync();
                }
                else
                {
                    // 加载特定工作空间的图层
                    layerArray = await layerService.GetWorkspaceLayersAsync(SelectedWorkspace);
                }

                Layers.Clear();
                foreach (var layer in layerArray)
                {
                    Layers.Add(layer);
                }

                StatusMessage = $"Loaded {Layers.Count} layers";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load layers: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 删除选中的图层
        /// </summary>
        [RelayCommand]
        private async Task DeleteLayerAsync()
        {
            if (SelectedLayer == null)
            {
                StatusMessage = "No layer selected";
                return;
            }

            IsLoading = true;
            StatusMessage = $"Deleting layer '{SelectedLayer.Name}'...";

            try
            {
                var layerService = _connectionService.GetLayerService();
                await layerService.DeleteLayerAsync(SelectedLayer.Name, recurse: false);

                StatusMessage = $"Layer '{SelectedLayer.Name}' deleted successfully";
                SelectedLayer = null;

                // 重新加载图层列表
                await LoadLayersAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to delete layer: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 初始化命令 - 加载工作空间
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadWorkspacesAsync();
        }
    }
}
