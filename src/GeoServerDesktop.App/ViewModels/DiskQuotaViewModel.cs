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
    /// GeoWebCache 磁盘配额配置的视图模型
    /// </summary>
    public partial class DiskQuotaViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>是否启用磁盘配额</summary>
        [ObservableProperty]
        private bool _isEnabled;

        /// <summary>磁盘块大小（字节）</summary>
        [ObservableProperty]
        private int _diskBlockSize;

        /// <summary>缓存清理频率</summary>
        [ObservableProperty]
        private int _cacheCleanUpFrequency;

        /// <summary>缓存清理单位</summary>
        [ObservableProperty]
        private string _cacheCleanUpUnits = string.Empty;

        /// <summary>全局配额数值</summary>
        [ObservableProperty]
        private double _globalQuotaValue;

        /// <summary>全局配额单位</summary>
        [ObservableProperty]
        private string _globalQuotaUnits = "GiB";

        /// <summary>可用的配额单位选项</summary>
        public ObservableCollection<string> QuotaUnitOptions { get; } = new()
        {
            "B", "KiB", "MiB", "GiB", "TiB"
        };

        /// <summary>可用的清理频率单位选项</summary>
        public ObservableCollection<string> CleanUpUnitOptions { get; } = new()
        {
            "SECONDS", "MINUTES", "HOURS", "DAYS"
        };

        /// <summary>是否正在加载</summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>状态消息</summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>
        /// 初始化 DiskQuotaViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public DiskQuotaViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// 加载磁盘配额设置
        /// </summary>
        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = L.StatusNotConnected;
                return;
            }

            IsLoading = true;
            StatusMessage = L.StatusLoadingDiskQuota;

            try
            {
                var service = _connectionService.GetDiskQuotaService();
                var config = await service.GetDiskQuotaAsync();

                if (config != null)
                {
                    IsEnabled = config.Enabled ?? false;
                    DiskBlockSize = config.DiskBlockSize ?? 0;
                    CacheCleanUpFrequency = config.CacheCleanUpFrequency ?? 0;
                    CacheCleanUpUnits = config.CacheCleanUpUnits ?? "SECONDS";

                    if (config.GlobalQuota != null)
                    {
                        GlobalQuotaValue = config.GlobalQuota.Value;
                        GlobalQuotaUnits = config.GlobalQuota.Units ?? "GiB";
                    }
                }

                StatusMessage = L.StatusDiskQuotaLoaded;
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusDiskQuotaLoadFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 保存磁盘配额设置
        /// </summary>
        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = L.StatusNotConnected;
                return;
            }

            IsLoading = true;
            StatusMessage = L.StatusSavingDiskQuota;

            try
            {
                var service = _connectionService.GetDiskQuotaService();
                var config = new DiskQuotaConfig
                {
                    Enabled = IsEnabled,
                    DiskBlockSize = DiskBlockSize,
                    CacheCleanUpFrequency = CacheCleanUpFrequency,
                    CacheCleanUpUnits = CacheCleanUpUnits,
                    GlobalQuota = new Quota
                    {
                        Value = GlobalQuotaValue,
                        Units = GlobalQuotaUnits
                    }
                };

                await service.UpdateDiskQuotaAsync(config);
                StatusMessage = L.StatusDiskQuotaSaved;
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusDiskQuotaSaveFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
