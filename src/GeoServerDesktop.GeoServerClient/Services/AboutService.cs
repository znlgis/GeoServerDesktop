using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于检索 GeoServer 系统信息的服务
    /// </summary>
    public class AboutService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 AboutService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public AboutService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// 获取 GeoServer 版本信息
        /// </summary>
        /// <returns>版本信息，包括 GeoServer、GeoTools 和 GeoWebCache 版本</returns>
        public async Task<VersionInfoWrapper> GetVersionAsync()
        {
            var response = await _httpClient.GetAsync("/rest/about/version.json");
            return JsonConvert.DeserializeObject<VersionInfoWrapper>(response);
        }

        /// <summary>
        /// 获取已安装的 GeoServer 模块及其版本的列表
        /// </summary>
        /// <returns>所有已安装模块的清单信息</returns>
        public async Task<ManifestsWrapper> GetManifestsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/about/manifests.json");
            return JsonConvert.DeserializeObject<ManifestsWrapper>(response);
        }

        /// <summary>
        /// 获取当前系统状态，包括内存使用情况和 JVM 信息
        /// </summary>
        /// <returns>系统状态信息</returns>
        public async Task<SystemStatusWrapper> GetSystemStatusAsync()
        {
            var response = await _httpClient.GetAsync("/rest/about/system-status.json");
            return JsonConvert.DeserializeObject<SystemStatusWrapper>(response);
        }
    }
}
