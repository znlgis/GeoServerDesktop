using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoWebCache layers
    /// </summary>
    public class GWCLayerService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the GWCLayerService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public GWCLayerService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the list of cached layers
        /// </summary>
        /// <returns>List of cached layers</returns>
        public async Task<GWCLayerListWrapper> GetLayersAsync()
        {
            var response = await _httpClient.GetAsync("/gwc/rest/layers.json");
            return JsonConvert.DeserializeObject<GWCLayerListWrapper>(response);
        }

        /// <summary>
        /// Gets information about a specific cached layer
        /// </summary>
        /// <param name="layerName">Layer name</param>
        /// <returns>Layer information</returns>
        public async Task<GWCLayer> GetLayerAsync(string layerName)
        {
            var response = await _httpClient.GetAsync($"/gwc/rest/layers/{layerName}.json");
            return JsonConvert.DeserializeObject<GWCLayer>(response);
        }

        /// <summary>
        /// Seeds a layer (starts tile generation)
        /// </summary>
        /// <param name="layerName">Layer name</param>
        /// <param name="seedRequest">Seed request configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task SeedLayerAsync(string layerName, SeedRequest seedRequest)
        {
            var json = JsonConvert.SerializeObject(seedRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/gwc/rest/seed/{layerName}.json", content);
        }

        /// <summary>
        /// Truncates the cache for all layers (mass truncate)
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task TruncateAllLayersAsync()
        {
            var truncateRequest = new { truncateAll = true };
            var json = JsonConvert.SerializeObject(truncateRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/gwc/rest/masstruncate", content);
        }
    }
}
