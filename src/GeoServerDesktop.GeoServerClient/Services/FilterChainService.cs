using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing security filter chains
    /// </summary>
    public class FilterChainService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 FilterChainService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public FilterChainService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the list of all filter chains
        /// </summary>
        /// <returns>List of filter chains</returns>
        public async Task<FilterChainListWrapper> GetFilterChainsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/security/filterChains.json");
            return JsonConvert.DeserializeObject<FilterChainListWrapper>(response);
        }

        /// <summary>
        /// 获取特定过滤器的详细信息 chain
        /// </summary>
        /// <param name="chainName">Chain name</param>
        /// <returns>过滤器链详细信息</returns>
        public async Task<FilterChainWrapper> GetFilterChainAsync(string chainName)
        {
            var response = await _httpClient.GetAsync($"/rest/security/filterChains/{chainName}.json");
            return JsonConvert.DeserializeObject<FilterChainWrapper>(response);
        }

        /// <summary>
        /// 更新现有的过滤器 chain
        /// </summary>
        /// <param name="chainName">Chain name</param>
        /// <param name="chain">Updated chain information</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateFilterChainAsync(string chainName, FilterChain chain)
        {
            var wrapper = new { filterChain = chain };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/security/filterChains/{chainName}", content);
        }
    }
}
