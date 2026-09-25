using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IStructuredCoverageService 的服务接口（M5 接口化）：与实现类 StructuredCoverageService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IStructuredCoverageService
    {
        /// <summary>
        /// Gets the coverage index information
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageStore">Coverage store name</param>
        /// <param name="coverage">Coverage name</param>
        /// <returns>Coverage index information</returns>
        Task<StructuredCoverageIndex> GetIndexAsync(string workspace, string coverageStore, string coverage);

        /// <summary>
        /// Updates the coverage index configuration
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageStore">Coverage store name</param>
        /// <param name="coverage">Coverage name</param>
        /// <param name="index">Index configuration</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateIndexAsync(string workspace, string coverageStore, string coverage, StructuredCoverageIndex index);

        /// <summary>
        /// Gets the list of granules for a coverage
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageStore">Coverage store name</param>
        /// <param name="coverage">Coverage name</param>
        /// <param name="filter">Optional filter parameter</param>
        /// <param name="offset">Optional offset for pagination</param>
        /// <param name="limit">Optional limit for pagination</param>
        /// <returns>List of granules</returns>
        Task<GranuleListWrapper> GetGranulesAsync(string workspace, string coverageStore, string coverage, string filter = null, int? offset = null, int? limit = null);

        /// <summary>
        /// Gets details for a specific granule
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageStore">Coverage store name</param>
        /// <param name="coverage">Coverage name</param>
        /// <param name="granuleId">Granule ID</param>
        /// <returns>Granule details</returns>
        Task<Granule> GetGranuleAsync(string workspace, string coverageStore, string coverage, string granuleId);

        /// <summary>
        /// Deletes a specific granule
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageStore">Coverage store name</param>
        /// <param name="coverage">Coverage name</param>
        /// <param name="granuleId">Granule ID to delete</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteGranuleAsync(string workspace, string coverageStore, string coverage, string granuleId);

        /// <summary>
        /// Harvests (adds) new granules to the coverage
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageStore">Coverage store name</param>
        /// <param name="coverage">Coverage name</param>
        /// <param name="files">File paths or URLs to harvest</param>
        /// <returns>表示异步操作的任务</returns>
        Task HarvestGranulesAsync(string workspace, string coverageStore, string coverage, string[] files);
    }
}
