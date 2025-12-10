using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于管理 GeoServer 图层的服务
    /// </summary>
    public class LayerService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 LayerService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public LayerService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// 获取所有图层的列表
        /// </summary>
        /// <returns>图层数组</returns>
        public async Task<Layer[]> GetLayersAsync()
        {
            var response = await _httpClient.GetAsync("/rest/layers.json");
            var wrapper = JsonConvert.DeserializeObject<LayerListWrapper>(response);
            return wrapper?.LayerList?.Layers ?? new Layer[0];
        }

        /// <summary>
        /// 获取特定图层的详细信息
        /// </summary>
        /// <param name="layerName">图层的名称</param>
        /// <returns>图层详细信息</returns>
        public async Task<Layer> GetLayerAsync(string layerName)
        {
            var response = await _httpClient.GetAsync($"/rest/layers/{layerName}.json");
            var wrapper = JsonConvert.DeserializeObject<LayerWrapper>(response);
            return wrapper?.Layer;
        }

        /// <summary>
        /// 更新现有的图层
        /// </summary>
        /// <param name="layerName">图层的名称</param>
        /// <param name="layer">更新后的图层配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateLayerAsync(string layerName, Layer layer)
        {
            var wrapper = new { layer = layer };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/layers/{layerName}", content);
        }

        /// <summary>
        /// 删除图层
        /// </summary>
        /// <param name="layerName">要删除的图层名称</param>
        /// <param name="recurse">是否递归删除与图层相关的所有资源</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteLayerAsync(string layerName, bool recurse = false)
        {
            var path = $"/rest/layers/{layerName}?recurse={recurse.ToString().ToLower()}";
            await _httpClient.DeleteAsync(path);
        }

        /// <summary>
        /// 获取特定工作空间中的图层列表
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <returns>工作空间中的图层数组</returns>
        public async Task<Layer[]> GetWorkspaceLayersAsync(string workspaceName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/layers.json");
            var wrapper = JsonConvert.DeserializeObject<LayerListWrapper>(response);
            return wrapper?.LayerList?.Layers ?? new Layer[0];
        }

        /// <summary>
        /// 获取工作空间中特定图层的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="layerName">图层的名称</param>
        /// <returns>图层详细信息</returns>
        public async Task<Layer> GetWorkspaceLayerAsync(string workspaceName, string layerName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/layers/{layerName}.json");
            var wrapper = JsonConvert.DeserializeObject<LayerWrapper>(response);
            return wrapper?.Layer;
        }
    }
}
