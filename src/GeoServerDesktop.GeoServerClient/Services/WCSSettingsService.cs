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
        /// Initializes a new instance of the WCSSettingsService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public WCSSettingsService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the global WCS service settings
        /// </summary>
        /// <returns>WCS settings</returns>
        public async Task<WCSSettings> GetWCSSettingsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/services/wcs/settings.json");
            return JsonConvert.DeserializeObject<WCSSettings>(response);
        }

        /// <summary>
        /// Updates the global WCS service settings
        /// </summary>
        /// <param name="settings">Updated WCS settings</param>
        /// <returns>Task representing the asynchronous operation</returns>
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
        /// <returns>WCS settings for the workspace</returns>
        public async Task<WCSSettings> GetWorkspaceWCSSettingsAsync(string workspace)
        {
            var response = await _httpClient.GetAsync($"/rest/services/wcs/workspaces/{workspace}/settings.json");
            return JsonConvert.DeserializeObject<WCSSettings>(response);
        }

        /// <summary>
        /// Updates the WCS service settings for a specific workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="settings">Updated WCS settings</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateWorkspaceWCSSettingsAsync(string workspace, WCSSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/services/wcs/workspaces/{workspace}/settings", content);
        }
    }
}
