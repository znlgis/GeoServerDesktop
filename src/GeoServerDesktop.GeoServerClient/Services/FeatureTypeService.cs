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
    /// 用于管理 GeoServer 要素类型的服务
    /// </summary>
    public class FeatureTypeService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 FeatureTypeService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public FeatureTypeService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// 获取数据存储中的要素类型列表
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <returns>要素类型数组</returns>
        public async Task<FeatureType[]> GetFeatureTypesAsync(string workspaceName, string dataStoreName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/datastores/{dataStoreName}/featuretypes.json");
            var wrapper = JsonConvert.DeserializeObject<FeatureTypeListWrapper>(response);
            return wrapper?.FeatureTypeList?.FeatureTypes ?? Array.Empty<FeatureType>();
        }

        /// <summary>
        /// 获取特定要素类型的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <param name="featureTypeName">要素类型的名称</param>
        /// <returns>要素类型详细信息</returns>
        public async Task<FeatureType> GetFeatureTypeAsync(string workspaceName, string dataStoreName, string featureTypeName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/datastores/{dataStoreName}/featuretypes/{featureTypeName}.json");
            var wrapper = JsonConvert.DeserializeObject<FeatureTypeWrapper>(response);
            return wrapper?.FeatureType;
        }

        /// <summary>
        /// 发布新的要素类型（从要素类型创建图层）
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <param name="featureType">要素类型配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateFeatureTypeAsync(string workspaceName, string dataStoreName, FeatureType featureType)
        {
            var wrapper = new { featureType = featureType };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/workspaces/{workspaceName}/datastores/{dataStoreName}/featuretypes", content);
        }

        /// <summary>
        /// 更新现有的要素类型
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <param name="featureTypeName">要素类型的名称</param>
        /// <param name="featureType">更新后的要素类型配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateFeatureTypeAsync(string workspaceName, string dataStoreName, string featureTypeName, FeatureType featureType)
        {
            var wrapper = new { featureType = featureType };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/datastores/{dataStoreName}/featuretypes/{featureTypeName}", content);
        }

        /// <summary>
        /// 删除要素类型
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <param name="featureTypeName">要删除的要素类型名称</param>
        /// <param name="recurse">是否递归删除与要素类型相关的所有资源</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteFeatureTypeAsync(string workspaceName, string dataStoreName, string featureTypeName, bool recurse = false)
        {
            var path = $"/rest/workspaces/{workspaceName}/datastores/{dataStoreName}/featuretypes/{featureTypeName}?recurse={recurse.ToString().ToLowerInvariant()}";
            await _httpClient.DeleteAsync(path);
        }
    }
}
