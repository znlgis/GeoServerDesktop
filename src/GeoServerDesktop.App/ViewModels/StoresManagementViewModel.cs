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
    /// 数据存储管理的视图模型
    /// </summary>
    public partial class StoresManagementViewModel : ViewModelBase
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
        /// 数据存储列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<DataStore> _dataStores = new();

        /// <summary>
        /// 选中的数据存储
        /// </summary>
        [ObservableProperty]
        private DataStore? _selectedDataStore;

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
        /// 初始化 StoresManagementViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public StoresManagementViewModel(IGeoServerConnectionService connectionService)
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

                // 自动选择第一个工作空间
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
                _ = LoadDataStoresAsync();
            }
        }

        /// <summary>
        /// 加载数据存储列表
        /// </summary>
        [RelayCommand]
        private async Task LoadDataStoresAsync()
        {
            if (string.IsNullOrEmpty(SelectedWorkspace))
            {
                StatusMessage = "No workspace selected";
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading data stores...";

            try
            {
                var dataStoreService = _connectionService.GetDataStoreService();
                var stores = await dataStoreService.GetDataStoresAsync(SelectedWorkspace);

                DataStores.Clear();
                foreach (var store in stores)
                {
                    DataStores.Add(store);
                }

                StatusMessage = $"Loaded {DataStores.Count} data stores for workspace '{SelectedWorkspace}'";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load data stores: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 删除选中的数据存储
        /// </summary>
        [RelayCommand]
        private async Task DeleteDataStoreAsync()
        {
            if (SelectedDataStore == null || string.IsNullOrEmpty(SelectedWorkspace))
            {
                StatusMessage = "No data store selected";
                return;
            }

            IsLoading = true;
            StatusMessage = $"Deleting data store '{SelectedDataStore.Name}'...";

            try
            {
                var dataStoreService = _connectionService.GetDataStoreService();
                await dataStoreService.DeleteDataStoreAsync(SelectedWorkspace, SelectedDataStore.Name, recurse: false);

                StatusMessage = $"Data store '{SelectedDataStore.Name}' deleted successfully";
                SelectedDataStore = null;

                // 重新加载数据存储列表
                await LoadDataStoresAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to delete data store: {ex.Message}";
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
