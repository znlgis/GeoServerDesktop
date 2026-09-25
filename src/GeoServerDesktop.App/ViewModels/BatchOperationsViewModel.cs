using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// 批量操作视图模型（M4）：图层/样式/存储/工作空间多选列表 + 批量改默认样式、
    /// 批量启用/禁用存储、批量删除（级联选项）。
    /// 实测基线（GeoServer 3.0.1）：图层（LayerInfo）REST 无 enabled 通道，启停在存储级
    /// （禁用存储后其下图层从 WMS/WFS 消失）；引用中的样式删除返回 403，按失败项汇总上报。
    /// </summary>
    public partial class BatchOperationsViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>可选资源类型</summary>
        public static string[] Scopes { get; } =
        {
            nameof(BatchScope.Layer), nameof(BatchScope.Style),
            nameof(BatchScope.Store), nameof(BatchScope.Workspace),
        };

        /// <summary>工作空间列表（含"全部"空选项）</summary>
        [ObservableProperty]
        private ObservableCollection<string> _workspaces = new();

        /// <summary>筛选用的工作空间（空=全部）</summary>
        [ObservableProperty]
        private string? _selectedWorkspace;

        /// <summary>当前资源类型（Layer/Style/Store/Workspace）</summary>
        [ObservableProperty]
        private string _scope = nameof(BatchScope.Layer);

        /// <summary>条目列表（多选）</summary>
        [ObservableProperty]
        private ObservableCollection<BatchItem> _items = new();

        /// <summary>批量样式目标候选（全局样式 + 当前工作空间样式）</summary>
        [ObservableProperty]
        private ObservableCollection<string> _styleNames = new();

        /// <summary>选中的新默认样式</summary>
        [ObservableProperty]
        private string? _selectedStyleName;

        /// <summary>删除是否级联（recurse/purge）</summary>
        [ObservableProperty]
        private bool _cascadeDelete = true;

        /// <summary>状态消息</summary>

        /// <summary>是否正在加载</summary>

        /// <summary>
        /// 初始化 BatchOperationsViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public BatchOperationsViewModel(IGeoServerConnectionService connectionService)
        : base(connectionService)
        {
            _connectionService = connectionService;
            StatusMessage = L.BatchReady;
        }

        partial void OnScopeChanged(string value) => _ = LoadItemsCommand.ExecuteAsync(null);

        partial void OnSelectedWorkspaceChanged(string? value)
        {
            if (!_suppressWorkspaceReload) _ = LoadItemsCommand.ExecuteAsync(null);
        }

        private bool _suppressWorkspaceReload;

        /// <summary>加载工作空间与当前类型条目列表。</summary>
        [RelayCommand]
        private async Task LoadItemsAsync()
        {
            if (!HasConnection()) return;
            IsLoading = true;
            try
            {
                await LoadWorkspacesAsync();
                await LoadScopeItemsAsync();
                await LoadStyleCandidatesAsync();
                var selected = Items.Count(i => i.IsSelected);
                StatusMessage = string.Format(L.BatchStatusLoaded, Items.Count, selected);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.BatchStatusFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>全选/全不选切换。</summary>
        [RelayCommand]
        private void ToggleSelectAll()
        {
            var target = Items.Any(i => !i.IsSelected);
            foreach (var item in Items) item.IsSelected = target;
        }

        /// <summary>批量设置选中图层的默认样式。</summary>
        [RelayCommand]
        private async Task ApplyDefaultStyleAsync()
        {
            var names = SelectedDisplay;
            if (Scope != nameof(BatchScope.Layer) || names.Count == 0)
            {
                StatusMessage = L.BatchStatusNeedLayers;
                return;
            }
            if (string.IsNullOrEmpty(SelectedStyleName))
            {
                StatusMessage = L.BatchStatusNeedStyle;
                return;
            }
            await RunBatch(async () => await _connectionService.GetBatchOperationService()
                .SetLayersDefaultStyleAsync(names, SelectedStyleName!));
        }

        /// <summary>批量启用选中存储。</summary>
        [RelayCommand]
        private Task EnableSelectedAsync() => SetStoresEnabledAsync(true);

        /// <summary>批量禁用选中存储。</summary>
        [RelayCommand]
        private Task DisableSelectedAsync() => SetStoresEnabledAsync(false);

        /// <summary>批量删除选中项（图层/样式/工作空间；级联选项）。</summary>
        [RelayCommand]
        private async Task DeleteSelectedAsync()
        {
            var items = Items.Where(i => i.IsSelected).ToList();
            if (items.Count == 0)
            {
                StatusMessage = L.BatchStatusNeedSelection;
                return;
            }
            switch (Scope)
            {
                case nameof(BatchScope.Layer):
                    await RunBatch(async () => await _connectionService.GetBatchOperationService()
                        .DeleteLayersAsync(items.Select(i => i.QualifiedLayer).ToList(), CascadeDelete));
                    break;
                case nameof(BatchScope.Style):
                    await RunBatch(async () => await _connectionService.GetBatchOperationService()
                        .DeleteStylesAsync(items.Select(i => i.ToStyleTarget()).ToList(), CascadeDelete));
                    break;
                case nameof(BatchScope.Workspace):
                    await RunBatch(async () => await _connectionService.GetBatchOperationService()
                        .DeleteWorkspacesAsync(items.Select(i => i.Name).ToList(), CascadeDelete));
                    await LoadItemsAsync(); // 工作空间列表须整体刷新（含条目与筛选下拉）
                    return;
                default:
                    StatusMessage = L.BatchStatusDeleteUnsupported;
                    return;
            }
            await LoadScopeItemsAsync();
        }

        private async Task SetStoresEnabledAsync(bool enabled)
        {
            var targets = SelectedTargets;
            if (Scope != nameof(BatchScope.Store) || targets.Count == 0)
            {
                StatusMessage = L.BatchStatusNeedStores;
                return;
            }
            await RunBatch(async () => await _connectionService.GetBatchOperationService()
                .SetStoresEnabledAsync(targets, enabled));
        }

        private List<string> SelectedDisplay
        {
            get { return Items.Where(i => i.IsSelected).Select(i => i.QualifiedLayer).ToList(); }
        }

        private List<BatchTarget> SelectedTargets
        {
            get { return Items.Where(i => i.IsSelected).Select(i => i.ToStoreTarget()).ToList(); }
        }

        private Task RunBatch(Func<Task<BatchResult>> op) => RunGuardedAsync(
            async () =>
            {
                var result = await op();
                StatusMessage = result.Failed == 0
                    ? string.Format(L.BatchStatusAllOk, result.Succeeded)
                    : string.Format(L.BatchStatusPartial, result.Succeeded, result.Failed, result.FailureSummary);
            },
            ex => string.Format(L.BatchStatusFailed, ex.Message));

        private async Task LoadWorkspacesAsync()
        {
            var all = await _connectionService.GetWorkspaceService().GetWorkspacesAsync();
            var previous = SelectedWorkspace;
            _suppressWorkspaceReload = true;
            Workspaces = new ObservableCollection<string>(all.Select(w => w.Name).OrderBy(n => n, StringComparer.Ordinal));
            SelectedWorkspace = Workspaces.Contains(previous ?? string.Empty) || previous == null ? previous : null;
            _suppressWorkspaceReload = false;
        }

        private async Task LoadScopeItemsAsync()
        {
            var wsFilter = SelectedWorkspace;
            var list = new List<BatchItem>();
            switch (Scope)
            {
                case nameof(BatchScope.Layer):
                    {
                        var layers = string.IsNullOrEmpty(wsFilter)
                            ? await _connectionService.GetLayerService().GetLayersAsync()
                            : await _connectionService.GetLayerService().GetWorkspaceLayersAsync(wsFilter);
                        foreach (var l in layers)
                        {
                            if (l == null || string.IsNullOrEmpty(l.Name)) continue;
                            var display = l.Name.Contains(":") ? l.Name : (wsFilter ?? "") + ":" + l.Name;
                            var ws = display.Contains(":") ? display.Substring(0, display.IndexOf(':')) : display;
                            list.Add(new BatchItem(display, ws, display.Substring(display.IndexOf(':') + 1)));
                        }
                        break;
                    }
                case nameof(BatchScope.Style):
                    {
                        if (string.IsNullOrEmpty(wsFilter))
                        {
                            var styles = await _connectionService.GetStyleService().GetStylesAsync();
                            foreach (var s in styles)
                            {
                                if (s == null || string.IsNullOrEmpty(s.Name)) continue;
                                list.Add(new BatchItem(s.Name, null!, s.Name));
                            }
                        }
                        else
                        {
                            var styles = await _connectionService.GetStyleService().GetWorkspaceStylesAsync(wsFilter);
                            foreach (var s in styles)
                            {
                                if (s == null || string.IsNullOrEmpty(s.Name)) continue;
                                list.Add(new BatchItem(wsFilter + "/" + s.Name, wsFilter, s.Name, isWorkspaceStyle: true));
                            }
                        }
                        break;
                    }
                case nameof(BatchScope.Store):
                    {
                        var targets = string.IsNullOrEmpty(wsFilter)
                            ? Workspaces.ToList()
                            : new List<string> { wsFilter };
                        foreach (var ws in targets)
                        {
                            foreach (var d in await _connectionService.GetDataStoreService().GetDataStoresAsync(ws))
                            {
                                if (d == null || string.IsNullOrEmpty(d.Name)) continue;
                                list.Add(new BatchItem(ws + ":" + d.Name, ws, d.Name));
                            }
                            foreach (var c in await _connectionService.GetCoverageStoreService().GetCoverageStoresAsync(ws))
                            {
                                if (c == null || string.IsNullOrEmpty(c.Name)) continue;
                                list.Add(new BatchItem(ws + ":" + c.Name, ws, c.Name));
                            }
                        }
                        break;
                    }
                case nameof(BatchScope.Workspace):
                    {
                        foreach (var w in Workspaces)
                        {
                            list.Add(new BatchItem(w, w, w));
                        }
                        break;
                    }
            }
            Items = new ObservableCollection<BatchItem>(list.OrderBy(i => i.Display, StringComparer.Ordinal));
        }

        private async Task LoadStyleCandidatesAsync()
        {
            var names = new List<string>();
            try
            {
                var styles = await _connectionService.GetStyleService().GetStylesAsync();
                names.AddRange(styles.Where(s => s != null && !string.IsNullOrEmpty(s.Name)).Select(s => s.Name));
            }
            catch
            {
                // 候选加载失败不阻断页面
            }
            StyleNames = new ObservableCollection<string>(names.OrderBy(n => n, StringComparer.Ordinal));
        }
    }

    /// <summary>批量操作资源类型。</summary>
    public enum BatchScope
    {
        /// <summary>图层</summary>
        Layer,

        /// <summary>样式</summary>
        Style,

        /// <summary>存储（数据/覆盖）</summary>
        Store,

        /// <summary>工作空间</summary>
        Workspace,
    }
}
