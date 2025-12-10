using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing CSW (Catalogue Service for the Web) settings
    /// </summary>
    public class CSWSettingsService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the CSWSettingsService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public CSWSettingsService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the CSW service settings
        /// </summary>
        /// <returns>CSW settings</returns>
        public async Task<CSWSettings> GetSettingsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/services/csw/settings.json");
            return JsonConvert.DeserializeObject<CSWSettings>(response);
        }

        /// <summary>
        /// Updates the CSW service settings
        /// </summary>
        /// <param name="settings">Updated CSW settings</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateSettingsAsync(CSWSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync("/rest/services/csw/settings", content);
        }
    }
}
