using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoWebCache blobstores
    /// </summary>
    public class BlobstoreService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the BlobstoreService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public BlobstoreService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the list of all blobstores
        /// </summary>
        /// <returns>List of blobstores</returns>
        public async Task<BlobstoreListWrapper> GetBlobstoresAsync()
        {
            var response = await _httpClient.GetAsync("/gwc/rest/blobstores.json");
            return JsonConvert.DeserializeObject<BlobstoreListWrapper>(response);
        }

        /// <summary>
        /// Gets details for a specific blobstore
        /// </summary>
        /// <param name="blobstoreId">Blobstore ID</param>
        /// <returns>Blobstore details</returns>
        public async Task<BlobstoreWrapper> GetBlobstoreAsync(string blobstoreId)
        {
            var response = await _httpClient.GetAsync($"/gwc/rest/blobstores/{blobstoreId}.json");
            return JsonConvert.DeserializeObject<BlobstoreWrapper>(response);
        }

        /// <summary>
        /// Creates a new blobstore
        /// </summary>
        /// <param name="blobstore">Blobstore to create</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task CreateBlobstoreAsync(Blobstore blobstore)
        {
            var wrapper = new { blobstore = blobstore };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/gwc/rest/blobstores", content);
        }

        /// <summary>
        /// Updates an existing blobstore
        /// </summary>
        /// <param name="blobstoreId">Blobstore ID</param>
        /// <param name="blobstore">Updated blobstore information</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateBlobstoreAsync(string blobstoreId, Blobstore blobstore)
        {
            var wrapper = new { blobstore = blobstore };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/gwc/rest/blobstores/{blobstoreId}", content);
        }

        /// <summary>
        /// Deletes a blobstore
        /// </summary>
        /// <param name="blobstoreId">Blobstore ID to delete</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteBlobstoreAsync(string blobstoreId)
        {
            await _httpClient.DeleteAsync($"/gwc/rest/blobstores/{blobstoreId}");
        }
    }
}
