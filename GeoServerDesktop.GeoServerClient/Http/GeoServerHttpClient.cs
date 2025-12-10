using GeoServerDesktop.GeoServerClient.Configuration;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Http
{
    /// <summary>
    /// HTTP client for GeoServer REST API operations
    /// </summary>
    public class GeoServerHttpClient : IGeoServerHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the GeoServerHttpClient class
        /// </summary>
        /// <param name="options">Configuration options</param>
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

            // Set up basic authentication
            if (!string.IsNullOrWhiteSpace(options.Username))
            {
                var authToken = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{options.Username}:{options.Password}"));
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", authToken);
            }

            // Set default headers for JSON
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Executes a GET request to the specified path
        /// </summary>
        /// <param name="path">Relative path to the REST endpoint</param>
        /// <returns>Response content as string</returns>
        public async Task<string> GetAsync(string path)
        {
            var url = BuildUrl(path);
            var response = await _httpClient.GetAsync(url);
            return await ProcessResponse(response);
        }

        /// <summary>
        /// Executes a POST request to the specified path
        /// </summary>
        /// <param name="path">Relative path to the REST endpoint</param>
        /// <param name="content">Request content</param>
        /// <returns>Response content as string</returns>
        public async Task<string> PostAsync(string path, HttpContent content)
        {
            var url = BuildUrl(path);
            var response = await _httpClient.PostAsync(url, content);
            return await ProcessResponse(response);
        }

        /// <summary>
        /// Executes a PUT request to the specified path
        /// </summary>
        /// <param name="path">Relative path to the REST endpoint</param>
        /// <param name="content">Request content</param>
        /// <returns>Response content as string</returns>
        public async Task<string> PutAsync(string path, HttpContent content)
        {
            var url = BuildUrl(path);
            var response = await _httpClient.PutAsync(url, content);
            return await ProcessResponse(response);
        }

        /// <summary>
        /// Executes a DELETE request to the specified path
        /// </summary>
        /// <param name="path">Relative path to the REST endpoint</param>
        /// <returns>Response content as string</returns>
        public async Task<string> DeleteAsync(string path)
        {
            var url = BuildUrl(path);
            var response = await _httpClient.DeleteAsync(url);
            return await ProcessResponse(response);
        }

        private string BuildUrl(string path)
        {
            path = path?.TrimStart('/') ?? string.Empty;
            return $"{_baseUrl}/{path}";
        }

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
        /// Disposes the HTTP client
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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
