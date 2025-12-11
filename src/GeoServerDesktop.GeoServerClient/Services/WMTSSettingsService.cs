using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing WMTS service settings
    /// </summary>
    public class WMTSSettingsService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 WMTSSettingsService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public WMTSSettingsService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the global WMTS service settings
        /// </summary>
        /// <returns>WMTS 设置</returns>
        public async Task<WMTSSettings> GetWMTSSettingsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/services/wmts/settings.json");
            return JsonConvert.DeserializeObject<WMTSSettings>(response);
        }

        /// <summary>
        /// Updates the global WMTS service settings
        /// </summary>
        /// <param name="settings">Updated WMTS 设置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateWMTSSettingsAsync(WMTSSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync("/rest/services/wmts/settings", content);
        }
    }
}
