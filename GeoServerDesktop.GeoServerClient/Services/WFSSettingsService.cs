using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing WFS service settings
    /// </summary>
    public class WFSSettingsService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the WFSSettingsService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public WFSSettingsService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the global WFS service settings
        /// </summary>
        /// <returns>WFS settings</returns>
        public async Task<WFSSettings> GetWFSSettingsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/services/wfs/settings.json");
            return JsonConvert.DeserializeObject<WFSSettings>(response);
        }

        /// <summary>
        /// Updates the global WFS service settings
        /// </summary>
        /// <param name="settings">Updated WFS settings</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateWFSSettingsAsync(WFSSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync("/rest/services/wfs/settings", content);
        }

        /// <summary>
        /// Gets the WFS service settings for a specific workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <returns>WFS settings for the workspace</returns>
        public async Task<WFSSettings> GetWorkspaceWFSSettingsAsync(string workspace)
        {
            var response = await _httpClient.GetAsync($"/rest/services/wfs/workspaces/{workspace}/settings.json");
            return JsonConvert.DeserializeObject<WFSSettings>(response);
        }

        /// <summary>
        /// Updates the WFS service settings for a specific workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="settings">Updated WFS settings</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateWorkspaceWFSSettingsAsync(string workspace, WFSSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/services/wfs/workspaces/{workspace}/settings", content);
        }
    }
}
