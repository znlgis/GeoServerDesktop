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
    /// Service for managing GeoServer coverages (raster layers)
    /// </summary>
    public class CoverageService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the CoverageService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public CoverageService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets a list of coverages in a coverage store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="coverageStoreName">Name of the coverage store</param>
        /// <returns>Array of coverages</returns>
        public async Task<Coverage[]> GetCoveragesAsync(string workspaceName, string coverageStoreName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}/coverages.json");
            var wrapper = JsonConvert.DeserializeObject<CoverageListWrapper>(response);
            return wrapper?.CoverageList?.Coverages ?? Array.Empty<Coverage>();
        }

        /// <summary>
        /// Gets details for a specific coverage
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="coverageStoreName">Name of the coverage store</param>
        /// <param name="coverageName">Name of the coverage</param>
        /// <returns>Coverage details</returns>
        public async Task<Coverage> GetCoverageAsync(string workspaceName, string coverageStoreName, string coverageName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}/coverages/{coverageName}.json");
            var wrapper = JsonConvert.DeserializeObject<CoverageWrapper>(response);
            return wrapper?.Coverage;
        }

        /// <summary>
        /// Publishes a new coverage (creates a raster layer)
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="coverageStoreName">Name of the coverage store</param>
        /// <param name="coverage">Coverage configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task CreateCoverageAsync(string workspaceName, string coverageStoreName, Coverage coverage)
        {
            var wrapper = new { coverage = coverage };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}/coverages", content);
        }

        /// <summary>
        /// Updates an existing coverage
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="coverageStoreName">Name of the coverage store</param>
        /// <param name="coverageName">Name of the coverage</param>
        /// <param name="coverage">Updated coverage configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateCoverageAsync(string workspaceName, string coverageStoreName, string coverageName, Coverage coverage)
        {
            var wrapper = new { coverage = coverage };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}/coverages/{coverageName}", content);
        }

        /// <summary>
        /// Deletes a coverage
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="coverageStoreName">Name of the coverage store</param>
        /// <param name="coverageName">Name of the coverage to delete</param>
        /// <param name="recurse">Whether to recursively delete all resources associated with the coverage</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteCoverageAsync(string workspaceName, string coverageStoreName, string coverageName, bool recurse = false)
        {
            var recurseValue = recurse ? "true" : "false";
            var path = $"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}/coverages/{coverageName}?recurse={recurseValue}";
            await _httpClient.DeleteAsync(path);
        }
    }
}
