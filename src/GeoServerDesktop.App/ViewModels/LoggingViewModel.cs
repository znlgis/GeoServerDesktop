using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// GeoServer 日志配置的视图模型
    /// </summary>
    public partial class LoggingViewModel : ViewModelBase
    {
        /// <summary>日志查看器最多展示的尾部行数（避免全量渲染超大日志文件）</summary>
        public const int MaxLogLines = 500;

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
        private string _statusMessage = string.Empty;

        /// <summary>日志文件内容（尾部若干行）</summary>
        [ObservableProperty]
        private string _logContent = string.Empty;

        /// <summary>是否正在加载日志文件</summary>
        [ObservableProperty]
        private bool _isLogLoading;

        /// <summary>日志文件在数据目录中的资源路径（通过 REST 读取）</summary>
        [ObservableProperty]
        private string _logResourcePath = string.Empty;

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
                StatusMessage = L.StatusNotConnected;
                return;
            }

            IsLoading = true;
            StatusMessage = L.StatusLoadingLoggingSettings;

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

                StatusMessage = L.StatusLoggingSettingsLoaded;
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusLoggingSettingsLoadFailed, ex.Message);
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
                StatusMessage = L.StatusNotConnected;
                return;
            }

            IsLoading = true;
            StatusMessage = L.StatusSavingLoggingSettings;

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
                StatusMessage = L.StatusLoggingSettingsSaved;
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusLoggingSettingsSaveFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 刷新日志文件内容（读取数据目录中日志文件的尾部）
        /// </summary>
        [RelayCommand]
        private async Task RefreshLogAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = L.StatusNotConnected;
                return;
            }

            IsLogLoading = true;
            StatusMessage = L.StatusLoadingLogFile;

            try
            {
                // 先取日志配置中的位置（GET 未返回 location 时使用 GeoServer 默认位置）
                var loggingService = _connectionService.GetLoggingService();
                var loggingSettings = await loggingService.GetLoggingSettingsAsync();
                var location = loggingSettings?.Logging?.Location;

                var resourcePath = string.IsNullOrWhiteSpace(location)
                    ? LogResourcePathResolver.DefaultLogResourcePath
                    : LogResourcePathResolver.Resolve(location);

                if (resourcePath == null)
                {
                    LogContent = string.Empty;
                    LogResourcePath = string.Empty;
                    StatusMessage = string.Format(L.StatusLogFileUnresolvable, location);
                    return;
                }

                LogResourcePath = resourcePath;

                var resourceService = _connectionService.GetResourceService();
                var content = await resourceService.GetResourceContentAsync(resourcePath);

                LogContent = TakeLastLines(content, MaxLogLines);
                StatusMessage = string.Format(L.StatusLogFileLoaded, CountLines(LogContent));
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusLogFileLoadFailed, ex.Message);
            }
            finally
            {
                IsLogLoading = false;
            }
        }

        /// <summary>
        /// 截取文本的最后若干行（用于展示日志尾部，避免全量渲染超大文件）。
        /// 内容不足 maxLines 行时返回原文。
        /// </summary>
        /// <param name="content">原始文本</param>
        /// <param name="maxLines">最多保留的行数</param>
        /// <returns>尾部文本</returns>
        public static string TakeLastLines(string content, int maxLines)
        {
            if (string.IsNullOrEmpty(content) || maxLines <= 0)
                return string.Empty;

            var idx = content.Length - 1;
            if (content[idx] == '\n')
                idx--; // 末尾换行不作为行边界

            var count = 0;
            var cut = -1;
            while (idx >= 0 && count < maxLines)
            {
                if (content[idx] == '\n')
                {
                    count++;
                    if (count == maxLines)
                        cut = idx;
                }
                idx--;
            }

            return cut < 0 ? content : content.Substring(cut + 1);
        }

        /// <summary>
        /// 统计文本行数（末尾换行不产生额外行）
        /// </summary>
        /// <param name="text">文本</param>
        /// <returns>行数；空文本为 0</returns>
        public static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            var count = 0;
            foreach (var ch in text)
            {
                if (ch == '\n')
                    count++;
            }

            if (text[text.Length - 1] != '\n')
                count++;

            return count;
        }
    }
}
