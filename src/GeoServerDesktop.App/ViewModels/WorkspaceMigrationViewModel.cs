using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Migration;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// 工作空间迁移视图模型（M4）：导出（REST 读全量资源 → ZIP 归档：manifest.json + styles/*.sld）
    /// 与导入（目标实例按依赖顺序重建：ws → namespace → store → style → 资源 → 图层绑定 → 图层组）。
    /// 幂等：已存在资源默认跳过，勾选覆盖时更新。文件选择经 FilePickerProvider 注入
    /// （UI 层挂接 Avalonia StorageProvider；无头测试不设置该委托即跳过对话框）。
    /// </summary>
    public partial class WorkspaceMigrationViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>可选：保存文件对话框（描述, 默认文件名）→ 目标路径（取消返回 null）</summary>
        public Func<string, string, Task<string?>>? SaveFilePicker { get; set; }

        /// <summary>可选：打开文件对话框（描述, 过滤器）→ 选中路径（取消返回 null）</summary>
        public Func<string, Task<string?>>? OpenFilePicker { get; set; }

        /// <summary>工作空间列表</summary>
        [ObservableProperty]
        private ObservableCollection<string> _workspaces = new();

        /// <summary>选中的待导出工作空间</summary>
        [ObservableProperty]
        private string? _selectedWorkspace;

        /// <summary>导出状态说明</summary>
        [ObservableProperty]
        private string _exportStatus = string.Empty;

        /// <summary>最近导出的归档字节（供保存）</summary>
        private byte[]? _lastExport;

        /// <summary>导入归档路径</summary>
        [ObservableProperty]
        private string _archivePath = string.Empty;

        /// <summary>目标工作空间名（留空沿用源名）</summary>
        [ObservableProperty]
        private string _targetWorkspace = string.Empty;

        /// <summary>目标命名空间前缀（留空同目标工作空间）</summary>
        [ObservableProperty]
        private string _targetNamespacePrefix = string.Empty;

        /// <summary>目标命名空间 URI（留空沿用源 URI）</summary>
        [ObservableProperty]
        private string _targetNamespaceUri = string.Empty;

        /// <summary>已存在资源是否覆盖</summary>
        [ObservableProperty]
        private bool _overwriteExisting;

        /// <summary>导入结果明细</summary>
        [ObservableProperty]
        private ObservableCollection<MigrationStepItem> _importResults = new();

        /// <summary>状态消息</summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>是否正在加载</summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// 初始化 WorkspaceMigrationViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public WorkspaceMigrationViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
            StatusMessage = L.MigReady;
        }

        /// <summary>加载工作空间列表。</summary>
        [RelayCommand]
        private async Task LoadAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = L.StatusPleaseConnect;
                return;
            }
            IsLoading = true;
            try
            {
                var all = await _connectionService.GetWorkspaceService().GetWorkspacesAsync();
                Workspaces = new ObservableCollection<string>(
                    all.Where(w => w != null && !string.IsNullOrEmpty(w.Name)).Select(w => w.Name)
                        .OrderBy(n => n, StringComparer.Ordinal));
                StatusMessage = string.Format(L.MigStatusLoaded, Workspaces.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.MigStatusFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>导出选中工作空间为归档。</summary>
        [RelayCommand]
        private async Task ExportAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = L.StatusPleaseConnect;
                return;
            }
            if (string.IsNullOrEmpty(SelectedWorkspace))
            {
                ExportStatus = L.MigStatusNeedWorkspace;
                return;
            }
            IsLoading = true;
            try
            {
                var svc = _connectionService.GetWorkspaceMigrationService();
                var result = await svc.ExportWorkspaceAsync(SelectedWorkspace);
                _lastExport = result.Archive;
                ExportStatus = string.Format(L.MigExportDone,
                    result.Workspace, result.ResourceCount,
                    result.Warnings.Count, result.Archive.Length);
                StatusMessage = ExportStatus;
            }
            catch (Exception ex)
            {
                ExportStatus = string.Format(L.MigStatusFailed, ex.Message);
                StatusMessage = ExportStatus;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>把最近导出的归档写到本地文件（经文件对话框选路径）。</summary>
        [RelayCommand]
        private async Task SaveArchiveAsync()
        {
            if (_lastExport == null || _lastExport.Length == 0)
            {
                ExportStatus = L.MigStatusExportFirst;
                return;
            }
            var path = SaveFilePicker != null
                ? await SaveFilePicker("GeoServer workspace archive",
                    (SelectedWorkspace ?? "workspace") + ".gdws.zip")
                : null;
            if (string.IsNullOrEmpty(path))
            {
                ExportStatus = L.MigStatusNoPath;
                return;
            }
            try
            {
                File.WriteAllBytes(path!, _lastExport);
                ExportStatus = string.Format(L.MigSaved, path);
            }
            catch (Exception ex)
            {
                ExportStatus = string.Format(L.MigStatusFailed, ex.Message);
            }
        }

        /// <summary>浏览选择归档文件。</summary>
        [RelayCommand]
        private async Task BrowseArchiveAsync()
        {
            var path = OpenFilePicker != null ? await OpenFilePicker("GeoServer workspace archive (*.gdws.zip;*.zip)") : null;
            if (!string.IsNullOrEmpty(path)) ArchivePath = path!;
        }

        /// <summary>导入归档到当前实例。</summary>
        [RelayCommand]
        private async Task ImportAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = L.StatusPleaseConnect;
                return;
            }
            if (string.IsNullOrWhiteSpace(ArchivePath) || !File.Exists(ArchivePath))
            {
                StatusMessage = L.MigStatusNeedArchive;
                return;
            }
            IsLoading = true;
            try
            {
                var svc = _connectionService.GetWorkspaceMigrationService();
                var result = await svc.ImportWorkspaceAsync(new WorkspaceImportRequest
                {
                    ArchivePath = ArchivePath.Trim(),
                    TargetWorkspace = NullIfBlank(TargetWorkspace),
                    TargetNamespacePrefix = NullIfBlank(TargetNamespacePrefix),
                    TargetNamespaceUri = NullIfBlank(TargetNamespaceUri),
                    OverwriteExisting = OverwriteExisting,
                });
                ImportResults = new ObservableCollection<MigrationStepItem>(
                    result.Items.Select(i => new MigrationStepItem(i)));
                var created = result.Items.Count(i => i.Status != MigrationItemStatus.Skipped && !i.IsFailure);
                StatusMessage = result.Success
                    ? string.Format(L.MigImportDone, created, result.SkippedCount)
                    : string.Format(L.MigImportPartial, result.FailedCount, result.FailureSummary);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.MigStatusFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static string? NullIfBlank(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }

    /// <summary>导入结果行展示包装。</summary>
    public sealed class MigrationStepItem : ViewModelBase
    {
        /// <summary>步骤（workspace/namespace/dataStore/...）</summary>
        public string Step { get; }

        /// <summary>目标展示名</summary>
        public string Target { get; }

        /// <summary>状态显示文本</summary>
        public string StatusDisplay { get; }

        /// <summary>说明</summary>
        public string Message { get; }

        /// <summary>
        /// 初始化 MigrationStepItem 类的新实例
        /// </summary>
        public MigrationStepItem(MigrationItemResult item)
        {
            Step = item.Step;
            Target = item.Target;
            StatusDisplay = DescribeStatus(item.Status);
            Message = item.Message ?? string.Empty;
        }

        private static string DescribeStatus(MigrationItemStatus status)
        {
            // 静态上下文：不能复用基类实例属性 L，直接取本地化单例
            var l = LocalizationService.Instance;
            switch (status)
            {
                case MigrationItemStatus.Created: return l.MigStatusCreated;
                case MigrationItemStatus.Updated: return l.MigStatusUpdated;
                case MigrationItemStatus.Skipped: return l.MigStatusSkipped;
                default: return l.MigStatusFailedItem;
            }
        }

    }
}
