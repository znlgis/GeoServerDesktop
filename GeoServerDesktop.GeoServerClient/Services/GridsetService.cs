using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoWebCache gridsets
    /// </summary>
    public class GridsetService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the GridsetService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public GridsetService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the list of gridsets
        /// </summary>
        /// <returns>List of gridsets</returns>
        public async Task<GridsetListWrapper> GetGridsetsAsync()
        {
            var response = await _httpClient.GetAsync("/gwc/rest/gridsets.json");
            return JsonConvert.DeserializeObject<GridsetListWrapper>(response);
        }

        /// <summary>
        /// Gets details for a specific gridset
        /// </summary>
        /// <param name="gridsetName">Gridset name</param>
        /// <returns>Gridset details</returns>
        public async Task<Gridset> GetGridsetAsync(string gridsetName)
        {
            var response = await _httpClient.GetAsync($"/gwc/rest/gridsets/{gridsetName}.json");
            return JsonConvert.DeserializeObject<Gridset>(response);
        }

        /// <summary>
        /// Creates a new gridset
        /// </summary>
        /// <param name="gridset">Gridset to create</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task CreateGridsetAsync(Gridset gridset)
        {
            var json = JsonConvert.SerializeObject(gridset);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/gwc/rest/gridsets", content);
        }

        /// <summary>
        /// Deletes a gridset
        /// </summary>
        /// <param name="gridsetName">Gridset name</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteGridsetAsync(string gridsetName)
        {
            await _httpClient.DeleteAsync($"/gwc/rest/gridsets/{gridsetName}");
        }
    }
}
