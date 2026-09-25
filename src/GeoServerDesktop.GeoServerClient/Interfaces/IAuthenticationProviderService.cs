using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IAuthenticationProviderService 的服务接口（M5 接口化）：与实现类 AuthenticationProviderService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IAuthenticationProviderService
    {
        /// <summary>
        /// Gets the list of all authentication providers
        /// </summary>
        /// <returns>List of authentication providers</returns>
        Task<AuthenticationProviderListWrapper> GetProvidersAsync();

        /// <summary>
        /// Gets details for a specific authentication provider
        /// </summary>
        /// <param name="providerName">Provider name</param>
        /// <returns>Authentication provider details</returns>
        Task<AuthenticationProviderWrapper> GetProviderAsync(string providerName);

        /// <summary>
        /// Creates a new authentication provider
        /// </summary>
        /// <param name="provider">Authentication provider to create</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateProviderAsync(AuthenticationProvider provider);

        /// <summary>
        /// Updates an existing authentication provider
        /// </summary>
        /// <param name="providerName">Provider name</param>
        /// <param name="provider">Updated provider information</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateProviderAsync(string providerName, AuthenticationProvider provider);

        /// <summary>
        /// Deletes an authentication provider
        /// </summary>
        /// <param name="providerName">Provider name to delete</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteProviderAsync(string providerName);
    }
}
