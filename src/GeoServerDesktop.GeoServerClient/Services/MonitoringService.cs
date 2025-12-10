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
        /// Initializes a new instance of the MonitoringService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
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
