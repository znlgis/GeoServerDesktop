using System;
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
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// 获取磁盘配额配置
        /// </summary>
        /// <returns>磁盘配额配置</returns>
        public async Task<DiskQuotaConfig> GetDiskQuotaAsync()
        {
            // FIXED-E13：diskquota 端点恒返回 XML（DiskQuotaController，与 Accept 无关），XDocument 映射解析
            var response = await _httpClient.GetAsync("/gwc/rest/diskquota");
            return DiskQuotaConfig.ParseXml(response);
        }

        /// <summary>
        /// 更新磁盘配额配置
        /// </summary>
        /// <param name="config">Updated disk quota configuration</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateDiskQuotaAsync(DiskQuotaConfig config)
        {
            // FIXED-E13：回写同样使用 XML（与 ParseXml 字段往返）
            var json = (config ?? new DiskQuotaConfig()).ToXmlBody();
            using (var content = new StringContent(json, Encoding.UTF8, "application/xml"))
            {
                await _httpClient.PutAsync("/gwc/rest/diskquota", content);
            }
        }
    }
}
