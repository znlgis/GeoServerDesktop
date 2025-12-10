using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing authentication filters
    /// </summary>
    public class AuthenticationFilterService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 AuthenticationFilterService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public AuthenticationFilterService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the list of all authentication filters
        /// </summary>
        /// <returns>List of authentication filters</returns>
        public async Task<AuthenticationFilterListWrapper> GetFiltersAsync()
        {
            var response = await _httpClient.GetAsync("/rest/security/authFilters.json");
            return JsonConvert.DeserializeObject<AuthenticationFilterListWrapper>(response);
        }

        /// <summary>
        /// Gets details for a specific authentication filter
        /// </summary>
        /// <param name="filterName">Filter name</param>
        /// <returns>Authentication filter details</returns>
        public async Task<AuthenticationFilterWrapper> GetFilterAsync(string filterName)
        {
            var response = await _httpClient.GetAsync($"/rest/security/authFilters/{filterName}.json");
            return JsonConvert.DeserializeObject<AuthenticationFilterWrapper>(response);
        }

        /// <summary>
        /// Creates a new authentication filter
        /// </summary>
        /// <param name="filter">Authentication filter to create</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateFilterAsync(AuthenticationFilter filter)
        {
            var wrapper = new { filter = filter };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/rest/security/authFilters", content);
        }

        /// <summary>
        /// Updates an existing authentication filter
        /// </summary>
        /// <param name="filterName">Filter name</param>
        /// <param name="filter">Updated filter information</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateFilterAsync(string filterName, AuthenticationFilter filter)
        {
            var wrapper = new { filter = filter };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/security/authFilters/{filterName}", content);
        }

        /// <summary>
        /// Deletes an authentication filter
        /// </summary>
        /// <param name="filterName">Filter name to delete</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteFilterAsync(string filterName)
        {
            await _httpClient.DeleteAsync($"/rest/security/authFilters/{filterName}");
        }
    }
}
