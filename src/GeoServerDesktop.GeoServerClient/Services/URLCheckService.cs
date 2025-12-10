using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing URL validation checks
    /// </summary>
    public class URLCheckService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the URLCheckService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public URLCheckService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the list of URL validation checks
        /// </summary>
        /// <returns>List of URL checks</returns>
        public async Task<URLCheckListWrapper> GetURLChecksAsync()
        {
            var response = await _httpClient.GetAsync("/rest/urlchecks.json");
            return JsonConvert.DeserializeObject<URLCheckListWrapper>(response);
        }

        /// <summary>
        /// Creates a new URL validation check
        /// </summary>
        /// <param name="check">URL check to create</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task CreateURLCheckAsync(URLCheck check)
        {
            var json = JsonConvert.SerializeObject(check);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/rest/urlchecks", content);
        }

        /// <summary>
        /// Deletes a URL validation check
        /// </summary>
        /// <param name="checkName">Check name</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteURLCheckAsync(string checkName)
        {
            await _httpClient.DeleteAsync($"/rest/urlchecks/{checkName}");
        }
    }
}
