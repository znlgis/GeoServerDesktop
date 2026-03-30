using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Models;

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
        /// 数据存储名称列表（供发布图层对话框选择）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _dataStoreNames = new();

        /// <summary>
        /// 新建图层名称
        /// </summary>
        [ObservableProperty]
        private string _newLayerName = string.Empty;

        /// <summary>
        /// 新建图层的本地名称（数据库/文件中的实际名称）
        /// </summary>
        [ObservableProperty]
        private string _newNativeName = string.Empty;

        /// <summary>
        /// 新建图层的坐标参考系
        /// </summary>
        [ObservableProperty]
        private string _newLayerSrs = "EPSG:4326";

        /// <summary>
        /// 新建图层选中的数据存储名称
        /// </summary>
        [ObservableProperty]
        private string? _newLayerDataStore;

        /// <summary>
        /// 是否显示发布图层对话框
        /// </summary>
        [ObservableProperty]
        private bool _isCreateLayerDialogVisible;

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
                // 触发异步加载操作，错误处理在 LoadLayersAsync 方法内部
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
                // recurse=false: 仅删除图层本身，不删除关联的资源（如要素类型）
                // 这是为了保护底层数据存储中的数据
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
        /// 显示发布图层对话框
        /// </summary>
        [RelayCommand]
        private async Task ShowCreateLayerDialogAsync()
        {
            if (string.IsNullOrEmpty(SelectedWorkspace) || SelectedWorkspace == "All Workspaces")
            {
                StatusMessage = "Please select a specific workspace first";
                return;
            }

            // 加载该工作空间下的数据存储名称
            try
            {
                var dataStoreService = _connectionService.GetDataStoreService();
                var stores = await dataStoreService.GetDataStoresAsync(SelectedWorkspace);
                DataStoreNames.Clear();
                foreach (var store in stores)
                {
                    DataStoreNames.Add(store.Name);
                }
                NewLayerDataStore = DataStoreNames.Count > 0 ? DataStoreNames[0] : null;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load data stores: {ex.Message}";
                return;
            }

            NewLayerName = string.Empty;
            NewNativeName = string.Empty;
            NewLayerSrs = "EPSG:4326";
            IsCreateLayerDialogVisible = true;
        }

        /// <summary>
        /// 取消发布图层
        /// </summary>
        [RelayCommand]
        private void CancelCreateLayer()
        {
            IsCreateLayerDialogVisible = false;
        }

        /// <summary>
        /// 发布新图层（FeatureType）
        /// </summary>
        [RelayCommand]
        private async Task CreateLayerAsync()
        {
            if (string.IsNullOrWhiteSpace(NewLayerName))
            {
                StatusMessage = "Layer name is required";
                return;
            }

            if (string.IsNullOrEmpty(NewLayerDataStore))
            {
                StatusMessage = "Please select a data store";
                return;
            }

            IsLoading = true;
            StatusMessage = $"Publishing layer '{NewLayerName}'...";

            try
            {
                var featureTypeService = _connectionService.GetFeatureTypeService();
                var featureType = new FeatureType
                {
                    Name = NewLayerName,
                    NativeName = string.IsNullOrWhiteSpace(NewNativeName) ? NewLayerName : NewNativeName,
                    Title = NewLayerName,
                    Srs = string.IsNullOrWhiteSpace(NewLayerSrs) ? "EPSG:4326" : NewLayerSrs,
                    Enabled = true
                };
                await featureTypeService.CreateFeatureTypeAsync(SelectedWorkspace!, NewLayerDataStore, featureType);

                IsCreateLayerDialogVisible = false;
                NewLayerName = string.Empty;
                NewNativeName = string.Empty;
                await LoadLayersAsync();
                StatusMessage = "Layer published successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to publish layer: {ex.Message}";
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
