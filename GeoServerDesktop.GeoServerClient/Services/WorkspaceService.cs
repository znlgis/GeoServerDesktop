using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoServer workspaces
    /// </summary>
    public class WorkspaceService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the WorkspaceService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public WorkspaceService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets a list of all workspaces
        /// </summary>
        /// <returns>Array of workspaces</returns>
        public async Task<Workspace[]> GetWorkspacesAsync()
        {
            var response = await _httpClient.GetAsync("/rest/workspaces.json");
            var wrapper = JsonConvert.DeserializeObject<WorkspaceListWrapper>(response);
            return wrapper?.WorkspaceList?.Workspaces ?? new Workspace[0];
        }

        /// <summary>
        /// Gets details for a specific workspace
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <returns>Workspace details</returns>
        public async Task<Workspace> GetWorkspaceAsync(string workspaceName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}.json");
            var wrapper = JsonConvert.DeserializeObject<WorkspaceWrapper>(response);
            return wrapper?.Workspace;
        }

        /// <summary>
        /// Creates a new workspace
        /// </summary>
        /// <param name="workspaceName">Name of the workspace to create</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task CreateWorkspaceAsync(string workspaceName)
        {
            var workspace = new { workspace = new { name = workspaceName } };
            var json = JsonConvert.SerializeObject(workspace);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/rest/workspaces", content);
        }

        /// <summary>
        /// Deletes a workspace
        /// </summary>
        /// <param name="workspaceName">Name of the workspace to delete</param>
        /// <param name="recurse">Whether to recursively delete all resources in the workspace</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteWorkspaceAsync(string workspaceName, bool recurse = false)
        {
            var path = $"/rest/workspaces/{workspaceName}?recurse={recurse.ToString().ToLower()}";
            await _httpClient.DeleteAsync(path);
        }
    }
}
