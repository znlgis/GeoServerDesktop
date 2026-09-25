using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing WCS service settings
    /// </summary>
    public class WCSSettingsService : ServiceBase, IWCSSettingsService
    {

        /// <summary>
        /// 初始化 WCSSettingsService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public WCSSettingsService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// Gets the global WCS service settings
        /// </summary>
        /// <returns>WCS 设置</returns>
        public async Task<WCSSettings> GetWCSSettingsAsync()
        {
            var response = await Http.GetAsync("/rest/services/wcs/settings.json");
            return JsonConvert.DeserializeObject<WCSSettings>(response);
        }

        /// <summary>
        /// Updates the global WCS service settings
        /// </summary>
        /// <param name="settings">Updated WCS 设置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateWCSSettingsAsync(WCSSettings settings)
        {
            await PutJsonAsync("/rest/services/wcs/settings", settings);
        }

        /// <summary>
        /// Gets the WCS service settings for a specific workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <returns>WCS 设置 for the workspace</returns>
        public async Task<WCSSettings> GetWorkspaceWCSSettingsAsync(string workspace)
        {
            var response = await Http.GetAsync($"/rest/services/wcs/workspaces/{Esc(workspace)}/settings.json");
            return JsonConvert.DeserializeObject<WCSSettings>(response);
        }

        /// <summary>
        /// Updates the WCS service settings for a specific workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="settings">Updated WCS 设置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateWorkspaceWCSSettingsAsync(string workspace, WCSSettings settings)
        {
            await PutJsonAsync($"/rest/services/wcs/workspaces/{Esc(workspace)}/settings", settings);
        }
    }
}
