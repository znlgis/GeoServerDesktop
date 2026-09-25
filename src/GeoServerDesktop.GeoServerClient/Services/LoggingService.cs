using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于管理 GeoServer 日志配置的服务
    /// </summary>
    public class LoggingService : ServiceBase, ILoggingService
    {

        /// <summary>
        /// 初始化 LoggingService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public LoggingService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// Gets the current logging configuration
        /// </summary>
        /// <returns>Current logging settings</returns>
        public async Task<LoggingSettings> GetLoggingSettingsAsync()
        {
            var response = await Http.GetAsync("/rest/logging.json");
            return JsonConvert.DeserializeObject<LoggingSettings>(response);
        }

        /// <summary>
        /// Updates the logging configuration
        /// </summary>
        /// <param name="loggingSettings">New logging settings</param>
        /// <returns>表示异步操作的任务</returns>
        /// <remarks>
        /// Common logging levels: DEFAULT_LOGGING, GEOTOOLS_DEVELOPER_LOGGING, GEOSERVER_DEVELOPER_LOGGING,
        /// VERBOSE_LOGGING, PRODUCTION_LOGGING, QUIET_LOGGING
        /// </remarks>
        public async Task UpdateLoggingSettingsAsync(LoggingSettings loggingSettings)
        {
            await PutJsonAsync("/rest/logging", loggingSettings);
        }
    }
}
