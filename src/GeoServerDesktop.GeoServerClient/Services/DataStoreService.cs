using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoServer data stores
    /// </summary>
    public class DataStoreService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the DataStoreService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public DataStoreService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets a list of data stores in a workspace
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <returns>Array of data stores</returns>
        public async Task<DataStore[]> GetDataStoresAsync(string workspaceName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/datastores.json");
            var wrapper = JsonConvert.DeserializeObject<DataStoreListWrapper>(response);
            return wrapper?.DataStoreList?.DataStores ?? new DataStore[0];
        }

        /// <summary>
        /// Gets details for a specific data store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="dataStoreName">Name of the data store</param>
        /// <returns>Data store details</returns>
        public async Task<DataStore> GetDataStoreAsync(string workspaceName, string dataStoreName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/datastores/{dataStoreName}.json");
            var wrapper = JsonConvert.DeserializeObject<DataStoreWrapper>(response);
            return wrapper?.DataStore;
        }

        /// <summary>
        /// Creates a new data store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="dataStore">Data store configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task CreateDataStoreAsync(string workspaceName, DataStore dataStore)
        {
            var wrapper = new { dataStore = dataStore };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/workspaces/{workspaceName}/datastores", content);
        }

        /// <summary>
        /// Updates an existing data store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="dataStoreName">Name of the data store</param>
        /// <param name="dataStore">Updated data store configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateDataStoreAsync(string workspaceName, string dataStoreName, DataStore dataStore)
        {
            var wrapper = new { dataStore = dataStore };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/datastores/{dataStoreName}", content);
        }

        /// <summary>
        /// Deletes a data store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="dataStoreName">Name of the data store to delete</param>
        /// <param name="recurse">Whether to recursively delete all resources in the data store</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteDataStoreAsync(string workspaceName, string dataStoreName, bool recurse = false)
        {
            var path = $"/rest/workspaces/{workspaceName}/datastores/{dataStoreName}?recurse={recurse.ToString().ToLower()}";
            await _httpClient.DeleteAsync(path);
        }

        /// <summary>
        /// Resets the data store cache
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="dataStoreName">Name of the data store</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task ResetDataStoreAsync(string workspaceName, string dataStoreName)
        {
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/datastores/{dataStoreName}/reset", null);
        }

        /// <summary>
        /// Uploads a file to the data store (e.g., shapefile, properties file)
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="dataStoreName">Name of the data store</param>
        /// <param name="fileFormat">File format extension (e.g., shp, properties)</param>
        /// <param name="fileContent">File content as byte array</param>
        /// <param name="contentType">Content type (e.g., application/zip for shapefiles)</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UploadFileAsync(string workspaceName, string dataStoreName, string fileFormat, byte[] fileContent, string contentType = "application/octet-stream")
        {
            var content = new ByteArrayContent(fileContent);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/datastores/{dataStoreName}/file.{fileFormat}", content);
        }
    }
}
