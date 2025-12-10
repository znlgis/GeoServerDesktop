using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoWebCache disk quotas
    /// </summary>
    public class DiskQuotaService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the DiskQuotaService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public DiskQuotaService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the disk quota configuration
        /// </summary>
        /// <returns>Disk quota configuration</returns>
        public async Task<DiskQuotaConfig> GetDiskQuotaAsync()
        {
            var response = await _httpClient.GetAsync("/gwc/rest/diskquota.json");
            return JsonConvert.DeserializeObject<DiskQuotaConfig>(response);
        }

        /// <summary>
        /// Updates the disk quota configuration
        /// </summary>
        /// <param name="config">Updated disk quota configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateDiskQuotaAsync(DiskQuotaConfig config)
        {
            var json = JsonConvert.SerializeObject(config);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync("/gwc/rest/diskquota", content);
        }
    }
}
