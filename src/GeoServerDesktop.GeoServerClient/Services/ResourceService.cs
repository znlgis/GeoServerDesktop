using System.Net.Http;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoServer resource files
    /// </summary>
    public class ResourceService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 ResourceService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public ResourceService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Lists resources at the specified path
        /// </summary>
        /// <param name="resourcePath">Path to the resource directory (e.g., "styles", "workspaces/myws")</param>
        /// <returns>Resource listing as string</returns>
        /// <remarks>
        /// The path is relative to the GeoServer data directory.
        /// Returns an HTML directory listing or file content.
        /// </remarks>
        public async Task<string> ListResourcesAsync(string resourcePath)
        {
            var path = string.IsNullOrEmpty(resourcePath) ? "/rest/resource" : $"/rest/resource/{resourcePath}";
            return await _httpClient.GetAsync(path);
        }

        /// <summary>
        /// Uploads a resource file to the specified path
        /// </summary>
        /// <param name="resourcePath">Path where the resource should be stored (e.g., "styles/myStyle.sld")</param>
        /// <param name="content">File content as byte array</param>
        /// <param name="contentType">MIME type of the content (e.g., "application/vnd.ogc.sld+xml")</param>
        /// <returns>表示异步操作的任务</returns>
        /// <remarks>
        /// This operation creates or overwrites a file in the GeoServer data directory.
        /// The path is relative to the data directory root.
        /// </remarks>
        public async Task UploadResourceAsync(string resourcePath, byte[] content, string contentType = "application/octet-stream")
        {
            var httpContent = new ByteArrayContent(content);
            httpContent.Headers.Add("Content-Type", contentType);
            await _httpClient.PutAsync($"/rest/resource/{resourcePath}", httpContent);
        }

        /// <summary>
        /// Deletes a resource at the specified path
        /// </summary>
        /// <param name="resourcePath">Path to the resource to delete (e.g., "styles/myStyle.sld")</param>
        /// <returns>表示异步操作的任务</returns>
        /// <remarks>
        /// This operation permanently deletes a file from the GeoServer data directory.
        /// </remarks>
        public async Task DeleteResourceAsync(string resourcePath)
        {
            await _httpClient.DeleteAsync($"/rest/resource/{resourcePath}");
        }
    }
}
