using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing WPS (Web Processing Service) settings
    /// </summary>
    public class WPSSettingsService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the WPSSettingsService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public WPSSettingsService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the WPS service settings
        /// </summary>
        /// <returns>WPS settings</returns>
        public async Task<WPSSettings> GetSettingsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/services/wps/settings.json");
            return JsonConvert.DeserializeObject<WPSSettings>(response);
        }

        /// <summary>
        /// Updates the WPS service settings
        /// </summary>
        /// <param name="settings">Updated WPS settings</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateSettingsAsync(WPSSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync("/rest/services/wps/settings", content);
        }
    }
}
