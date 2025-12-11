using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing URL validation checks
    /// </summary>
    public class URLCheckService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 URLCheckService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public URLCheckService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the list of URL validation checks
        /// </summary>
        /// <returns>List of URL checks</returns>
        public async Task<URLCheckListWrapper> GetURLChecksAsync()
        {
            var response = await _httpClient.GetAsync("/rest/urlchecks.json");
            return JsonConvert.DeserializeObject<URLCheckListWrapper>(response);
        }

        /// <summary>
        /// Creates a new URL validation check
        /// </summary>
        /// <param name="check">URL check to create</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateURLCheckAsync(URLCheck check)
        {
            var json = JsonConvert.SerializeObject(check);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/rest/urlchecks", content);
        }

        /// <summary>
        /// Deletes a URL validation check
        /// </summary>
        /// <param name="checkName">Check name</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteURLCheckAsync(string checkName)
        {
            await _httpClient.DeleteAsync($"/rest/urlchecks/{checkName}");
        }
    }
}
