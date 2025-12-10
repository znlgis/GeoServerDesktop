using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for monitoring GeoServer requests
    /// </summary>
    public class MonitoringService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 MonitoringService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public MonitoringService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the request history
        /// </summary>
        /// <returns>List of monitored requests</returns>
        public async Task<MonitorRequestListWrapper> GetRequestsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/monitor/requests.json");
            return JsonConvert.DeserializeObject<MonitorRequestListWrapper>(response);
        }

        /// <summary>
        /// Gets monitoring statistics
        /// </summary>
        /// <returns>Monitoring statistics</returns>
        public async Task<MonitorStatistics> GetStatisticsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/monitor/statistics.json");
            return JsonConvert.DeserializeObject<MonitorStatistics>(response);
        }
    }
}
