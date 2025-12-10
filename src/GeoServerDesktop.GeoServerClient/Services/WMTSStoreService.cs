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
    /// Service for managing GeoServer WMTS stores (cascaded WMTS services)
    /// </summary>
    public class WMTSStoreService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the WMTSStoreService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public WMTSStoreService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets a list of WMTS stores in a workspace
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <returns>Array of WMTS stores</returns>
        public async Task<WMTSStore[]> GetWMTSStoresAsync(string workspaceName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/wmtsstores.json");
            var wrapper = JsonConvert.DeserializeObject<WMTSStoreListWrapper>(response);
            return wrapper?.WMTSStoreList?.WMTSStores ?? Array.Empty<WMTSStore>();
        }

        /// <summary>
        /// Gets details for a specific WMTS store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmtsStoreName">Name of the WMTS store</param>
        /// <returns>WMTS store details</returns>
        public async Task<WMTSStore> GetWMTSStoreAsync(string workspaceName, string wmtsStoreName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/wmtsstores/{wmtsStoreName}.json");
            var wrapper = JsonConvert.DeserializeObject<WMTSStoreWrapper>(response);
            return wrapper?.WMTSStore;
        }

        /// <summary>
        /// Creates a new WMTS store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmtsStore">WMTS store configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        /// <remarks>
        /// The WMTS store enables cascading of a remote WMTS service.
        /// The capabilitiesURL should point to a valid WMTS GetCapabilities document.
        /// </remarks>
        public async Task CreateWMTSStoreAsync(string workspaceName, WMTSStore wmtsStore)
        {
            var wrapper = new { wmtsStore = wmtsStore };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/workspaces/{workspaceName}/wmtsstores", content);
        }

        /// <summary>
        /// Updates an existing WMTS store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmtsStoreName">Name of the WMTS store</param>
        /// <param name="wmtsStore">Updated WMTS store configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateWMTSStoreAsync(string workspaceName, string wmtsStoreName, WMTSStore wmtsStore)
        {
            var wrapper = new { wmtsStore = wmtsStore };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/wmtsstores/{wmtsStoreName}", content);
        }

        /// <summary>
        /// Deletes a WMTS store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmtsStoreName">Name of the WMTS store to delete</param>
        /// <param name="recurse">Whether to recursively delete all WMTS layers in the store</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteWMTSStoreAsync(string workspaceName, string wmtsStoreName, bool recurse = false)
        {
            var recurseValue = recurse ? "true" : "false";
            var path = $"/rest/workspaces/{workspaceName}/wmtsstores/{wmtsStoreName}?recurse={recurseValue}";
            await _httpClient.DeleteAsync(path);
        }
    }
}
