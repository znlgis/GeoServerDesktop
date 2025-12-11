using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// 图层组管理的视图模型
    /// </summary>
    public partial class LayerGroupsManagementViewModel : ViewModelBase
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
        /// 图层组列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<LayerGroup> _layerGroups = new();

        /// <summary>
        /// 选中的图层组
        /// </summary>
        [ObservableProperty]
        private LayerGroup? _selectedLayerGroup;

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
        /// 是否显示创建对话框
        /// </summary>
        [ObservableProperty]
        private bool _isCreateDialogVisible;

        /// <summary>
        /// 新图层组名称
        /// </summary>
        [ObservableProperty]
        private string _newLayerGroupName = string.Empty;

        /// <summary>
        /// 新图层组标题
        /// </summary>
        [ObservableProperty]
        private string _newLayerGroupTitle = string.Empty;

        /// <summary>
        /// 初始化 LayerGroupsManagementViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public LayerGroupsManagementViewModel(IGeoServerConnectionService connectionService)
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
                // 触发异步加载操作，错误处理在 LoadLayerGroupsAsync 方法内部
                _ = LoadLayerGroupsAsync();
            }
        }

        /// <summary>
        /// 加载图层组列表
        /// </summary>
        [RelayCommand]
        private async Task LoadLayerGroupsAsync()
        {
            if (string.IsNullOrEmpty(SelectedWorkspace))
            {
                StatusMessage = "No workspace selected";
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading layer groups...";

            try
            {
                var layerGroupService = _connectionService.GetLayerGroupService();
                LayerGroup[] layerGroupArray;

                if (SelectedWorkspace == "All Workspaces")
                {
                    // 加载所有图层组
                    layerGroupArray = await layerGroupService.GetLayerGroupsAsync();
                }
                else
                {
                    // 加载特定工作空间的图层组
                    layerGroupArray = await layerGroupService.GetWorkspaceLayerGroupsAsync(SelectedWorkspace);
                }

                LayerGroups.Clear();
                foreach (var layerGroup in layerGroupArray)
                {
                    LayerGroups.Add(layerGroup);
                }

                StatusMessage = $"Loaded {LayerGroups.Count} layer groups";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load layer groups: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 显示创建图层组对话框
        /// </summary>
        [RelayCommand]
        private void ShowCreateDialog()
        {
            NewLayerGroupName = string.Empty;
            NewLayerGroupTitle = string.Empty;
            IsCreateDialogVisible = true;
        }

        /// <summary>
        /// 取消创建图层组
        /// </summary>
        [RelayCommand]
        private void CancelCreate()
        {
            IsCreateDialogVisible = false;
        }

        /// <summary>
        /// 创建新图层组
        /// </summary>
        [RelayCommand]
        private async Task CreateLayerGroupAsync()
        {
            if (string.IsNullOrWhiteSpace(NewLayerGroupName))
            {
                StatusMessage = "Layer group name is required";
                return;
            }

            IsLoading = true;
            StatusMessage = $"Creating layer group '{NewLayerGroupName}'...";

            try
            {
                var layerGroup = new LayerGroup
                {
                    Name = NewLayerGroupName.Trim(),
                    Title = string.IsNullOrWhiteSpace(NewLayerGroupTitle) ? NewLayerGroupName.Trim() : NewLayerGroupTitle.Trim(),
                    Mode = "SINGLE",
                    Publishables = new PublishableList { Published = new PublishedItem[0] }
                };

                var layerGroupService = _connectionService.GetLayerGroupService();

                if (SelectedWorkspace == "All Workspaces" || string.IsNullOrEmpty(SelectedWorkspace))
                {
                    // 创建全局图层组
                    await layerGroupService.CreateLayerGroupAsync(layerGroup);
                }
                else
                {
                    // 创建工作空间图层组
                    await layerGroupService.CreateWorkspaceLayerGroupAsync(SelectedWorkspace, layerGroup);
                }

                StatusMessage = $"Layer group '{NewLayerGroupName}' created successfully";
                IsCreateDialogVisible = false;

                // 重新加载图层组列表
                await LoadLayerGroupsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to create layer group: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 删除选中的图层组
        /// </summary>
        [RelayCommand]
        private async Task DeleteLayerGroupAsync()
        {
            if (SelectedLayerGroup == null)
            {
                StatusMessage = "No layer group selected";
                return;
            }

            IsLoading = true;
            StatusMessage = $"Deleting layer group '{SelectedLayerGroup.Name}'...";

            try
            {
                var layerGroupService = _connectionService.GetLayerGroupService();
                
                if (SelectedLayerGroup.Workspace != null && !string.IsNullOrEmpty(SelectedLayerGroup.Workspace.Name))
                {
                    // 删除工作空间图层组
                    await layerGroupService.DeleteWorkspaceLayerGroupAsync(
                        SelectedLayerGroup.Workspace.Name, 
                        SelectedLayerGroup.Name);
                }
                else
                {
                    // 删除全局图层组
                    await layerGroupService.DeleteLayerGroupAsync(SelectedLayerGroup.Name);
                }

                StatusMessage = $"Layer group '{SelectedLayerGroup.Name}' deleted successfully";
                SelectedLayerGroup = null;

                // 重新加载图层组列表
                await LoadLayerGroupsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to delete layer group: {ex.Message}";
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
