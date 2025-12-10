using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing WMTS service settings
    /// </summary>
    public class WMTSSettingsService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the WMTSSettingsService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public WMTSSettingsService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the global WMTS service settings
        /// </summary>
        /// <returns>WMTS settings</returns>
        public async Task<WMTSSettings> GetWMTSSettingsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/services/wmts/settings.json");
            return JsonConvert.DeserializeObject<WMTSSettings>(response);
        }

        /// <summary>
        /// Updates the global WMTS service settings
        /// </summary>
        /// <param name="settings">Updated WMTS settings</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateWMTSSettingsAsync(WMTSSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync("/rest/services/wmts/settings", content);
        }
    }
}
