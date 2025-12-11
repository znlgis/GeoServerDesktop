using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于管理 GeoServer 覆盖范围的服务 (raster layers)
    /// </summary>
    public class CoverageService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 CoverageService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public CoverageService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets a list of coverages in a coverage store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store</param>
        /// <returns>覆盖范围数组</returns>
        public async Task<Coverage[]> GetCoveragesAsync(string workspaceName, string coverageStoreName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}/coverages.json");
            var wrapper = JsonConvert.DeserializeObject<CoverageListWrapper>(response);
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
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}/coverages/{coverageName}.json");
            var wrapper = JsonConvert.DeserializeObject<CoverageWrapper>(response);
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
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}/coverages", content);
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
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}/coverages/{coverageName}", content);
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
            var path = $"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}/coverages/{coverageName}?recurse={recurseValue}";
            await _httpClient.DeleteAsync(path);
        }
    }
}
