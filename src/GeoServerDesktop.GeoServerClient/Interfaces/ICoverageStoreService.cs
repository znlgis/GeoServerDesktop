using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// ICoverageStoreService 的服务接口（M5 接口化）：与实现类 CoverageStoreService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface ICoverageStoreService
    {
        /// <summary>
        /// 获取工作空间中的覆盖范围存储列表
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <returns>覆盖范围存储数组</returns>
        Task<CoverageStore[]> GetCoverageStoresAsync(string workspaceName);

        /// <summary>
        /// 获取特定覆盖范围的详细信息 store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store</param>
        /// <returns>覆盖范围存储详细信息</returns>
        Task<CoverageStore> GetCoverageStoreAsync(string workspaceName, string coverageStoreName);

        /// <summary>
        /// 创建新的覆盖范围 store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStore">覆盖范围存储配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateCoverageStoreAsync(string workspaceName, CoverageStore coverageStore);

        /// <summary>
        /// 更新现有的覆盖范围 store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store</param>
        /// <param name="coverageStore">更新的覆盖范围存储配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateCoverageStoreAsync(string workspaceName, string coverageStoreName, CoverageStore coverageStore);

        /// <summary>
        /// 删除覆盖范围 store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围存储的名称</param>
        /// <param name="recurse">Whether to recursively delete all coverages in the store</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteCoverageStoreAsync(string workspaceName, string coverageStoreName, bool recurse = false);

        /// <summary>
        /// Uploads a file to create or update a coverage store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store</param>
        /// <param name="fileContent">Content of the file to upload</param>
        /// <param name="extension">File extension (e.g., "geotiff", "worldimage")</param>
        /// <returns>表示异步操作的任务</returns>
        Task UploadCoverageFileAsync(string workspaceName, string coverageStoreName, byte[] fileContent, string extension);
    }
}
