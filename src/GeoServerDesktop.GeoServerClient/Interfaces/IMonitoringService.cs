using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IMonitoringService 的服务接口（M5 接口化）：与实现类 MonitoringService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IMonitoringService
    {
        /// <summary>
        /// Gets the request history
        /// </summary>
        /// <returns>List of monitored requests</returns>
        Task<MonitorRequestListWrapper> GetRequestsAsync();

        /// <summary>
        /// Gets monitoring statistics。
        /// 说明（E6 维持基线）：statistics.json 实际为 {"statistics":{"stat":[...]}} 序列形态，
        /// 与本模型的聚合结构（totalRequests/byPath）完全不同且随 monitor 版本变化，维持原样透传反序列化。
        /// </summary>
        /// <returns>Monitoring statistics</returns>
        Task<MonitorStatistics> GetStatisticsAsync();
    }
}
