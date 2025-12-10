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
    /// 用于管理 GeoServer WMTS 图层的服务 (layers from cascaded WMTS services)
    /// </summary>
    public class WMTSLayerService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 WMTSLayerService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public WMTSLayerService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets a list of WMTS layers in a WMTS store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStoreName">WMTS 存储的名称</param>
        /// <returns>WMTS 图层数组</returns>
        public async Task<WMTSLayer[]> GetWMTSLayersAsync(string workspaceName, string wmtsStoreName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/wmtsstores/{wmtsStoreName}/wmtslayers.json");
            var wrapper = JsonConvert.DeserializeObject<WMTSLayerListWrapper>(response);
            return wrapper?.WMTSLayerList?.WMTSLayers ?? Array.Empty<WMTSLayer>();
        }

        /// <summary>
        /// 获取特定 WMTS 图层的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStoreName">WMTS 存储的名称</param>
        /// <param name="wmtsLayerName">WMTS 图层的名称</param>
        /// <returns>WMTS 图层详细信息</returns>
        public async Task<WMTSLayer> GetWMTSLayerAsync(string workspaceName, string wmtsStoreName, string wmtsLayerName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/wmtsstores/{wmtsStoreName}/wmtslayers/{wmtsLayerName}.json");
            var wrapper = JsonConvert.DeserializeObject<WMTSLayerWrapper>(response);
            return wrapper?.WMTSLayer;
        }

        /// <summary>
        /// Publishes a new WMTS layer from the cascaded WMTS store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStoreName">WMTS 存储的名称</param>
        /// <param name="wmtsLayer">WMTS 图层配置</param>
        /// <returns>表示异步操作的任务</returns>
        /// <remarks>
        /// The nativeName should match a layer name available in the remote WMTS service.
        /// </remarks>
        public async Task CreateWMTSLayerAsync(string workspaceName, string wmtsStoreName, WMTSLayer wmtsLayer)
        {
            var wrapper = new { wmtsLayer = wmtsLayer };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/workspaces/{workspaceName}/wmtsstores/{wmtsStoreName}/wmtslayers", content);
        }

        /// <summary>
        /// 更新现有的 WMTS 图层
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStoreName">WMTS 存储的名称</param>
        /// <param name="wmtsLayerName">WMTS 图层的名称</param>
        /// <param name="wmtsLayer">Updated WMTS 图层配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateWMTSLayerAsync(string workspaceName, string wmtsStoreName, string wmtsLayerName, WMTSLayer wmtsLayer)
        {
            var wrapper = new { wmtsLayer = wmtsLayer };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/wmtsstores/{wmtsStoreName}/wmtslayers/{wmtsLayerName}", content);
        }

        /// <summary>
        /// 删除 WMTS 图层
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStoreName">WMTS 存储的名称</param>
        /// <param name="wmtsLayerName">WMTS 图层的名称</param>
        /// <param name="recurse">Whether to recursively delete all resources associated with the WMTS layer</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteWMTSLayerAsync(string workspaceName, string wmtsStoreName, string wmtsLayerName, bool recurse = false)
        {
            var recurseValue = recurse ? "true" : "false";
            var path = $"/rest/workspaces/{workspaceName}/wmtsstores/{wmtsStoreName}/wmtslayers/{wmtsLayerName}?recurse={recurseValue}";
            await _httpClient.DeleteAsync(path);
        }
    }
}
