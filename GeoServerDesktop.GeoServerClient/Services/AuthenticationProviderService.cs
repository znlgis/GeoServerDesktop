using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing authentication providers
    /// </summary>
    public class AuthenticationProviderService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the AuthenticationProviderService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public AuthenticationProviderService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the list of all authentication providers
        /// </summary>
        /// <returns>List of authentication providers</returns>
        public async Task<AuthenticationProviderListWrapper> GetProvidersAsync()
        {
            var response = await _httpClient.GetAsync("/rest/security/authProviders.json");
            return JsonConvert.DeserializeObject<AuthenticationProviderListWrapper>(response);
        }

        /// <summary>
        /// Gets details for a specific authentication provider
        /// </summary>
        /// <param name="providerName">Provider name</param>
        /// <returns>Authentication provider details</returns>
        public async Task<AuthenticationProviderWrapper> GetProviderAsync(string providerName)
        {
            var response = await _httpClient.GetAsync($"/rest/security/authProviders/{providerName}.json");
            return JsonConvert.DeserializeObject<AuthenticationProviderWrapper>(response);
        }

        /// <summary>
        /// Creates a new authentication provider
        /// </summary>
        /// <param name="provider">Authentication provider to create</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task CreateProviderAsync(AuthenticationProvider provider)
        {
            var wrapper = new { provider = provider };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/rest/security/authProviders", content);
        }

        /// <summary>
        /// Updates an existing authentication provider
        /// </summary>
        /// <param name="providerName">Provider name</param>
        /// <param name="provider">Updated provider information</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateProviderAsync(string providerName, AuthenticationProvider provider)
        {
            var wrapper = new { provider = provider };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/security/authProviders/{providerName}", content);
        }

        /// <summary>
        /// Deletes an authentication provider
        /// </summary>
        /// <param name="providerName">Provider name to delete</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteProviderAsync(string providerName)
        {
            await _httpClient.DeleteAsync($"/rest/security/authProviders/{providerName}");
        }
    }
}
