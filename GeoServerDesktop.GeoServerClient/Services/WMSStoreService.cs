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
    /// Service for managing GeoServer WMS stores (cascaded WMS)
    /// </summary>
    public class WMSStoreService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the WMSStoreService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public WMSStoreService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets a list of WMS stores in a workspace
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <returns>Array of WMS stores</returns>
        public async Task<WMSStore[]> GetWMSStoresAsync(string workspaceName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/wmsstores.json");
            var wrapper = JsonConvert.DeserializeObject<WMSStoreListWrapper>(response);
            return wrapper?.WMSStoreList?.WMSStores ?? Array.Empty<WMSStore>();
        }

        /// <summary>
        /// Gets details for a specific WMS store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmsStoreName">Name of the WMS store</param>
        /// <returns>WMS store details</returns>
        public async Task<WMSStore> GetWMSStoreAsync(string workspaceName, string wmsStoreName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/wmsstores/{wmsStoreName}.json");
            var wrapper = JsonConvert.DeserializeObject<WMSStoreWrapper>(response);
            return wrapper?.WMSStore;
        }

        /// <summary>
        /// Creates a new WMS store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmsStore">WMS store configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task CreateWMSStoreAsync(string workspaceName, WMSStore wmsStore)
        {
            var wrapper = new { wmsStore = wmsStore };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/workspaces/{workspaceName}/wmsstores", content);
        }

        /// <summary>
        /// Updates an existing WMS store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmsStoreName">Name of the WMS store</param>
        /// <param name="wmsStore">Updated WMS store configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateWMSStoreAsync(string workspaceName, string wmsStoreName, WMSStore wmsStore)
        {
            var wrapper = new { wmsStore = wmsStore };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/wmsstores/{wmsStoreName}", content);
        }

        /// <summary>
        /// Deletes a WMS store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="wmsStoreName">Name of the WMS store to delete</param>
        /// <param name="recurse">Whether to recursively delete all WMS layers in the store</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteWMSStoreAsync(string workspaceName, string wmsStoreName, bool recurse = false)
        {
            var recurseValue = recurse ? "true" : "false";
            var path = $"/rest/workspaces/{workspaceName}/wmsstores/{wmsStoreName}?recurse={recurseValue}";
            await _httpClient.DeleteAsync(path);
        }
    }
}
