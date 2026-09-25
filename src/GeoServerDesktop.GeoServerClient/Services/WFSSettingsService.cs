using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing WFS service settings
    /// </summary>
    public class WFSSettingsService : ServiceBase
    {

        /// <summary>
        /// 初始化 WFSSettingsService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public WFSSettingsService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// Gets the global WFS service settings
        /// </summary>
        /// <returns>WFS 设置</returns>
        public async Task<WFSSettings> GetWFSSettingsAsync()
        {
            var response = await Http.GetAsync("/rest/services/wfs/settings.json");
            return JsonConvert.DeserializeObject<WFSSettings>(response);
        }

        /// <summary>
        /// Updates the global WFS service settings
        /// </summary>
        /// <param name="settings">Updated WFS 设置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateWFSSettingsAsync(WFSSettings settings)
        {
            await PutJsonAsync("/rest/services/wfs/settings", settings);
        }

        /// <summary>
        /// Gets the WFS service settings for a specific workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <returns>WFS 设置 for the workspace</returns>
        public async Task<WFSSettings> GetWorkspaceWFSSettingsAsync(string workspace)
        {
            var response = await Http.GetAsync($"/rest/services/wfs/workspaces/{Esc(workspace)}/settings.json");
            return JsonConvert.DeserializeObject<WFSSettings>(response);
        }

        /// <summary>
        /// Updates the WFS service settings for a specific workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="settings">Updated WFS 设置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateWorkspaceWFSSettingsAsync(string workspace, WFSSettings settings)
        {
            await PutJsonAsync($"/rest/services/wfs/workspaces/{Esc(workspace)}/settings", settings);
        }
    }
}
