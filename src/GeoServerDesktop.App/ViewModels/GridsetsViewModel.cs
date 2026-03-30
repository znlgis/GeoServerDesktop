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
        private string _statusMessage = string.Empty;

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
                StatusMessage = L.StatusNotConnected;
                return;
            }

            IsLoading = true;
            StatusMessage = L.StatusLoadingGridsets;

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

                StatusMessage = string.Format(L.StatusGridsetsLoaded, Gridsets.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusGridsetsLoadFailed, ex.Message);
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
                StatusMessage = L.StatusNoGridsetSelected;
                return;
            }

            var nameToDelete = SelectedGridset;
            IsLoading = true;
            StatusMessage = string.Format(L.StatusDeletingGridset, nameToDelete);

            try
            {
                var service = _connectionService.GetGridsetService();
                await service.DeleteGridsetAsync(nameToDelete);
                StatusMessage = string.Format(L.StatusGridsetDeleted, nameToDelete);
                SelectedGridset = null;
                await LoadGridsetsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusGridsetDeleteFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
