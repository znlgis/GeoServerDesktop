using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoServer WMS layers (cascaded WMS layers)
    /// </summary>
    public class WMSLayerService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the WMSLayerService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public WMSLayerService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets a list of WMS layers in a WMS store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmsStoreName">Name of the WMS store</param>
        /// <returns>Array of WMS layers</returns>
        public async Task<WMSLayer[]> GetWMSLayersAsync(string workspaceName, string wmsStoreName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/wmsstores/{wmsStoreName}/wmslayers.json");
            var wrapper = JsonConvert.DeserializeObject<WMSLayerListWrapper>(response);
            return wrapper?.WMSLayerList?.WMSLayers ?? Array.Empty<WMSLayer>();
        }

        /// <summary>
        /// Gets details for a specific WMS layer
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmsStoreName">Name of the WMS store</param>
        /// <param name="wmsLayerName">Name of the WMS layer</param>
        /// <returns>WMS layer details</returns>
        public async Task<WMSLayer> GetWMSLayerAsync(string workspaceName, string wmsStoreName, string wmsLayerName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/wmsstores/{wmsStoreName}/wmslayers/{wmsLayerName}.json");
            var wrapper = JsonConvert.DeserializeObject<WMSLayerWrapper>(response);
            return wrapper?.WMSLayer;
        }

        /// <summary>
        /// Creates a new WMS layer (publishes a layer from remote WMS)
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmsStoreName">Name of the WMS store</param>
        /// <param name="wmsLayer">WMS layer configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task CreateWMSLayerAsync(string workspaceName, string wmsStoreName, WMSLayer wmsLayer)
        {
            var wrapper = new { wmsLayer = wmsLayer };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/workspaces/{workspaceName}/wmsstores/{wmsStoreName}/wmslayers", content);
        }

        /// <summary>
        /// Updates an existing WMS layer
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmsStoreName">Name of the WMS store</param>
        /// <param name="wmsLayerName">Name of the WMS layer</param>
        /// <param name="wmsLayer">Updated WMS layer configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateWMSLayerAsync(string workspaceName, string wmsStoreName, string wmsLayerName, WMSLayer wmsLayer)
        {
            var wrapper = new { wmsLayer = wmsLayer };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/wmsstores/{wmsStoreName}/wmslayers/{wmsLayerName}", content);
        }

        /// <summary>
        /// Deletes a WMS layer
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmsStoreName">Name of the WMS store</param>
        /// <param name="wmsLayerName">Name of the WMS layer to delete</param>
        /// <param name="recurse">Whether to recursively delete all resources associated with the WMS layer</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteWMSLayerAsync(string workspaceName, string wmsStoreName, string wmsLayerName, bool recurse = false)
        {
            var recurseValue = recurse ? "true" : "false";
            var path = $"/rest/workspaces/{workspaceName}/wmsstores/{wmsStoreName}/wmslayers/{wmsLayerName}?recurse={recurseValue}";
            await _httpClient.DeleteAsync(path);
        }
    }
}
