using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IAuthenticationFilterService 的服务接口（M5 接口化）：与实现类 AuthenticationFilterService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IAuthenticationFilterService
    {
        /// <summary>
        /// Gets the list of all authentication filters
        /// </summary>
        /// <returns>List of authentication filters</returns>
        Task<AuthenticationFilterListWrapper> GetFiltersAsync();

        /// <summary>
        /// Gets details for a specific authentication filter
        /// </summary>
        /// <param name="filterName">Filter name</param>
        /// <returns>Authentication filter details</returns>
        Task<AuthenticationFilterWrapper> GetFilterAsync(string filterName);

        /// <summary>
        /// Creates a new authentication filter
        /// </summary>
        /// <param name="filter">Authentication filter to create</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateFilterAsync(AuthenticationFilter filter);

        /// <summary>
        /// Updates an existing authentication filter
        /// </summary>
        /// <param name="filterName">Filter name</param>
        /// <param name="filter">Updated filter information</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateFilterAsync(string filterName, AuthenticationFilter filter);

        /// <summary>
        /// Deletes an authentication filter
        /// </summary>
        /// <param name="filterName">Filter name to delete</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteFilterAsync(string filterName);
    }
}
