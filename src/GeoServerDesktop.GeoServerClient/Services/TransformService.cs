using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing XSLT transforms
    /// </summary>
    public class TransformService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 TransformService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public TransformService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the list of available transforms
        /// </summary>
        /// <returns>List of transforms</returns>
        public async Task<TransformListWrapper> GetTransformsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/transforms.json");
            return JsonConvert.DeserializeObject<TransformListWrapper>(response);
        }

        /// <summary>
        /// Gets a specific transform
        /// </summary>
        /// <param name="transformName">Transform name</param>
        /// <returns>Transform content</returns>
        public async Task<string> GetTransformAsync(string transformName)
        {
            return await _httpClient.GetAsync($"/rest/transforms/{transformName}");
        }

        /// <summary>
        /// Creates a new transform
        /// </summary>
        /// <param name="transformName">Transform name</param>
        /// <param name="xsltContent">XSLT content</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateTransformAsync(string transformName, string xsltContent)
        {
            var content = new StringContent(xsltContent, Encoding.UTF8, "application/xslt+xml");
            await _httpClient.PostAsync($"/rest/transforms/{transformName}", content);
        }

        /// <summary>
        /// Deletes a transform
        /// </summary>
        /// <param name="transformName">Transform name</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteTransformAsync(string transformName)
        {
            await _httpClient.DeleteAsync($"/rest/transforms/{transformName}");
        }
    }
}
