using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于管理 GeoServer 日志配置的服务
    /// </summary>
    public class LoggingService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 LoggingService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public LoggingService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the current logging configuration
        /// </summary>
        /// <returns>Current logging settings</returns>
        public async Task<LoggingSettings> GetLoggingSettingsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/logging.json");
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
            var json = JsonConvert.SerializeObject(loggingSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync("/rest/logging", content);
        }
    }
}
