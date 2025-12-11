using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing WPS (Web Processing Service) settings
    /// </summary>
    public class WPSSettingsService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 WPSSettingsService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public WPSSettingsService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the WPS service settings
        /// </summary>
        /// <returns>WPS 设置</returns>
        public async Task<WPSSettings> GetSettingsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/services/wps/settings.json");
            return JsonConvert.DeserializeObject<WPSSettings>(response);
        }

        /// <summary>
        /// Updates the WPS service settings
        /// </summary>
        /// <param name="settings">Updated WPS 设置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateSettingsAsync(WPSSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync("/rest/services/wps/settings", content);
        }
    }
}
