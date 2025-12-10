using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing WCS service settings
    /// </summary>
    public class WCSSettingsService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 WCSSettingsService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public WCSSettingsService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the global WCS service settings
        /// </summary>
        /// <returns>WCS 设置</returns>
        public async Task<WCSSettings> GetWCSSettingsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/services/wcs/settings.json");
            return JsonConvert.DeserializeObject<WCSSettings>(response);
        }

        /// <summary>
        /// Updates the global WCS service settings
        /// </summary>
        /// <param name="settings">Updated WCS 设置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateWCSSettingsAsync(WCSSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync("/rest/services/wcs/settings", content);
        }

        /// <summary>
        /// Gets the WCS service settings for a specific workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <returns>WCS 设置 for the workspace</returns>
        public async Task<WCSSettings> GetWorkspaceWCSSettingsAsync(string workspace)
        {
            var response = await _httpClient.GetAsync($"/rest/services/wcs/workspaces/{workspace}/settings.json");
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
            var json = JsonConvert.SerializeObject(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/services/wcs/workspaces/{workspace}/settings", content);
        }
    }
}
