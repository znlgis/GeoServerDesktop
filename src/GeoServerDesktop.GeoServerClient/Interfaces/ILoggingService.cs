using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// ILoggingService 的服务接口（M5 接口化）：与实现类 LoggingService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Gets the current logging configuration
        /// </summary>
        /// <returns>Current logging settings</returns>
        Task<LoggingSettings> GetLoggingSettingsAsync();

        /// <summary>
        /// Updates the logging configuration
        /// </summary>
        /// <param name="loggingSettings">New logging settings</param>
        /// <returns>表示异步操作的任务</returns>
        /// <remarks>
        /// Common logging levels: DEFAULT_LOGGING, GEOTOOLS_DEVELOPER_LOGGING, GEOSERVER_DEVELOPER_LOGGING,
        /// VERBOSE_LOGGING, PRODUCTION_LOGGING, QUIET_LOGGING
        /// </remarks>
        Task UpdateLoggingSettingsAsync(LoggingSettings loggingSettings);
    }
}
