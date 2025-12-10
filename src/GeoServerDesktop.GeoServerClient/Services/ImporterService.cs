using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing bulk data imports
    /// </summary>
    public class ImporterService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the ImporterService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public ImporterService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Creates a new import context
        /// </summary>
        /// <param name="targetWorkspace">Target workspace name</param>
        /// <param name="targetStore">Optional target store name</param>
        /// <returns>Import context</returns>
        public async Task<ImportContextWrapper> CreateImportAsync(string targetWorkspace, string targetStore = null)
        {
            var import = new
            {
                import = new
                {
                    targetWorkspace = new { name = targetWorkspace },
                    targetStore = targetStore != null ? new { name = targetStore } : null
                }
            };
            var json = JsonConvert.SerializeObject(import);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/rest/imports", content);
            return JsonConvert.DeserializeObject<ImportContextWrapper>(response);
        }

        /// <summary>
        /// Gets an import context
        /// </summary>
        /// <param name="importId">Import ID</param>
        /// <returns>Import context</returns>
        public async Task<ImportContextWrapper> GetImportAsync(int importId)
        {
            var response = await _httpClient.GetAsync($"/rest/imports/{importId}.json");
            return JsonConvert.DeserializeObject<ImportContextWrapper>(response);
        }

        /// <summary>
        /// Deletes an import context
        /// </summary>
        /// <param name="importId">Import ID</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteImportAsync(int importId)
        {
            await _httpClient.DeleteAsync($"/rest/imports/{importId}");
        }

        /// <summary>
        /// Gets the tasks for an import
        /// </summary>
        /// <param name="importId">Import ID</param>
        /// <returns>List of tasks</returns>
        public async Task<string> GetImportTasksAsync(int importId)
        {
            return await _httpClient.GetAsync($"/rest/imports/{importId}/tasks.json");
        }

        /// <summary>
        /// Uploads data to an import task
        /// </summary>
        /// <param name="importId">Import ID</param>
        /// <param name="taskId">Task ID</param>
        /// <param name="data">Data to upload</param>
        /// <param name="contentType">Content type</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UploadDataAsync(int importId, int taskId, byte[] data, string contentType = "application/octet-stream")
        {
            var content = new ByteArrayContent(data);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            await _httpClient.PutAsync($"/rest/imports/{importId}/tasks/{taskId}/data", content);
        }
    }
}
