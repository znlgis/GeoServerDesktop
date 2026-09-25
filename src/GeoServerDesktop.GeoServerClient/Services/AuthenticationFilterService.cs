using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing authentication filters
    /// </summary>
    public class AuthenticationFilterService : ServiceBase
    {

        /// <summary>
        /// 初始化 AuthenticationFilterService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public AuthenticationFilterService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// Gets the list of all authentication filters
        /// </summary>
        /// <returns>List of authentication filters</returns>
        public async Task<AuthenticationFilterListWrapper> GetFiltersAsync()
        {
            var response = await Http.GetAsync("/rest/security/authfilters.json");
            // FIXED-E1：3.0.1 列表实测 {"authfilters":{"authfilter":[{name,href}]}}，双层解析
            return AuthenticationFilterListWrapper.Parse(response);
        }

        /// <summary>
        /// Gets details for a specific authentication filter
        /// </summary>
        /// <param name="filterName">Filter name</param>
        /// <returns>Authentication filter details</returns>
        public async Task<AuthenticationFilterWrapper> GetFilterAsync(string filterName)
        {
            var response = await Http.GetAsync($"/rest/security/authfilters/{filterName}.json");
            // FIXED-E1：单体实测以配置类全名为动态根键，需 ParseSingle
            return new AuthenticationFilterWrapper { Filter = AuthenticationFilter.ParseSingle(response) };
        }

        /// <summary>
        /// Creates a new authentication filter
        /// </summary>
        /// <param name="filter">Authentication filter to create</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateFilterAsync(AuthenticationFilter filter)
        {
            var wrapper = new { filter = filter };
            await PostJsonAsync("/rest/security/authfilters", wrapper);
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
            await PutJsonAsync($"/rest/security/authfilters/{filterName}", wrapper);
        }

        /// <summary>
        /// Deletes an authentication filter
        /// </summary>
        /// <param name="filterName">Filter name to delete</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteFilterAsync(string filterName)
        {
            await Http.DeleteAsync($"/rest/security/authfilters/{filterName}");
        }
    }
}
