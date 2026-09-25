using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IWMSSettingsService 的服务接口（M5 接口化）：与实现类 WMSSettingsService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IWMSSettingsService
    {
        /// <summary>
        /// Gets the global WMS service settings
        /// </summary>
        /// <returns>WMS 设置</returns>
        Task<WMSSettings> GetWMSSettingsAsync();

        /// <summary>
        /// Updates the global WMS service settings
        /// </summary>
        /// <param name="settings">Updated WMS 设置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateWMSSettingsAsync(WMSSettings settings);

        /// <summary>
        /// Gets the WMS service settings for a specific workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <returns>WMS 设置 for the workspace</returns>
        Task<WMSSettings> GetWorkspaceWMSSettingsAsync(string workspace);

        /// <summary>
        /// Updates the WMS service settings for a specific workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="settings">Updated WMS 设置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateWorkspaceWMSSettingsAsync(string workspace, WMSSettings settings);
    }
}
