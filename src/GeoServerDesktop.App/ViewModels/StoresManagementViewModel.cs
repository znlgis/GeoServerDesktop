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
        /// 新建数据存储名称
        /// </summary>
        [ObservableProperty]
        private string _newDataStoreName = string.Empty;

        /// <summary>
        /// 新建数据存储描述
        /// </summary>
        [ObservableProperty]
        private string _newDataStoreDescription = string.Empty;

        /// <summary>
        /// 是否显示创建对话框
        /// </summary>
        [ObservableProperty]
        private bool _isCreateDialogVisible;

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

                // 自动选择第一个工作空间
                if (Workspaces.Count > 0)
                {
                    SelectedWorkspace = Workspaces[0];
                }
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
                StatusMessage = L.StatusNoWorkspaceSelected;
                return;
            }

            IsLoading = true;
            StatusMessage = L.StatusLoadingDataStores;

            try
            {
                var dataStoreService = _connectionService.GetDataStoreService();
                var stores = await dataStoreService.GetDataStoresAsync(SelectedWorkspace);

                DataStores.Clear();
                foreach (var store in stores)
                {
                    DataStores.Add(store);
                }

                StatusMessage = string.Format(L.StatusDataStoresLoaded, DataStores.Count, SelectedWorkspace);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusDataStoresLoadFailed, ex.Message);
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
                StatusMessage = L.StatusNoDataStoreSelected;
                return;
            }

            IsLoading = true;
            StatusMessage = string.Format(L.StatusDeletingDataStore, SelectedDataStore.Name);

            try
            {
                var dataStoreService = _connectionService.GetDataStoreService();
                await dataStoreService.DeleteDataStoreAsync(SelectedWorkspace, SelectedDataStore.Name, recurse: false);

                StatusMessage = string.Format(L.StatusDataStoreDeleted, SelectedDataStore.Name);
                SelectedDataStore = null;

                // 重新加载数据存储列表
                await LoadDataStoresAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusDataStoreDeleteFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 显示创建数据存储对话框
        /// </summary>
        [RelayCommand]
        private void ShowCreateDialog()
        {
            NewDataStoreName = string.Empty;
            NewDataStoreDescription = string.Empty;
            IsCreateDialogVisible = true;
        }

        /// <summary>
        /// 取消创建数据存储
        /// </summary>
        [RelayCommand]
        private void CancelCreate()
        {
            IsCreateDialogVisible = false;
        }

        /// <summary>
        /// 创建新的 Shapefile 数据存储
        /// </summary>
        [RelayCommand]
        private async Task CreateDataStoreAsync()
        {
            if (string.IsNullOrWhiteSpace(NewDataStoreName))
            {
                StatusMessage = L.StatusDataStoreNameRequired;
                return;
            }

            if (string.IsNullOrEmpty(SelectedWorkspace))
            {
                StatusMessage = L.StatusPleaseSelectWorkspace;
                return;
            }

            IsLoading = true;
            StatusMessage = string.Format(L.StatusCreatingDataStore, NewDataStoreName);

            try
            {
                var dataStoreService = _connectionService.GetDataStoreService();
                var dataStore = new DataStore
                {
                    Name = NewDataStoreName,
                    Enabled = true,
                    ConnectionParameters = new ConnectionParameters
                    {
                        Entries = new[]
                        {
                            new ConnectionParameterEntry { Key = "namespace", Value = SelectedWorkspace },
                        }
                    }
                };
                await dataStoreService.CreateDataStoreAsync(SelectedWorkspace, dataStore);

                IsCreateDialogVisible = false;
                NewDataStoreName = string.Empty;
                NewDataStoreDescription = string.Empty;
                await LoadDataStoresAsync();
                StatusMessage = L.StatusDataStoreCreated;
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusDataStoreCreateFailed, ex.Message);
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
