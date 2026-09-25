using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for monitoring GeoServer requests
    /// </summary>
    public class MonitoringService : ServiceBase, IMonitoringService
    {

        /// <summary>
        /// 初始化 MonitoringService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public MonitoringService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// Gets the request history
        /// </summary>
        /// <returns>List of monitored requests</returns>
        public async Task<MonitorRequestListWrapper> GetRequestsAsync()
        {
            var response = await Http.GetAsync("/rest/monitor/requests.json");
            // FIXED-E7：3.0.1 实测根为 {"org.geoserver.monitor.RequestDatas":{"org.geoserver.monitor.RequestData":[{name,href}]}}
            // （动态类名键、列表项仅摘要），直接反序列化取不到；改用手工 Parse。
            return MonitorRequestListWrapper.Parse(response);
        }

        /// <summary>
        /// Gets monitoring statistics。
        /// 说明（E6 维持基线）：statistics.json 实际为 {"statistics":{"stat":[...]}} 序列形态，
        /// 与本模型的聚合结构（totalRequests/byPath）完全不同且随 monitor 版本变化，维持原样透传反序列化。
        /// </summary>
        /// <returns>Monitoring statistics</returns>
        public async Task<MonitorStatistics> GetStatisticsAsync()
        {
            var response = await Http.GetAsync("/rest/monitor/statistics.json");
            return JsonConvert.DeserializeObject<MonitorStatistics>(response);
        }
    }
}
