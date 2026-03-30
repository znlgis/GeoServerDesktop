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
        private string _statusMessage = string.Empty;

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
                StatusMessage = L.StatusNotConnected;
                return;
            }

            IsLoading = true;
            StatusMessage = L.StatusLoadingWorkspaces;

            try
            {
                var workspaceService = _connectionService.GetWorkspaceService();
                var workspaceList = await workspaceService.GetWorkspacesAsync();

                Workspaces.Clear();
                foreach (var workspace in workspaceList)
                {
                    Workspaces.Add(workspace.Name);
                }

                StatusMessage = string.Format(L.StatusWorkspacesLoaded, Workspaces.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusWorkspacesLoadFailed, ex.Message);
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
                StatusMessage = L.StatusWorkspaceNameRequired;
                return;
            }

            IsLoading = true;
            StatusMessage = string.Format(L.StatusCreatingWorkspace, NewWorkspaceName);

            try
            {
                var workspaceService = _connectionService.GetWorkspaceService();
                await workspaceService.CreateWorkspaceAsync(NewWorkspaceName);

                StatusMessage = string.Format(L.StatusWorkspaceCreated, NewWorkspaceName);
                NewWorkspaceName = string.Empty;

                // 重新加载工作空间
                await LoadWorkspacesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusWorkspaceCreateFailed, ex.Message);
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
                StatusMessage = L.StatusNoWorkspaceSelected;
                return;
            }

            IsLoading = true;
            StatusMessage = string.Format(L.StatusDeletingWorkspace, SelectedWorkspace);

            try
            {
                var workspaceService = _connectionService.GetWorkspaceService();
                await workspaceService.DeleteWorkspaceAsync(SelectedWorkspace, recurse: false);

                StatusMessage = string.Format(L.StatusWorkspaceDeleted, SelectedWorkspace);
                SelectedWorkspace = null;

                // 重新加载工作空间
                await LoadWorkspacesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusWorkspaceDeleteFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
