using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于管理 GeoServer 全局设置的服务
    /// </summary>
    public class SettingsService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 SettingsService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public SettingsService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// 获取 GeoServer 全局设置
        /// </summary>
        /// <returns>全局设置</returns>
        public async Task<GlobalSettings> GetGlobalSettingsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/settings.json");
            return JsonConvert.DeserializeObject<GlobalSettings>(response);
        }

        /// <summary>
        /// 更新 GeoServer 全局设置
        /// </summary>
        /// <param name="settings">更新后的全局设置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateGlobalSettingsAsync(GlobalSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync("/rest/settings", content);
        }

        /// <summary>
        /// 从全局设置中获取联系信息
        /// </summary>
        /// <returns>联系信息</returns>
        public async Task<ContactInfoWrapper> GetContactInfoAsync()
        {
            var response = await _httpClient.GetAsync("/rest/settings/contact.json");
            return JsonConvert.DeserializeObject<ContactInfoWrapper>(response);
        }

        /// <summary>
        /// 更新全局设置中的联系信息
        /// </summary>
        /// <param name="contact">更新后的联系信息</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateContactInfoAsync(ContactInfo contact)
        {
            var wrapper = new { contact = contact };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync("/rest/settings/contact", content);
        }
    }
}
