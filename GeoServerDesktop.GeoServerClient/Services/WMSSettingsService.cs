using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing WMS service settings
    /// </summary>
    public class WMSSettingsService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the WMSSettingsService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public WMSSettingsService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the global WMS service settings
        /// </summary>
        /// <returns>WMS settings</returns>
        public async Task<WMSSettings> GetWMSSettingsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/services/wms/settings.json");
            return JsonConvert.DeserializeObject<WMSSettings>(response);
        }

        /// <summary>
        /// Updates the global WMS service settings
        /// </summary>
        /// <param name="settings">Updated WMS settings</param>
        /// <returns>Task representing the asynchronous operation</returns>
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
        /// <returns>WMS settings for the workspace</returns>
        public async Task<WMSSettings> GetWorkspaceWMSSettingsAsync(string workspace)
        {
            var response = await _httpClient.GetAsync($"/rest/services/wms/workspaces/{workspace}/settings.json");
            return JsonConvert.DeserializeObject<WMSSettings>(response);
        }

        /// <summary>
        /// Updates the WMS service settings for a specific workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="settings">Updated WMS settings</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateWorkspaceWMSSettingsAsync(string workspace, WMSSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/services/wms/workspaces/{workspace}/settings", content);
        }
    }
}
