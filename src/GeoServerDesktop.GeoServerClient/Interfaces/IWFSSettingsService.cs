using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IWFSSettingsService 的服务接口（M5 接口化）：与实现类 WFSSettingsService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IWFSSettingsService
    {
        /// <summary>
        /// Gets the global WFS service settings
        /// </summary>
        /// <returns>WFS 设置</returns>
        Task<WFSSettings> GetWFSSettingsAsync();

        /// <summary>
        /// Updates the global WFS service settings
        /// </summary>
        /// <param name="settings">Updated WFS 设置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateWFSSettingsAsync(WFSSettings settings);

        /// <summary>
        /// Gets the WFS service settings for a specific workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <returns>WFS 设置 for the workspace</returns>
        Task<WFSSettings> GetWorkspaceWFSSettingsAsync(string workspace);

        /// <summary>
        /// Updates the WFS service settings for a specific workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="settings">Updated WFS 设置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateWorkspaceWFSSettingsAsync(string workspace, WFSSettings settings);
    }
}
