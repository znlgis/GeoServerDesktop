using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoWebCache blobstores
    /// </summary>
    public class BlobstoreService : ServiceBase
    {

        /// <summary>
        /// 初始化 BlobstoreService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public BlobstoreService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// Gets the list of all blobstores
        /// </summary>
        /// <returns>List of blobstores</returns>
        public async Task<BlobstoreListWrapper> GetBlobstoresAsync()
        {
            var response = await Http.GetAsync("/gwc/rest/blobstores.json");
            return JsonConvert.DeserializeObject<BlobstoreListWrapper>(response);
        }

        /// <summary>
        /// 获取特定 Blob 存储的详细信息
        /// </summary>
        /// <param name="blobstoreId">Blobstore ID</param>
        /// <returns>Blob 存储详细信息</returns>
        public async Task<BlobstoreWrapper> GetBlobstoreAsync(string blobstoreId)
        {
            var response = await Http.GetAsync($"/gwc/rest/blobstores/{Esc(blobstoreId)}.json");
            // FIXED-E11：单体实测根为实现类名 {"FileBlobStore":{...}}，动态键需 ParseSingle
            return new BlobstoreWrapper { Blobstore = Blobstore.ParseSingle(response, blobstoreId) };
        }

        /// <summary>
        /// 创建新的 Blob 存储
        /// </summary>
        /// <param name="blobstore">Blobstore to create</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateBlobstoreAsync(Blobstore blobstore)
        {
            // FIXED-E11：BlobStoreController 创建为 PUT /gwc/rest/blobstores/{name} + XStream XML
            // （{"FileBlobStore">...}，实测 201）；集合 POST JSON 不被接受。
            var xml = (blobstore ?? new Blobstore()).ToXmlBody(null);
            await PutContentAsync($"/gwc/rest/blobstores/{Esc(blobstore?.Id)}", TextContent(xml, "application/xml"));
        }

        /// <summary>
        /// 更新现有的 Blob 存储
        /// </summary>
        /// <param name="blobstoreId">Blobstore ID</param>
        /// <param name="blobstore">Updated blobstore information</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateBlobstoreAsync(string blobstoreId, Blobstore blobstore)
        {
            // FIXED-E11：更新与创建同路由 PUT /gwc/rest/blobstores/{name}，同样仅接受 XStream XML。
            var xml = (blobstore ?? new Blobstore()).ToXmlBody(blobstoreId);
            await PutContentAsync($"/gwc/rest/blobstores/{Esc(blobstoreId)}", TextContent(xml, "application/xml"));
        }

        /// <summary>
        /// 删除 Blob 存储
        /// </summary>
        /// <param name="blobstoreId">Blobstore ID to delete</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteBlobstoreAsync(string blobstoreId)
        {
            await Http.DeleteAsync($"/gwc/rest/blobstores/{Esc(blobstoreId)}");
        }
    }
}
