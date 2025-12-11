using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// 工作空间管理的视图模型
    /// </summary>
    public partial class WorkspaceManagementViewModel : ViewModelBase
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
        /// 新工作空间名称
        /// </summary>
        [ObservableProperty]
        private string _newWorkspaceName = string.Empty;

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
        /// 初始化 WorkspaceManagementViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public WorkspaceManagementViewModel(IGeoServerConnectionService connectionService)
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
                foreach (var workspace in workspaceList)
                {
                    Workspaces.Add(workspace.Name);
                }

                StatusMessage = $"Loaded {Workspaces.Count} workspaces";
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
        /// 创建新的工作空间
        /// </summary>
        [RelayCommand]
        private async Task CreateWorkspaceAsync()
        {
            if (string.IsNullOrWhiteSpace(NewWorkspaceName))
            {
                StatusMessage = "Workspace name is required";
                return;
            }

            IsLoading = true;
            StatusMessage = $"Creating workspace '{NewWorkspaceName}'...";

            try
            {
                var workspaceService = _connectionService.GetWorkspaceService();
                await workspaceService.CreateWorkspaceAsync(NewWorkspaceName);

                StatusMessage = $"Workspace '{NewWorkspaceName}' created successfully";
                NewWorkspaceName = string.Empty;

                // 重新加载工作空间
                await LoadWorkspacesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to create workspace: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 删除选中的工作空间
        /// </summary>
        [RelayCommand]
        private async Task DeleteWorkspaceAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedWorkspace))
            {
                StatusMessage = "No workspace selected";
                return;
            }

            IsLoading = true;
            StatusMessage = $"Deleting workspace '{SelectedWorkspace}'...";

            try
            {
                var workspaceService = _connectionService.GetWorkspaceService();
                await workspaceService.DeleteWorkspaceAsync(SelectedWorkspace, recurse: false);

                StatusMessage = $"Workspace '{SelectedWorkspace}' deleted successfully";
                SelectedWorkspace = null;

                // 重新加载工作空间
                await LoadWorkspacesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to delete workspace: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
