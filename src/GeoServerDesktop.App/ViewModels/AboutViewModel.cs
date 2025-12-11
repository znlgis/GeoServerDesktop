using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Models;
using System;
using System.Threading.Tasks;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// 关于和系统状态的视图模型
    /// </summary>
    public partial class AboutViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>
        /// GeoServer 版本
        /// </summary>
        [ObservableProperty]
        private string _geoServerVersion = "Loading...";

        /// <summary>
        /// GeoTools 版本
        /// </summary>
        [ObservableProperty]
        private string _geoToolsVersion = "Loading...";

        /// <summary>
        /// GeoWebCache 版本
        /// </summary>
        [ObservableProperty]
        private string _geoWebCacheVersion = "Loading...";

        /// <summary>
        /// JVM 版本
        /// </summary>
        [ObservableProperty]
        private string _jvmVersion = "Loading...";

        /// <summary>
        /// JVM 供应商
        /// </summary>
        [ObservableProperty]
        private string _jvmVendor = "Loading...";

        /// <summary>
        /// 总内存（MB）
        /// </summary>
        [ObservableProperty]
        private string _totalMemory = "Loading...";

        /// <summary>
        /// 已使用内存（MB）
        /// </summary>
        [ObservableProperty]
        private string _usedMemory = "Loading...";

        /// <summary>
        /// 空闲内存（MB）
        /// </summary>
        [ObservableProperty]
        private string _freeMemory = "Loading...";

        /// <summary>
        /// 最大内存（MB）
        /// </summary>
        [ObservableProperty]
        private string _maxMemory = "Loading...";

        /// <summary>
        /// 内存使用百分比
        /// </summary>
        [ObservableProperty]
        private double _memoryUsagePercentage;

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
        /// 初始化 AboutViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public AboutViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// 加载系统信息
        /// </summary>
        [RelayCommand]
        private async Task LoadSystemInfoAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = "Not connected to GeoServer";
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading system information...";

            try
            {
                var aboutService = _connectionService.GetAboutService();

                // 加载版本信息
                var versionInfo = await aboutService.GetVersionAsync();
                if (versionInfo?.About?.Resources != null)
                {
                    foreach (var resource in versionInfo.About.Resources)
                    {
                        switch (resource.Name)
                        {
                            case "GeoServer":
                                GeoServerVersion = resource.Version ?? "N/A";
                                break;
                            case "GeoTools":
                                GeoToolsVersion = resource.Version ?? "N/A";
                                break;
                            case "GeoWebCache":
                                GeoWebCacheVersion = resource.Version ?? "N/A";
                                break;
                        }
                    }
                }

                // 加载系统状态
                var systemStatus = await aboutService.GetSystemStatusAsync();
                if (systemStatus?.Metrics?.MetricArray != null)
                {
                    // 处理 JVM 信息
                    JvmVersion = systemStatus.GetJvmVersion() ?? "N/A";
                    JvmVendor = systemStatus.GetJvmVendor() ?? "N/A";

                    // 处理内存信息
                    var maxMemory = systemStatus.GetMaximumHeapMemory();
                    var totalMemory = systemStatus.GetTotalHeapMemory();
                    var freeMemory = systemStatus.GetFreeHeapMemory();
                    var usedMemory = systemStatus.GetUsedHeapMemory();

                    TotalMemory = FormatMemory(totalMemory);
                    UsedMemory = FormatMemory(usedMemory);
                    FreeMemory = FormatMemory(freeMemory);
                    MaxMemory = FormatMemory(maxMemory);

                    // 计算使用百分比
                    if (maxMemory > 0)
                    {
                        MemoryUsagePercentage = (double)usedMemory / maxMemory * 100;
                    }
                }

                StatusMessage = "System information loaded successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load system information: {ex.Message}";
                GeoServerVersion = "Error";
                GeoToolsVersion = "Error";
                GeoWebCacheVersion = "Error";
                JvmVersion = "Error";
                JvmVendor = "Error";
                TotalMemory = "Error";
                UsedMemory = "Error";
                FreeMemory = "Error";
                MaxMemory = "Error";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 格式化内存大小（字节转 MB）
        /// </summary>
        private string FormatMemory(long bytes)
        {
            double mb = bytes / (1024.0 * 1024.0);
            return $"{mb:N2} MB";
        }

        /// <summary>
        /// 初始化命令 - 加载系统信息
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadSystemInfoAsync();
        }
    }
}
