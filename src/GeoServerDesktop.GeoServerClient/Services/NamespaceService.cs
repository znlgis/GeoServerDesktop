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
    /// 用于管理 GeoServer 命名空间的服务
    /// </summary>
    public class NamespaceService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 NamespaceService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public NamespaceService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// 获取所有命名空间的列表
        /// </summary>
        /// <returns>命名空间数组</returns>
        public async Task<Namespace[]> GetNamespacesAsync()
        {
            var response = await _httpClient.GetAsync("/rest/namespaces.json");
            var wrapper = JsonConvert.DeserializeObject<NamespaceListWrapper>(response);
            return wrapper?.NamespaceList?.Namespaces ?? Array.Empty<Namespace>();
        }

        /// <summary>
        /// 获取特定命名空间的详细信息
        /// </summary>
        /// <param name="namespacePrefix">命名空间的前缀</param>
        /// <returns>命名空间详细信息</returns>
        public async Task<Namespace> GetNamespaceAsync(string namespacePrefix)
        {
            var response = await _httpClient.GetAsync($"/rest/namespaces/{namespacePrefix}.json");
            var wrapper = JsonConvert.DeserializeObject<NamespaceWrapper>(response);
            return wrapper?.Namespace;
        }

        /// <summary>
        /// 创建新的命名空间
        /// </summary>
        /// <param name="namespacePrefix">命名空间的前缀</param>
        /// <param name="uri">命名空间的 URI</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateNamespaceAsync(string namespacePrefix, string uri)
        {
            var ns = new { @namespace = new { prefix = namespacePrefix, uri = uri } };
            var json = JsonConvert.SerializeObject(ns);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/rest/namespaces", content);
        }

        /// <summary>
        /// 更新现有的命名空间
        /// </summary>
        /// <param name="namespacePrefix">要更新的命名空间前缀</param>
        /// <param name="uri">命名空间的新 URI</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateNamespaceAsync(string namespacePrefix, string uri)
        {
            var ns = new { @namespace = new { prefix = namespacePrefix, uri = uri } };
            var json = JsonConvert.SerializeObject(ns);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/namespaces/{namespacePrefix}", content);
        }

        /// <summary>
        /// 删除命名空间
        /// </summary>
        /// <param name="namespacePrefix">要删除的命名空间前缀</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteNamespaceAsync(string namespacePrefix)
        {
            await _httpClient.DeleteAsync($"/rest/namespaces/{namespacePrefix}");
        }
    }
}
