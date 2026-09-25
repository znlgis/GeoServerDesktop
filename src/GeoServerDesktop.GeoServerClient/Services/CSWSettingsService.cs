using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing CSW (Catalogue Service for the Web) settings
    /// </summary>
    public class CSWSettingsService : ServiceBase, ICSWSettingsService
    {

        /// <summary>
        /// 初始化 CSWSettingsService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public CSWSettingsService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// Gets the CSW service settings
        /// </summary>
        /// <returns>CSW 设置</returns>
        public async Task<CSWSettings> GetSettingsAsync()
        {
            var response = await Http.GetAsync("/rest/services/csw/settings.json");
            return JsonConvert.DeserializeObject<CSWSettings>(response);
        }

        /// <summary>
        /// Updates the CSW service settings
        /// </summary>
        /// <param name="settings">Updated CSW 设置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateSettingsAsync(CSWSettings settings)
        {
            await PutJsonAsync("/rest/services/csw/settings", settings);
        }
    }
}
