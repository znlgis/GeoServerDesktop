using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoServer layer groups
    /// </summary>
    public class LayerGroupService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the LayerGroupService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public LayerGroupService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets a list of all layer groups
        /// </summary>
        /// <returns>Array of layer groups</returns>
        public async Task<LayerGroup[]> GetLayerGroupsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/layergroups.json");
            var wrapper = JsonConvert.DeserializeObject<LayerGroupListWrapper>(response);
            return wrapper?.LayerGroupList?.LayerGroups ?? new LayerGroup[0];
        }

        /// <summary>
        /// Gets details for a specific layer group
        /// </summary>
        /// <param name="layerGroupName">Name of the layer group</param>
        /// <returns>Layer group details</returns>
        public async Task<LayerGroup> GetLayerGroupAsync(string layerGroupName)
        {
            var response = await _httpClient.GetAsync($"/rest/layergroups/{layerGroupName}.json");
            var wrapper = JsonConvert.DeserializeObject<LayerGroupWrapper>(response);
            return wrapper?.LayerGroup;
        }

        /// <summary>
        /// Creates a new layer group
        /// </summary>
        /// <param name="layerGroup">Layer group configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task CreateLayerGroupAsync(LayerGroup layerGroup)
        {
            var wrapper = new { layerGroup = layerGroup };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/rest/layergroups", content);
        }

        /// <summary>
        /// Updates an existing layer group
        /// </summary>
        /// <param name="layerGroupName">Name of the layer group</param>
        /// <param name="layerGroup">Updated layer group configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateLayerGroupAsync(string layerGroupName, LayerGroup layerGroup)
        {
            var wrapper = new { layerGroup = layerGroup };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/layergroups/{layerGroupName}", content);
        }

        /// <summary>
        /// Deletes a layer group
        /// </summary>
        /// <param name="layerGroupName">Name of the layer group to delete</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteLayerGroupAsync(string layerGroupName)
        {
            await _httpClient.DeleteAsync($"/rest/layergroups/{layerGroupName}");
        }
    }
}
