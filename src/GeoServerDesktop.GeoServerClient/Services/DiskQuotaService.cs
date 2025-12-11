using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoWebCache disk quotas
    /// </summary>
    public class DiskQuotaService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 DiskQuotaService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public DiskQuotaService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// 获取磁盘配额配置
        /// </summary>
        /// <returns>磁盘配额配置</returns>
        public async Task<DiskQuotaConfig> GetDiskQuotaAsync()
        {
            var response = await _httpClient.GetAsync("/gwc/rest/diskquota.json");
            return JsonConvert.DeserializeObject<DiskQuotaConfig>(response);
        }

        /// <summary>
        /// 更新磁盘配额配置
        /// </summary>
        /// <param name="config">Updated disk quota configuration</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateDiskQuotaAsync(DiskQuotaConfig config)
        {
            var json = JsonConvert.SerializeObject(config);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync("/gwc/rest/diskquota", content);
        }
    }
}
