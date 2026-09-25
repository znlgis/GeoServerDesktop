using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IFilterChainService 的服务接口（M5 接口化）：与实现类 FilterChainService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IFilterChainService
    {
        /// <summary>
        /// Gets the list of all filter chains
        /// </summary>
        /// <returns>List of filter chains</returns>
        Task<FilterChainListWrapper> GetFilterChainsAsync();

        /// <summary>
        /// 获取特定过滤器的详细信息 chain
        /// </summary>
        /// <param name="chainName">Chain name</param>
        /// <returns>过滤器链详细信息</returns>
        Task<FilterChainWrapper> GetFilterChainAsync(string chainName);

        /// <summary>
        /// 更新现有的过滤器 chain
        /// </summary>
        /// <param name="chainName">Chain name</param>
        /// <param name="chain">Updated chain information</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateFilterChainAsync(string chainName, FilterChain chain);
    }
}
