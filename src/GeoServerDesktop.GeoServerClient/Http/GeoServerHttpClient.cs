using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Configuration;

namespace GeoServerDesktop.GeoServerClient.Http
{
    /// <summary>
    /// 用于 GeoServer REST API 操作的 HTTP 客户端
    /// </summary>
    public class GeoServerHttpClient : IGeoServerHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private bool _disposed;

        /// <summary>
        /// 初始化 GeoServerHttpClient 类的新实例
        /// </summary>
        /// <param name="options">配置选项</param>
        public GeoServerHttpClient(GeoServerClientOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(options.BaseUrl))
                throw new ArgumentException("BaseUrl is required", nameof(options));

            _baseUrl = options.BaseUrl.TrimEnd('/');

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
            };

            // 设置基本身份验证
            if (!string.IsNullOrWhiteSpace(options.Username))
            {
                var authToken = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{options.Username}:{options.Password}"));
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", authToken);
            }

            // 设置 JSON 的默认头
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// 对指定路径执行 GET 请求
        /// </summary>
        /// <param name="path">REST 端点的相对路径</param>
        /// <returns>响应内容字符串</returns>
        public async Task<string> GetAsync(string path)
        {
            var url = BuildUrl(path);
            var response = await _httpClient.GetAsync(url);
            return await ProcessResponse(response);
        }

        /// <summary>
        /// 对指定路径执行 POST 请求
        /// </summary>
        /// <param name="path">REST 端点的相对路径</param>
        /// <param name="content">请求内容</param>
        /// <returns>响应内容字符串</returns>
        public async Task<string> PostAsync(string path, HttpContent content)
        {
            var url = BuildUrl(path);
            var response = await _httpClient.PostAsync(url, content);
            return await ProcessResponse(response);
        }

        /// <summary>
        /// 对指定路径执行 PUT 请求
        /// </summary>
        /// <param name="path">REST 端点的相对路径</param>
        /// <param name="content">请求内容</param>
        /// <returns>响应内容字符串</returns>
        public async Task<string> PutAsync(string path, HttpContent content)
        {
            var url = BuildUrl(path);
            var response = await _httpClient.PutAsync(url, content);
            return await ProcessResponse(response);
        }

        /// <summary>
        /// 对指定路径执行 DELETE 请求
        /// </summary>
        /// <param name="path">REST 端点的相对路径</param>
        /// <returns>响应内容字符串</returns>
        public async Task<string> DeleteAsync(string path)
        {
            var url = BuildUrl(path);
            var response = await _httpClient.DeleteAsync(url);
            return await ProcessResponse(response);
        }

        /// <summary>
        /// 构建完整的 URL
        /// </summary>
        private string BuildUrl(string path)
        {
            path = path?.TrimStart('/') ?? string.Empty;
            return $"{_baseUrl}/{path}";
        }

        /// <summary>
        /// 处理 HTTP 响应
        /// </summary>
        private async Task<string> ProcessResponse(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new GeoServerRequestException(
                    $"GeoServer request failed with status code {(int)response.StatusCode} ({response.StatusCode})",
                    (int)response.StatusCode,
                    content);
            }

            return content;
        }

        /// <summary>
        /// 释放 HTTP 客户端资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的受保护方法
        /// </summary>
        /// <param name="disposing">是否释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
