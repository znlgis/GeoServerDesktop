using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IGWCLayerService 的服务接口（M5 接口化）：与实现类 GWCLayerService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IGWCLayerService
    {
        /// <summary>
        /// Gets the list of cached layers
        /// </summary>
        /// <returns>List of cached layers</returns>
        Task<GWCLayerListWrapper> GetLayersAsync();

        /// <summary>
        /// Gets information about a specific cached layer
        /// </summary>
        /// <param name="layerName">Layer name</param>
        /// <returns>Layer information</returns>
        Task<GWCLayer> GetLayerAsync(string layerName);

        /// <summary>
        /// Seeds a layer (starts tile generation)
        /// </summary>
        /// <param name="layerName">Layer name</param>
        /// <param name="seedRequest">Seed request configuration</param>
        /// <returns>表示异步操作的任务</returns>
        Task SeedLayerAsync(string layerName, SeedRequest seedRequest);

        /// <summary>
        /// Truncates the cache for all layers (mass truncate)
        /// </summary>
        /// <returns>表示异步操作的任务</returns>
        Task TruncateAllLayersAsync();
    }
}
