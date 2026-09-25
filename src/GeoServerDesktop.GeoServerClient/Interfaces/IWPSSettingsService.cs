using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IWPSSettingsService 的服务接口（M5 接口化）：与实现类 WPSSettingsService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IWPSSettingsService
    {
        /// <summary>
        /// Gets the WPS service settings
        /// </summary>
        /// <returns>WPS 设置</returns>
        Task<WPSSettings> GetSettingsAsync();

        /// <summary>
        /// Updates the WPS service settings
        /// </summary>
        /// <param name="settings">Updated WPS 设置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateSettingsAsync(WPSSettings settings);
    }
}
