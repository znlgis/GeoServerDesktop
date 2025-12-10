using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Http
{
    /// <summary>
    /// Interface for GeoServer HTTP client operations
    /// </summary>
    public interface IGeoServerHttpClient : IDisposable
    {
        /// <summary>
        /// Executes a GET request to the specified path
        /// </summary>
        /// <param name="path">Relative path to the REST endpoint</param>
        /// <returns>Response content as string</returns>
        Task<string> GetAsync(string path);

        /// <summary>
        /// Executes a POST request to the specified path
        /// </summary>
        /// <param name="path">Relative path to the REST endpoint</param>
        /// <param name="content">Request content</param>
        /// <returns>Response content as string</returns>
        Task<string> PostAsync(string path, HttpContent content);

        /// <summary>
        /// Executes a PUT request to the specified path
        /// </summary>
        /// <param name="path">Relative path to the REST endpoint</param>
        /// <param name="content">Request content</param>
        /// <returns>Response content as string</returns>
        Task<string> PutAsync(string path, HttpContent content);

        /// <summary>
        /// Executes a DELETE request to the specified path
        /// </summary>
        /// <param name="path">Relative path to the REST endpoint</param>
        /// <returns>Response content as string</returns>
        Task<string> DeleteAsync(string path);
    }
}
