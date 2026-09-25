using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// ICoverageService 的服务接口（M5 接口化）：与实现类 CoverageService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface ICoverageService
    {
        /// <summary>
        /// Gets a list of coverages in a coverage store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store</param>
        /// <returns>覆盖范围数组</returns>
        Task<Coverage[]> GetCoveragesAsync(string workspaceName, string coverageStoreName);

        /// <summary>
        /// 获取特定覆盖范围的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store</param>
        /// <param name="coverageName">覆盖范围的名称</param>
        /// <returns>覆盖范围详细信息</returns>
        Task<Coverage> GetCoverageAsync(string workspaceName, string coverageStoreName, string coverageName);

        /// <summary>
        /// Publishes a new coverage (creates a raster layer)
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store</param>
        /// <param name="coverage">覆盖范围配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateCoverageAsync(string workspaceName, string coverageStoreName, Coverage coverage);

        /// <summary>
        /// 更新现有的覆盖范围
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store</param>
        /// <param name="coverageName">覆盖范围的名称</param>
        /// <param name="coverage">更新的覆盖范围配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateCoverageAsync(string workspaceName, string coverageStoreName, string coverageName, Coverage coverage);

        /// <summary>
        /// 删除覆盖范围
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store</param>
        /// <param name="coverageName">覆盖范围的名称</param>
        /// <param name="recurse">Whether to recursively delete all resources associated with the coverage</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteCoverageAsync(string workspaceName, string coverageStoreName, string coverageName, bool recurse = false);
    }
}
