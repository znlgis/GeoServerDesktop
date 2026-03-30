using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// GeoWebCache 格网集管理的视图模型
    /// </summary>
    public partial class GridsetsViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>格网集名称列表</summary>
        [ObservableProperty]
        private ObservableCollection<string> _gridsets = new();

        /// <summary>选中的格网集名称</summary>
        [ObservableProperty]
        private string? _selectedGridset;

        /// <summary>是否正在加载</summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>状态消息</summary>
        [ObservableProperty]
        private string _statusMessage = "Ready";

        /// <summary>
        /// 初始化 GridsetsViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public GridsetsViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// 加载格网集列表
        /// </summary>
        [RelayCommand]
        private async Task LoadGridsetsAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = "Not connected to GeoServer";
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading gridsets...";

            try
            {
                var service = _connectionService.GetGridsetService();
                var wrapper = await service.GetGridsetsAsync();
                Gridsets.Clear();

                if (wrapper?.GridSets != null)
                {
                    foreach (var name in wrapper.GridSets)
                    {
                        Gridsets.Add(name);
                    }
                }

                StatusMessage = $"Loaded {Gridsets.Count} gridsets";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load gridsets: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 删除选中的格网集
        /// </summary>
        [RelayCommand]
        private async Task DeleteGridsetAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedGridset))
            {
                StatusMessage = "No gridset selected";
                return;
            }

            var nameToDelete = SelectedGridset;
            IsLoading = true;
            StatusMessage = $"Deleting gridset '{nameToDelete}'...";

            try
            {
                var service = _connectionService.GetGridsetService();
                await service.DeleteGridsetAsync(nameToDelete);
                StatusMessage = $"Gridset '{nameToDelete}' deleted";
                SelectedGridset = null;
                await LoadGridsetsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to delete gridset: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
