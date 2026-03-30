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
    /// GeoServer 日志配置的视图模型
    /// </summary>
    public partial class LoggingViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>当前日志级别</summary>
        [ObservableProperty]
        private string _logLevel = string.Empty;

        /// <summary>日志文件位置</summary>
        [ObservableProperty]
        private string _logLocation = string.Empty;

        /// <summary>是否启用标准输出日志</summary>
        [ObservableProperty]
        private bool _stdOutLogging;

        /// <summary>是否启用文件日志</summary>
        [ObservableProperty]
        private bool _fileLogging;

        /// <summary>可用的日志级别选项</summary>
        public ObservableCollection<string> LogLevelOptions { get; } = new()
        {
            "DEFAULT_LOGGING",
            "GEOTOOLS_DEVELOPER_LOGGING",
            "GEOSERVER_DEVELOPER_LOGGING",
            "VERBOSE_LOGGING",
            "PRODUCTION_LOGGING",
            "QUIET_LOGGING"
        };

        /// <summary>是否正在加载</summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>状态消息</summary>
        [ObservableProperty]
        private string _statusMessage = "Ready";

        /// <summary>
        /// 初始化 LoggingViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public LoggingViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// 加载日志设置
        /// </summary>
        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = "Not connected to GeoServer";
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading logging settings...";

            try
            {
                var service = _connectionService.GetLoggingService();
                var loggingSettings = await service.GetLoggingSettingsAsync();

                if (loggingSettings?.Logging != null)
                {
                    LogLevel = loggingSettings.Logging.Level ?? string.Empty;
                    LogLocation = loggingSettings.Logging.Location ?? string.Empty;
                    StdOutLogging = loggingSettings.Logging.StdOutLogging ?? false;
                    FileLogging = loggingSettings.Logging.FileLogging ?? false;
                }

                StatusMessage = "Logging settings loaded";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load logging settings: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 保存日志设置
        /// </summary>
        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = "Not connected to GeoServer";
                return;
            }

            IsLoading = true;
            StatusMessage = "Saving logging settings...";

            try
            {
                var service = _connectionService.GetLoggingService();
                var loggingSettings = new LoggingSettings
                {
                    Logging = new LoggingConfig
                    {
                        Level = LogLevel,
                        Location = LogLocation,
                        StdOutLogging = StdOutLogging,
                        FileLogging = FileLogging
                    }
                };

                await service.UpdateLoggingSettingsAsync(loggingSettings);
                StatusMessage = "Logging settings saved successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to save logging settings: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
