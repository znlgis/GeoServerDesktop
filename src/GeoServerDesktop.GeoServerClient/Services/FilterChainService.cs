using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing security filter chains
    /// </summary>
    public class FilterChainService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the FilterChainService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public FilterChainService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the list of all filter chains
        /// </summary>
        /// <returns>List of filter chains</returns>
        public async Task<FilterChainListWrapper> GetFilterChainsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/security/filterChains.json");
            return JsonConvert.DeserializeObject<FilterChainListWrapper>(response);
        }

        /// <summary>
        /// Gets details for a specific filter chain
        /// </summary>
        /// <param name="chainName">Chain name</param>
        /// <returns>Filter chain details</returns>
        public async Task<FilterChainWrapper> GetFilterChainAsync(string chainName)
        {
            var response = await _httpClient.GetAsync($"/rest/security/filterChains/{chainName}.json");
            return JsonConvert.DeserializeObject<FilterChainWrapper>(response);
        }

        /// <summary>
        /// Updates an existing filter chain
        /// </summary>
        /// <param name="chainName">Chain name</param>
        /// <param name="chain">Updated chain information</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateFilterChainAsync(string chainName, FilterChain chain)
        {
            var wrapper = new { filterChain = chain };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/security/filterChains/{chainName}", content);
        }
    }
}
