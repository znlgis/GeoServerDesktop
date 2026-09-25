using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing XSLT transforms。
    /// 注：3.0.1 默认镜像未安装 WPS/XSLT 转换扩展，/rest/transforms 实测 404——
    /// 列表模型基线（{"transforms":[...]}）维持不变（清单+基线不动）。
    /// </summary>
    public class TransformService : ServiceBase, ITransformService
    {

        /// <summary>
        /// 初始化 TransformService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public TransformService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// Gets the list of available transforms
        /// </summary>
        /// <returns>List of transforms</returns>
        public async Task<TransformListWrapper> GetTransformsAsync()
        {
            var response = await Http.GetAsync("/rest/transforms.json");
            return JsonConvert.DeserializeObject<TransformListWrapper>(response);
        }

        /// <summary>
        /// Gets a specific transform
        /// </summary>
        /// <param name="transformName">Transform name</param>
        /// <returns>Transform content</returns>
        public async Task<string> GetTransformAsync(string transformName)
        {
            return await Http.GetAsync($"/rest/transforms/{transformName}");
        }

        /// <summary>
        /// Creates a new transform
        /// </summary>
        /// <param name="transformName">Transform name</param>
        /// <param name="xsltContent">XSLT content</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateTransformAsync(string transformName, string xsltContent)
        {
            await PostContentAsync($"/rest/transforms/{transformName}", TextContent(xsltContent, "application/xslt+xml"));
        }

        /// <summary>
        /// Deletes a transform
        /// </summary>
        /// <param name="transformName">Transform name</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteTransformAsync(string transformName)
        {
            await Http.DeleteAsync($"/rest/transforms/{transformName}");
        }
    }
}
