using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing coverage views
    /// </summary>
    public class CoverageViewService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the CoverageViewService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public CoverageViewService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the list of all coverage views in a workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <returns>List of coverage views</returns>
        public async Task<CoverageViewListWrapper> GetCoverageViewsAsync(string workspace)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspace}/coverageviews.json");
            return JsonConvert.DeserializeObject<CoverageViewListWrapper>(response);
        }

        /// <summary>
        /// Gets details for a specific coverage view
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageView">Coverage view name</param>
        /// <returns>Coverage view details</returns>
        public async Task<CoverageViewWrapper> GetCoverageViewAsync(string workspace, string coverageView)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspace}/coverageviews/{coverageView}.json");
            return JsonConvert.DeserializeObject<CoverageViewWrapper>(response);
        }

        /// <summary>
        /// Creates a new coverage view
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageView">Coverage view to create</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task CreateCoverageViewAsync(string workspace, CoverageView coverageView)
        {
            var wrapper = new { coverageView = coverageView };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/workspaces/{workspace}/coverageviews", content);
        }

        /// <summary>
        /// Updates an existing coverage view
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageViewName">Coverage view name</param>
        /// <param name="coverageView">Updated coverage view information</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateCoverageViewAsync(string workspace, string coverageViewName, CoverageView coverageView)
        {
            var wrapper = new { coverageView = coverageView };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/workspaces/{workspace}/coverageviews/{coverageViewName}", content);
        }

        /// <summary>
        /// Deletes a coverage view
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageView">Coverage view name to delete</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteCoverageViewAsync(string workspace, string coverageView)
        {
            await _httpClient.DeleteAsync($"/rest/workspaces/{workspace}/coverageviews/{coverageView}");
        }
    }
}
