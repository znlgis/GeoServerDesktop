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
    /// 用于管理 GeoServer WMS 图层的服务 (cascaded WMS layers)
    /// </summary>
    public class WMSLayerService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 WMSLayerService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public WMSLayerService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets a list of WMS layers in a WMS store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStoreName">WMS 存储的名称</param>
        /// <returns>WMS 图层数组</returns>
        public async Task<WMSLayer[]> GetWMSLayersAsync(string workspaceName, string wmsStoreName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/wmsstores/{wmsStoreName}/wmslayers.json");
            var wrapper = JsonConvert.DeserializeObject<WMSLayerListWrapper>(response);
            return wrapper?.WMSLayerList?.WMSLayers ?? Array.Empty<WMSLayer>();
        }

        /// <summary>
        /// 获取特定 WMS 图层的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStoreName">WMS 存储的名称</param>
        /// <param name="wmsLayerName">WMS 图层的名称</param>
        /// <returns>WMS 图层详细信息</returns>
        public async Task<WMSLayer> GetWMSLayerAsync(string workspaceName, string wmsStoreName, string wmsLayerName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/wmsstores/{wmsStoreName}/wmslayers/{wmsLayerName}.json");
            var wrapper = JsonConvert.DeserializeObject<WMSLayerWrapper>(response);
            return wrapper?.WMSLayer;
        }

        /// <summary>
        /// 创建新的 WMS 图层 (publishes a layer from remote WMS)
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStoreName">WMS 存储的名称</param>
        /// <param name="wmsLayer">WMS 图层配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateWMSLayerAsync(string workspaceName, string wmsStoreName, WMSLayer wmsLayer)
        {
            var wrapper = new { wmsLayer = wmsLayer };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/workspaces/{workspaceName}/wmsstores/{wmsStoreName}/wmslayers", content);
        }

        /// <summary>
        /// 更新现有的 WMS 图层
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStoreName">WMS 存储的名称</param>
        /// <param name="wmsLayerName">WMS 图层的名称</param>
        /// <param name="wmsLayer">Updated WMS 图层配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateWMSLayerAsync(string workspaceName, string wmsStoreName, string wmsLayerName, WMSLayer wmsLayer)
        {
            var wrapper = new { wmsLayer = wmsLayer };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/wmsstores/{wmsStoreName}/wmslayers/{wmsLayerName}", content);
        }

        /// <summary>
        /// 删除 WMS 图层
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStoreName">WMS 存储的名称</param>
        /// <param name="wmsLayerName">WMS 图层的名称 to delete</param>
        /// <param name="recurse">Whether to recursively delete all resources associated with the WMS layer</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteWMSLayerAsync(string workspaceName, string wmsStoreName, string wmsLayerName, bool recurse = false)
        {
            var recurseValue = recurse ? "true" : "false";
            var path = $"/rest/workspaces/{workspaceName}/wmsstores/{wmsStoreName}/wmslayers/{wmsLayerName}?recurse={recurseValue}";
            await _httpClient.DeleteAsync(path);
        }
    }
}
