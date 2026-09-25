using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于管理 GeoServer 覆盖范围的服务 (raster layers)
    /// </summary>
    public class CoverageService : ServiceBase
    {

        /// <summary>
        /// 初始化 CoverageService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public CoverageService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// Gets a list of coverages in a coverage store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store</param>
        /// <returns>覆盖范围数组</returns>
        public async Task<Coverage[]> GetCoveragesAsync(string workspaceName, string coverageStoreName)
        {
            var wrapper = await GetJsonAsync<CoverageListWrapper>($"/rest/workspaces/{Esc(workspaceName)}/coveragestores/{Esc(coverageStoreName)}/coverages.json");
            return wrapper?.CoverageList?.Coverages ?? Array.Empty<Coverage>();
        }

        /// <summary>
        /// 获取特定覆盖范围的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store</param>
        /// <param name="coverageName">覆盖范围的名称</param>
        /// <returns>覆盖范围详细信息</returns>
        public async Task<Coverage> GetCoverageAsync(string workspaceName, string coverageStoreName, string coverageName)
        {
            var wrapper = await GetJsonAsync<CoverageWrapper>($"/rest/workspaces/{Esc(workspaceName)}/coveragestores/{Esc(coverageStoreName)}/coverages/{Esc(coverageName)}.json");
            return wrapper?.Coverage;
        }

        /// <summary>
        /// Publishes a new coverage (creates a raster layer)
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store</param>
        /// <param name="coverage">覆盖范围配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateCoverageAsync(string workspaceName, string coverageStoreName, Coverage coverage)
        {
            var wrapper = new { coverage = coverage };
            await PostJsonAsync($"/rest/workspaces/{Esc(workspaceName)}/coveragestores/{Esc(coverageStoreName)}/coverages", wrapper);
        }

        /// <summary>
        /// 更新现有的覆盖范围
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store</param>
        /// <param name="coverageName">覆盖范围的名称</param>
        /// <param name="coverage">更新的覆盖范围配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateCoverageAsync(string workspaceName, string coverageStoreName, string coverageName, Coverage coverage)
        {
            var wrapper = new { coverage = coverage };
            await PutJsonAsync($"/rest/workspaces/{Esc(workspaceName)}/coveragestores/{Esc(coverageStoreName)}/coverages/{Esc(coverageName)}", wrapper);
        }

        /// <summary>
        /// 删除覆盖范围
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store</param>
        /// <param name="coverageName">覆盖范围的名称</param>
        /// <param name="recurse">Whether to recursively delete all resources associated with the coverage</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteCoverageAsync(string workspaceName, string coverageStoreName, string coverageName, bool recurse = false)
        {
            var recurseValue = recurse ? "true" : "false";
            var path = $"/rest/workspaces/{Esc(workspaceName)}/coveragestores/{Esc(coverageStoreName)}/coverages/{Esc(coverageName)}?recurse={recurseValue}";
            await Http.DeleteAsync(path);
        }
    }
}
