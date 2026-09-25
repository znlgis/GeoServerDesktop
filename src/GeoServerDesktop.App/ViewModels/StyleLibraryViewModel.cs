using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// 样式库视图模型（M3）：绑定总览（每个全局样式被哪些图层引用为默认样式）+
    /// 未引用样式筛选与清理（引用中的样式禁止删除，服务端 403 兜底）。
    /// </summary>
    public partial class StyleLibraryViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>全量条目缓存（用于筛选重建显示集合）</summary>
        private List<StyleUsageItem> _allUsages = new();

        /// <summary>显示条目（受"仅显示未引用"筛选影响）</summary>
        [ObservableProperty]
        private ObservableCollection<StyleUsageItem> _usages = new();

        /// <summary>选中条目</summary>
        [ObservableProperty]
        private StyleUsageItem? _selectedUsage;

        /// <summary>是否仅显示未引用样式</summary>
        [ObservableProperty]
        private bool _showUnusedOnly;

        /// <summary>状态消息</summary>

        /// <summary>是否正在加载</summary>

        /// <summary>
        /// 初始化 StyleLibraryViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public StyleLibraryViewModel(IGeoServerConnectionService connectionService)
        : base(connectionService)
        {
            _connectionService = connectionService;
            StatusMessage = L.StyleLibReady;
        }

        partial void OnShowUnusedOnlyChanged(bool value) => ApplyFilter();

        /// <summary>
        /// 加载样式使用总览
        /// </summary>
        [RelayCommand]
        private async Task LoadUsagesAsync()
        {
            IsLoading = true;
            try
            {
                await RefreshUsagesAsync();
                var unused = _allUsages.Count(u => !u.IsUsed);
                StatusMessage = string.Format(L.StyleLibStatusLoaded, _allUsages.Count, unused);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StyleLibStatusLoadFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 删除选中样式（仅允许未引用样式；引用中的由服务端 403 兜底）
        /// </summary>
        [RelayCommand]
        private async Task DeleteSelectedAsync()
        {
            if (SelectedUsage == null)
            {
                StatusMessage = L.StyleLibStatusSelectFirst;
                return;
            }
            if (SelectedUsage.IsUsed)
            {
                StatusMessage = L.StyleLibStatusDeleteInUse;
                return;
            }

            IsLoading = true;
            try
            {
                var name = SelectedUsage.StyleName;
                await _connectionService.GetStyleService().DeleteStyleAsync(name, purge: true);
                await RefreshUsagesAsync();
                StatusMessage = string.Format(L.StyleLibStatusDeleted, name);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StyleLibStatusDeleteFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>重新聚合使用关系并刷新显示（不改变状态消息）。</summary>
        private async Task RefreshUsagesAsync()
        {
            var usages = await _connectionService.GetStyleUsageService().GetStyleUsageAsync();
            _allUsages = usages
                .Select(u => new StyleUsageItem(u.StyleName, u.IsUsed, u.Layers))
                .ToList();
            ApplyFilter();
        }

        /// <summary>按"仅显示未引用"筛选重建显示集合。</summary>
        private void ApplyFilter()
        {
            Usages.Clear();
            foreach (var u in _allUsages.Where(u => !ShowUnusedOnly || !u.IsUsed))
            {
                Usages.Add(u);
            }
        }
    }
}
