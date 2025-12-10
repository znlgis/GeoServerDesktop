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
    /// Service for managing GeoServer WMTS layers (layers from cascaded WMTS services)
    /// </summary>
    public class WMTSLayerService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the WMTSLayerService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public WMTSLayerService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets a list of WMTS layers in a WMTS store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmtsStoreName">Name of the WMTS store</param>
        /// <returns>Array of WMTS layers</returns>
        public async Task<WMTSLayer[]> GetWMTSLayersAsync(string workspaceName, string wmtsStoreName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/wmtsstores/{wmtsStoreName}/wmtslayers.json");
            var wrapper = JsonConvert.DeserializeObject<WMTSLayerListWrapper>(response);
            return wrapper?.WMTSLayerList?.WMTSLayers ?? Array.Empty<WMTSLayer>();
        }

        /// <summary>
        /// Gets details for a specific WMTS layer
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmtsStoreName">Name of the WMTS store</param>
        /// <param name="wmtsLayerName">Name of the WMTS layer</param>
        /// <returns>WMTS layer details</returns>
        public async Task<WMTSLayer> GetWMTSLayerAsync(string workspaceName, string wmtsStoreName, string wmtsLayerName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/wmtsstores/{wmtsStoreName}/wmtslayers/{wmtsLayerName}.json");
            var wrapper = JsonConvert.DeserializeObject<WMTSLayerWrapper>(response);
            return wrapper?.WMTSLayer;
        }

        /// <summary>
        /// Publishes a new WMTS layer from the cascaded WMTS store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmtsStoreName">Name of the WMTS store</param>
        /// <param name="wmtsLayer">WMTS layer configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        /// <remarks>
        /// The nativeName should match a layer name available in the remote WMTS service.
        /// </remarks>
        public async Task CreateWMTSLayerAsync(string workspaceName, string wmtsStoreName, WMTSLayer wmtsLayer)
        {
            var wrapper = new { wmtsLayer = wmtsLayer };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/workspaces/{workspaceName}/wmtsstores/{wmtsStoreName}/wmtslayers", content);
        }

        /// <summary>
        /// Updates an existing WMTS layer
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmtsStoreName">Name of the WMTS store</param>
        /// <param name="wmtsLayerName">Name of the WMTS layer</param>
        /// <param name="wmtsLayer">Updated WMTS layer configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateWMTSLayerAsync(string workspaceName, string wmtsStoreName, string wmtsLayerName, WMTSLayer wmtsLayer)
        {
            var wrapper = new { wmtsLayer = wmtsLayer };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/wmtsstores/{wmtsStoreName}/wmtslayers/{wmtsLayerName}", content);
        }

        /// <summary>
        /// Deletes a WMTS layer
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmtsStoreName">Name of the WMTS store</param>
        /// <param name="wmtsLayerName">Name of the WMTS layer to delete</param>
        /// <param name="recurse">Whether to recursively delete all resources associated with the WMTS layer</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteWMTSLayerAsync(string workspaceName, string wmtsStoreName, string wmtsLayerName, bool recurse = false)
        {
            var recurseValue = recurse ? "true" : "false";
            var path = $"/rest/workspaces/{workspaceName}/wmtsstores/{wmtsStoreName}/wmtslayers/{wmtsLayerName}?recurse={recurseValue}";
            await _httpClient.DeleteAsync(path);
        }
    }
}
