using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于管理 GeoServer 覆盖范围存储的服务 (raster data stores)
    /// </summary>
    public class CoverageStoreService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 CoverageStoreService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public CoverageStoreService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// 获取工作空间中的覆盖范围存储列表
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <returns>覆盖范围存储数组</returns>
        public async Task<CoverageStore[]> GetCoverageStoresAsync(string workspaceName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/coveragestores.json");
            var wrapper = JsonConvert.DeserializeObject<CoverageStoreListWrapper>(response);
            return wrapper?.CoverageStoreList?.CoverageStores ?? Array.Empty<CoverageStore>();
        }

        /// <summary>
        /// 获取特定覆盖范围的详细信息 store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store</param>
        /// <returns>覆盖范围存储详细信息</returns>
        public async Task<CoverageStore> GetCoverageStoreAsync(string workspaceName, string coverageStoreName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}.json");
            var wrapper = JsonConvert.DeserializeObject<CoverageStoreWrapper>(response);
            return wrapper?.CoverageStore;
        }

        /// <summary>
        /// 创建新的覆盖范围 store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStore">覆盖范围存储配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateCoverageStoreAsync(string workspaceName, CoverageStore coverageStore)
        {
            var wrapper = new { coverageStore = coverageStore };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/workspaces/{workspaceName}/coveragestores", content);
        }

        /// <summary>
        /// 更新现有的覆盖范围 store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store</param>
        /// <param name="coverageStore">更新的覆盖范围存储配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateCoverageStoreAsync(string workspaceName, string coverageStoreName, CoverageStore coverageStore)
        {
            var wrapper = new { coverageStore = coverageStore };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}", content);
        }

        /// <summary>
        /// 删除覆盖范围 store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store to delete</param>
        /// <param name="recurse">Whether to recursively delete all coverages in the store</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteCoverageStoreAsync(string workspaceName, string coverageStoreName, bool recurse = false)
        {
            var recurseValue = recurse ? "true" : "false";
            var path = $"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}?recurse={recurseValue}";
            await _httpClient.DeleteAsync(path);
        }

        /// <summary>
        /// Uploads a file to create or update a coverage store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="coverageStoreName">覆盖范围的名称 store</param>
        /// <param name="fileContent">Content of the file to upload</param>
        /// <param name="extension">File extension (e.g., "geotiff", "worldimage")</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UploadCoverageFileAsync(string workspaceName, string coverageStoreName, byte[] fileContent, string extension)
        {
            var content = new ByteArrayContent(fileContent);
            content.Headers.Add("Content-Type", "application/octet-stream");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/coveragestores/{coverageStoreName}/file.{extension}", content);
        }
    }
}
