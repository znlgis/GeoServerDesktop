using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IBlobstoreService 的服务接口（M5 接口化）：与实现类 BlobstoreService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IBlobstoreService
    {
        /// <summary>
        /// Gets the list of all blobstores
        /// </summary>
        /// <returns>List of blobstores</returns>
        Task<BlobstoreListWrapper> GetBlobstoresAsync();

        /// <summary>
        /// 获取特定 Blob 存储的详细信息
        /// </summary>
        /// <param name="blobstoreId">Blobstore ID</param>
        /// <returns>Blob 存储详细信息</returns>
        Task<BlobstoreWrapper> GetBlobstoreAsync(string blobstoreId);

        /// <summary>
        /// 创建新的 Blob 存储
        /// </summary>
        /// <param name="blobstore">Blobstore to create</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateBlobstoreAsync(Blobstore blobstore);

        /// <summary>
        /// 更新现有的 Blob 存储
        /// </summary>
        /// <param name="blobstoreId">Blobstore ID</param>
        /// <param name="blobstore">Updated blobstore information</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateBlobstoreAsync(string blobstoreId, Blobstore blobstore);

        /// <summary>
        /// 删除 Blob 存储
        /// </summary>
        /// <param name="blobstoreId">Blobstore ID to delete</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteBlobstoreAsync(string blobstoreId);
    }
}
