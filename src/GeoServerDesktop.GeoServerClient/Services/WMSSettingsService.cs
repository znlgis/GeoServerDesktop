using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing WMS service settings
    /// </summary>
    public class WMSSettingsService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 WMSSettingsService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public WMSSettingsService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the global WMS service settings
        /// </summary>
        /// <returns>WMS 设置</returns>
        public async Task<WMSSettings> GetWMSSettingsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/services/wms/settings.json");
            return JsonConvert.DeserializeObject<WMSSettings>(response);
        }

        /// <summary>
        /// Updates the global WMS service settings
        /// </summary>
        /// <param name="settings">Updated WMS 设置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateWMSSettingsAsync(WMSSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync("/rest/services/wms/settings", content);
        }

        /// <summary>
        /// Gets the WMS service settings for a specific workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <returns>WMS 设置 for the workspace</returns>
        public async Task<WMSSettings> GetWorkspaceWMSSettingsAsync(string workspace)
        {
            var response = await _httpClient.GetAsync($"/rest/services/wms/workspaces/{workspace}/settings.json");
            return JsonConvert.DeserializeObject<WMSSettings>(response);
        }

        /// <summary>
        /// Updates the WMS service settings for a specific workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="settings">Updated WMS 设置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateWorkspaceWMSSettingsAsync(string workspace, WMSSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/services/wms/workspaces/{workspace}/settings", content);
        }
    }
}
