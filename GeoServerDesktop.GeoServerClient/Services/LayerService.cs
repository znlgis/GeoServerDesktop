using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoServer layers
    /// </summary>
    public class LayerService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the LayerService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public LayerService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets a list of all layers
        /// </summary>
        /// <returns>Array of layers</returns>
        public async Task<Layer[]> GetLayersAsync()
        {
            var response = await _httpClient.GetAsync("/rest/layers.json");
            var wrapper = JsonConvert.DeserializeObject<LayerListWrapper>(response);
            return wrapper?.LayerList?.Layers ?? new Layer[0];
        }

        /// <summary>
        /// Gets details for a specific layer
        /// </summary>
        /// <param name="layerName">Name of the layer</param>
        /// <returns>Layer details</returns>
        public async Task<Layer> GetLayerAsync(string layerName)
        {
            var response = await _httpClient.GetAsync($"/rest/layers/{layerName}.json");
            var wrapper = JsonConvert.DeserializeObject<LayerWrapper>(response);
            return wrapper?.Layer;
        }

        /// <summary>
        /// Updates an existing layer
        /// </summary>
        /// <param name="layerName">Name of the layer</param>
        /// <param name="layer">Updated layer configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateLayerAsync(string layerName, Layer layer)
        {
            var wrapper = new { layer = layer };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/layers/{layerName}", content);
        }

        /// <summary>
        /// Deletes a layer
        /// </summary>
        /// <param name="layerName">Name of the layer to delete</param>
        /// <param name="recurse">Whether to recursively delete all resources associated with the layer</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteLayerAsync(string layerName, bool recurse = false)
        {
            var path = $"/rest/layers/{layerName}?recurse={recurse.ToString().ToLower()}";
            await _httpClient.DeleteAsync(path);
        }

        /// <summary>
        /// Gets a list of layers in a specific workspace
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <returns>Array of layers in the workspace</returns>
        public async Task<Layer[]> GetWorkspaceLayersAsync(string workspaceName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/layers.json");
            var wrapper = JsonConvert.DeserializeObject<LayerListWrapper>(response);
            return wrapper?.LayerList?.Layers ?? new Layer[0];
        }

        /// <summary>
        /// Gets details for a specific layer in a workspace
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="layerName">Name of the layer</param>
        /// <returns>Layer details</returns>
        public async Task<Layer> GetWorkspaceLayerAsync(string workspaceName, string layerName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/layers/{layerName}.json");
            var wrapper = JsonConvert.DeserializeObject<LayerWrapper>(response);
            return wrapper?.Layer;
        }
    }
}
