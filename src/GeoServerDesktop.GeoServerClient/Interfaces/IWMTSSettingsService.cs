using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IWMTSSettingsService 的服务接口（M5 接口化）：与实现类 WMTSSettingsService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IWMTSSettingsService
    {
        /// <summary>
        /// Gets the global WMTS service settings
        /// </summary>
        /// <returns>WMTS 设置</returns>
        Task<WMTSSettings> GetWMTSSettingsAsync();

        /// <summary>
        /// Updates the global WMTS service settings
        /// </summary>
        /// <param name="settings">Updated WMTS 设置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateWMTSSettingsAsync(WMTSSettings settings);
    }
}
