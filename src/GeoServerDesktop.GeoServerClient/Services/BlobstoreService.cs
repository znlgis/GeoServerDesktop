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
        /// 初始化 BlobstoreService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
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
        /// 获取特定 Blob 存储的详细信息
        /// </summary>
        /// <param name="blobstoreId">Blobstore ID</param>
        /// <returns>Blob 存储详细信息</returns>
        public async Task<BlobstoreWrapper> GetBlobstoreAsync(string blobstoreId)
        {
            var response = await _httpClient.GetAsync($"/gwc/rest/blobstores/{blobstoreId}.json");
            return JsonConvert.DeserializeObject<BlobstoreWrapper>(response);
        }

        /// <summary>
        /// 创建新的 Blob 存储
        /// </summary>
        /// <param name="blobstore">Blobstore to create</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateBlobstoreAsync(Blobstore blobstore)
        {
            var wrapper = new { blobstore = blobstore };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/gwc/rest/blobstores", content);
        }

        /// <summary>
        /// 更新现有的 Blob 存储
        /// </summary>
        /// <param name="blobstoreId">Blobstore ID</param>
        /// <param name="blobstore">Updated blobstore information</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateBlobstoreAsync(string blobstoreId, Blobstore blobstore)
        {
            var wrapper = new { blobstore = blobstore };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/gwc/rest/blobstores/{blobstoreId}", content);
        }

        /// <summary>
        /// 删除 Blob 存储
        /// </summary>
        /// <param name="blobstoreId">Blobstore ID to delete</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteBlobstoreAsync(string blobstoreId)
        {
            await _httpClient.DeleteAsync($"/gwc/rest/blobstores/{blobstoreId}");
        }
    }
}
