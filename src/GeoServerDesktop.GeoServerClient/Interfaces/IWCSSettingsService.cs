using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IWCSSettingsService 的服务接口（M5 接口化）：与实现类 WCSSettingsService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IWCSSettingsService
    {
        /// <summary>
        /// Gets the global WCS service settings
        /// </summary>
        /// <returns>WCS 设置</returns>
        Task<WCSSettings> GetWCSSettingsAsync();

        /// <summary>
        /// Updates the global WCS service settings
        /// </summary>
        /// <param name="settings">Updated WCS 设置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateWCSSettingsAsync(WCSSettings settings);

        /// <summary>
        /// Gets the WCS service settings for a specific workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <returns>WCS 设置 for the workspace</returns>
        Task<WCSSettings> GetWorkspaceWCSSettingsAsync(string workspace);

        /// <summary>
        /// Updates the WCS service settings for a specific workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="settings">Updated WCS 设置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateWorkspaceWCSSettingsAsync(string workspace, WCSSettings settings);
    }
}
