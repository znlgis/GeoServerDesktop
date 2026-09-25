using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing security filter chains
    /// </summary>
    public class FilterChainService : ServiceBase, IFilterChainService
    {

        /// <summary>
        /// 初始化 FilterChainService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public FilterChainService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// Gets the list of all filter chains
        /// </summary>
        /// <returns>List of filter chains</returns>
        public async Task<FilterChainListWrapper> GetFilterChainsAsync()
        {
            var response = await Http.GetAsync("/rest/security/filterchain.json");
            // FIXED-E3：3.0.1 列表实测 {"filterchain":{"filters":[...]}}，双层解析
            return FilterChainListWrapper.Parse(response);
        }

        /// <summary>
        /// 获取特定过滤器的详细信息 chain
        /// </summary>
        /// <param name="chainName">Chain name</param>
        /// <returns>过滤器链详细信息</returns>
        public async Task<FilterChainWrapper> GetFilterChainAsync(string chainName)
        {
            var response = await Http.GetAsync($"/rest/security/filterchain/{chainName}.json");
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
            await PutJsonAsync($"/rest/security/filterchain/{chainName}", wrapper);
        }
    }
}
