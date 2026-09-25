using System;
using System.Net.Http;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IResourceService 的服务接口（M5 接口化）：与实现类 ResourceService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IResourceService
    {
        /// <summary>
        /// Lists resources at the specified path
        /// </summary>
        /// <param name="resourcePath">Path to the resource directory (e.g., "styles", "workspaces/myws")</param>
        /// <returns>Resource listing as string</returns>
        /// <remarks>
        /// The path is relative to the GeoServer data directory.
        /// Returns an HTML directory listing or file content.
        /// </remarks>
        Task<string> ListResourcesAsync(string resourcePath);

        /// <summary>
        /// Reads the content of a resource file from the GeoServer data directory
        /// </summary>
        /// <param name="resourcePath">Path to the file relative to the data directory (e.g., "logs/geoserver.log")</param>
        /// <returns>File content as string</returns>
        /// <remarks>
        /// Uses GET /rest/resource/{path}, which returns the raw file content for files
        /// (and an HTML directory listing for directories). The path is relative to the
        /// data directory root; empty paths are rejected to avoid requesting the bare
        /// directory listing endpoint.
        /// </remarks>
        Task<string> GetResourceContentAsync(string resourcePath);

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
        Task UploadResourceAsync(string resourcePath, byte[] content, string contentType = "application/octet-stream");

        /// <summary>
        /// Deletes a resource at the specified path
        /// </summary>
        /// <param name="resourcePath">Path to the resource to delete (e.g., "styles/myStyle.sld")</param>
        /// <returns>表示异步操作的任务</returns>
        /// <remarks>
        /// This operation permanently deletes a file from the GeoServer data directory.
        /// </remarks>
        Task DeleteResourceAsync(string resourcePath);
    }
}
