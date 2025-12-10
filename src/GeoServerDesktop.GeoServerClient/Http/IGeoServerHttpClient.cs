using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Http
{
    /// <summary>
    /// GeoServer HTTP 客户端操作的接口
    /// </summary>
    public interface IGeoServerHttpClient : IDisposable
    {
        /// <summary>
        /// 对指定路径执行 GET 请求
        /// </summary>
        /// <param name="path">REST 端点的相对路径</param>
        /// <returns>响应内容字符串</returns>
        Task<string> GetAsync(string path);

        /// <summary>
        /// 对指定路径执行 POST 请求
        /// </summary>
        /// <param name="path">REST 端点的相对路径</param>
        /// <param name="content">请求内容</param>
        /// <returns>响应内容字符串</returns>
        Task<string> PostAsync(string path, HttpContent content);

        /// <summary>
        /// 对指定路径执行 PUT 请求
        /// </summary>
        /// <param name="path">REST 端点的相对路径</param>
        /// <param name="content">请求内容</param>
        /// <returns>响应内容字符串</returns>
        Task<string> PutAsync(string path, HttpContent content);

        /// <summary>
        /// 对指定路径执行 DELETE 请求
        /// </summary>
        /// <param name="path">REST 端点的相对路径</param>
        /// <returns>响应内容字符串</returns>
        Task<string> DeleteAsync(string path);
    }
}
