using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.GeoServerClient.Migration;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// 设置同步视图模型（M4）：当前连接（目标实例）与另一连接（源实例）之间的设置差异比对与选择性应用。
    /// 读两侧同域设置 → 叶子级差异（排除 id/updateSequence 等 volatile 键）→ 勾选路径合并回目标整包 PUT
    /// （未勾选键原样保留，与库层"整包替换 + ExtensionData 防丢键"契约一致）。
    /// </summary>
    public partial class SettingsSyncViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>可选域</summary>
        public static string[] Domains { get; } =
        {
            nameof(SettingsDomain.Global), nameof(SettingsDomain.Wms),
            nameof(SettingsDomain.Wfs), nameof(SettingsDomain.Wcs),
            nameof(SettingsDomain.Wmts),
        };

        /// <summary>选中的域</summary>
        [ObservableProperty]
        private string _domain = nameof(SettingsDomain.Global);

        /// <summary>源实例 URL</summary>
        [ObservableProperty]
        private string _sourceUrl = string.Empty;

        /// <summary>源实例用户</summary>
        [ObservableProperty]
        private string _sourceUser = "admin";

        /// <summary>源实例密码</summary>
        [ObservableProperty]
        private string _sourcePassword = string.Empty;

        /// <summary>差异条目（含勾选）</summary>
        [ObservableProperty]
        private ObservableCollection<SettingsDiffEntry> _diffs = new();

        /// <summary>状态消息</summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>是否正在加载</summary>
        [ObservableProperty]
        private bool _isLoading;

        private SettingsDiffResult? _lastDiff;

        /// <summary>
        /// 初始化 SettingsSyncViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务（当前连接即目标实例）</param>
        public SettingsSyncViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
            StatusMessage = L.SyncReady;
        }

        /// <summary>读取两侧设置并比对。</summary>
        [RelayCommand]
        private async Task CompareAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = L.StatusPleaseConnect;
                return;
            }
            if (string.IsNullOrWhiteSpace(SourceUrl))
            {
                StatusMessage = L.SyncStatusNeedUrl;
                return;
            }
            IsLoading = true;
            try
            {
                var domain = (SettingsDomain)Enum.Parse(typeof(SettingsDomain), Domain, ignoreCase: true);
                var options = new GeoServerClientOptions
                {
                    BaseUrl = SourceUrl.Trim(),
                    Username = SourceUser,
                    Password = SourcePassword,
                };
                using var factory = new GeoServerClientFactory(options);
                var source = factory.CreateSettingsCompareService();
                var target = _connectionService.GetSettingsCompareService();

                var diff = await SettingsCompareService.CompareAsync(source, target, domain);
                _lastDiff = diff;
                Diffs = new ObservableCollection<SettingsDiffEntry>(
                    diff.Items.Select(i => new SettingsDiffEntry(i)));
                StatusMessage = diff.IsIdentical
                    ? L.SyncStatusIdentical
                    : string.Format(L.SyncStatusLoaded, diff.Items.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.SyncStatusFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>把勾选的差异从源侧应用到当前（目标）实例。</summary>
        [RelayCommand]
        private async Task ApplySelectedAsync()
        {
            if (_lastDiff == null)
            {
                StatusMessage = L.SyncStatusCompareFirst;
                return;
            }
            var paths = Diffs.Where(d => d.IsSelected).Select(d => d.Path).ToList();
            if (paths.Count == 0)
            {
                StatusMessage = L.SyncStatusNeedSelection;
                return;
            }
            IsLoading = true;
            try
            {
                var domain = (SettingsDomain)Enum.Parse(typeof(SettingsDomain), Domain, ignoreCase: true);
                var target = _connectionService.GetSettingsCompareService();
                await target.ApplyAsync(_lastDiff.SourceJson, domain, paths);

                // 应用后重比对（验证收敛）
                await CompareAsync();
                StatusMessage = string.Format(L.SyncStatusApplied, paths.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.SyncStatusFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    /// <summary>差异行展示包装（含勾选）。</summary>
    public sealed class SettingsDiffEntry : ViewModelBase
    {
        private bool _isSelected = true;

        /// <summary>差异路径</summary>
        public string Path { get; }

        /// <summary>源值（JSON 字面量）</summary>
        public string SourceValue { get; }

        /// <summary>目标值（JSON 字面量）</summary>
        public string TargetValue { get; }

        /// <summary>差异类型显示</summary>
        public string KindDisplay { get; }

        /// <summary>是否应用</summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }

        /// <summary>
        /// 初始化 SettingsDiffEntry 类的新实例
        /// </summary>
        public SettingsDiffEntry(SettingsDiffItem item)
        {
            Path = item.Path;
            SourceValue = item.SourceValue ?? "—";
            TargetValue = item.TargetValue ?? "—";
            KindDisplay = Describe(item.Kind);
        }

        private static string Describe(SettingsDiffKind kind)
        {
            switch (kind)
            {
                case SettingsDiffKind.AddedInSource: return LocalizationService.Instance.SyncKindAdded;
                case SettingsDiffKind.RemovedInSource: return LocalizationService.Instance.SyncKindRemoved;
                default: return LocalizationService.Instance.SyncKindChanged;
            }
        }
    }
}
