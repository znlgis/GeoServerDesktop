using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoServer coverage stores (raster data stores)
    /// </summary>
    public class CoverageStoreService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the CoverageStoreService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public CoverageStoreService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets a list of coverage stores in a workspace
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <returns>Array of coverage stores</returns>
        public async Task<CoverageStore[]> GetCoverageStoresAsync(string workspaceName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/coveragestores.json");
            var wrapper = JsonConvert.DeserializeObject<CoverageStoreListWrapper>(response);
            return wrapper?.CoverageStoreList?.CoverageStores ?? Array.Empty<CoverageStore>();
        }

        /// <summary>
        /// Gets details for a specific coverage store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="coverageStoreName">Name of the coverage store</param>
        /// <returns>Coverage store details</returns>
        public async Task<CoverageStore> GetCoverageStoreAsync(string workspaceName, string coverageStoreName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}.json");
            var wrapper = JsonConvert.DeserializeObject<CoverageStoreWrapper>(response);
            return wrapper?.CoverageStore;
        }

        /// <summary>
        /// Creates a new coverage store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="coverageStore">Coverage store configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task CreateCoverageStoreAsync(string workspaceName, CoverageStore coverageStore)
        {
            var wrapper = new { coverageStore = coverageStore };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/workspaces/{workspaceName}/coveragestores", content);
        }

        /// <summary>
        /// Updates an existing coverage store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="coverageStoreName">Name of the coverage store</param>
        /// <param name="coverageStore">Updated coverage store configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateCoverageStoreAsync(string workspaceName, string coverageStoreName, CoverageStore coverageStore)
        {
            var wrapper = new { coverageStore = coverageStore };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}", content);
        }

        /// <summary>
        /// Deletes a coverage store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="coverageStoreName">Name of the coverage store to delete</param>
        /// <param name="recurse">Whether to recursively delete all coverages in the store</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteCoverageStoreAsync(string workspaceName, string coverageStoreName, bool recurse = false)
        {
            var recurseValue = recurse ? "true" : "false";
            var path = $"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}?recurse={recurseValue}";
            await _httpClient.DeleteAsync(path);
        }

        /// <summary>
        /// Uploads a file to create or update a coverage store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="coverageStoreName">Name of the coverage store</param>
        /// <param name="fileContent">Content of the file to upload</param>
        /// <param name="extension">File extension (e.g., "geotiff", "worldimage")</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UploadCoverageFileAsync(string workspaceName, string coverageStoreName, byte[] fileContent, string extension)
        {
            var content = new ByteArrayContent(fileContent);
            content.Headers.Add("Content-Type", "application/octet-stream");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}/file.{extension}", content);
        }
    }
}
