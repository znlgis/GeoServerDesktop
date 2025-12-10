using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoServer WMTS stores (cascaded WMTS services)
    /// </summary>
    public class WMTSStoreService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 WMTSStoreService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public WMTSStoreService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// 获取工作空间中的 WMTS 存储列表
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <returns>WMTS 存储数组</returns>
        public async Task<WMTSStore[]> GetWMTSStoresAsync(string workspaceName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/wmtsstores.json");
            var wrapper = JsonConvert.DeserializeObject<WMTSStoreListWrapper>(response);
            return wrapper?.WMTSStoreList?.WMTSStores ?? Array.Empty<WMTSStore>();
        }

        /// <summary>
        /// 获取特定 WMTS 存储的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStoreName">WMTS 存储的名称</param>
        /// <returns>WMTS 存储详细信息</returns>
        public async Task<WMTSStore> GetWMTSStoreAsync(string workspaceName, string wmtsStoreName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/wmtsstores/{wmtsStoreName}.json");
            var wrapper = JsonConvert.DeserializeObject<WMTSStoreWrapper>(response);
            return wrapper?.WMTSStore;
        }

        /// <summary>
        /// 创建新的 WMTS 存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStore">WMTS 存储配置</param>
        /// <returns>表示异步操作的任务</returns>
        /// <remarks>
        /// The WMTS store enables cascading of a remote WMTS service.
        /// The capabilitiesURL should point to a valid WMTS GetCapabilities document.
        /// </remarks>
        public async Task CreateWMTSStoreAsync(string workspaceName, WMTSStore wmtsStore)
        {
            var wrapper = new { wmtsStore = wmtsStore };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/workspaces/{workspaceName}/wmtsstores", content);
        }

        /// <summary>
        /// 更新现有的 WMTS 存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStoreName">WMTS 存储的名称</param>
        /// <param name="wmtsStore">Updated WMTS 存储配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateWMTSStoreAsync(string workspaceName, string wmtsStoreName, WMTSStore wmtsStore)
        {
            var wrapper = new { wmtsStore = wmtsStore };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/wmtsstores/{wmtsStoreName}", content);
        }

        /// <summary>
        /// 删除 WMTS 存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStoreName">WMTS 存储的名称 to delete</param>
        /// <param name="recurse">Whether to recursively delete all WMTS layers in the store</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteWMTSStoreAsync(string workspaceName, string wmtsStoreName, bool recurse = false)
        {
            var recurseValue = recurse ? "true" : "false";
            var path = $"/rest/workspaces/{workspaceName}/wmtsstores/{wmtsStoreName}?recurse={recurseValue}";
            await _httpClient.DeleteAsync(path);
        }
    }
}
