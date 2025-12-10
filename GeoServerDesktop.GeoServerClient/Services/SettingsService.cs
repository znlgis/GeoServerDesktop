using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoServer global settings
    /// </summary>
    public class SettingsService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the SettingsService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public SettingsService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the global GeoServer settings
        /// </summary>
        /// <returns>Global settings</returns>
        public async Task<GlobalSettings> GetGlobalSettingsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/settings.json");
            return JsonConvert.DeserializeObject<GlobalSettings>(response);
        }

        /// <summary>
        /// Updates the global GeoServer settings
        /// </summary>
        /// <param name="settings">Updated global settings</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateGlobalSettingsAsync(GlobalSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync("/rest/settings", content);
        }

        /// <summary>
        /// Gets the contact information from global settings
        /// </summary>
        /// <returns>Contact information</returns>
        public async Task<ContactInfoWrapper> GetContactInfoAsync()
        {
            var response = await _httpClient.GetAsync("/rest/settings/contact.json");
            return JsonConvert.DeserializeObject<ContactInfoWrapper>(response);
        }

        /// <summary>
        /// Updates the contact information in global settings
        /// </summary>
        /// <param name="contact">Updated contact information</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateContactInfoAsync(ContactInfo contact)
        {
            var wrapper = new { contact = contact };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync("/rest/settings/contact", content);
        }
    }
}
