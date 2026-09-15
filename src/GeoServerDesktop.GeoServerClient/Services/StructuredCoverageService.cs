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
    /// Service for managing structured coverage indexes and granules。
    /// 注：3.0.1 默认镜像未安装 structured coverage observer 扩展（相关路由 404），
    /// E5/E21 等形态基线维持不变，安装扩展后需按实测复核。
    /// </summary>
    public class StructuredCoverageService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 StructuredCoverageService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public StructuredCoverageService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Gets the coverage index information
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageStore">Coverage store name</param>
        /// <param name="coverage">Coverage name</param>
        /// <returns>Coverage index information</returns>
        public async Task<StructuredCoverageIndex> GetIndexAsync(string workspace, string coverageStore, string coverage)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspace}/coveragestores/{coverageStore}/coverages/{coverage}/index.json");
            return JsonConvert.DeserializeObject<StructuredCoverageIndex>(response);
        }

        /// <summary>
        /// Updates the coverage index configuration
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageStore">Coverage store name</param>
        /// <param name="coverage">Coverage name</param>
        /// <param name="index">Index configuration</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateIndexAsync(string workspace, string coverageStore, string coverage, StructuredCoverageIndex index)
        {
            var json = JsonConvert.SerializeObject(index);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                await _httpClient.PutAsync($"/rest/workspaces/{workspace}/coveragestores/{coverageStore}/coverages/{coverage}/index", content);
            }
        }

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
        public async Task<GranuleListWrapper> GetGranulesAsync(string workspace, string coverageStore, string coverage, string filter = null, int? offset = null, int? limit = null)
        {
            var queryParams = new System.Collections.Generic.List<string>();
            // FIXED-E24：filter 为 CQL 表达式（含空格/&/>/中文），必须转义否则破坏查询串
            if (!string.IsNullOrEmpty(filter)) queryParams.Add($"filter={Uri.EscapeDataString(filter)}");
            if (offset.HasValue) queryParams.Add($"offset={offset.Value}");
            if (limit.HasValue) queryParams.Add($"limit={limit.Value}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspace}/coveragestores/{coverageStore}/coverages/{coverage}/index/granules.json{queryString}");
            return JsonConvert.DeserializeObject<GranuleListWrapper>(response);
        }

        /// <summary>
        /// Gets details for a specific granule
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageStore">Coverage store name</param>
        /// <param name="coverage">Coverage name</param>
        /// <param name="granuleId">Granule ID</param>
        /// <returns>Granule details</returns>
        public async Task<Granule> GetGranuleAsync(string workspace, string coverageStore, string coverage, string granuleId)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspace}/coveragestores/{coverageStore}/coverages/{coverage}/index/granules/{granuleId}.json");
            return JsonConvert.DeserializeObject<Granule>(response);
        }

        /// <summary>
        /// Deletes a specific granule
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageStore">Coverage store name</param>
        /// <param name="coverage">Coverage name</param>
        /// <param name="granuleId">Granule ID to delete</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteGranuleAsync(string workspace, string coverageStore, string coverage, string granuleId)
        {
            await _httpClient.DeleteAsync($"/rest/workspaces/{workspace}/coveragestores/{coverageStore}/coverages/{coverage}/index/granules/{granuleId}");
        }

        /// <summary>
        /// Harvests (adds) new granules to the coverage
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageStore">Coverage store name</param>
        /// <param name="coverage">Coverage name</param>
        /// <param name="files">File paths or URLs to harvest</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task HarvestGranulesAsync(string workspace, string coverageStore, string coverage, string[] files)
        {
            var request = new { files = files };
            var json = JsonConvert.SerializeObject(request);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                await _httpClient.PostAsync($"/rest/workspaces/{workspace}/coveragestores/{coverageStore}/coverages/{coverage}/index/granules", content);
            }
        }
    }
}
