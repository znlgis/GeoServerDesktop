using GeoServerDesktop.GeoServerClient.Http;
using System.Net.Http;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于重新加载和重置 GeoServer 目录和配置的服务
    /// </summary>
    public class ReloadService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 ReloadService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public ReloadService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// 从文件系统重新加载 GeoServer 目录
        /// </summary>
        /// <returns>表示异步操作的任务</returns>
        /// <remarks>
        /// 此操作从数据目录重新加载目录及其所有资源。
        /// 在手动修改配置文件后很有用。
        /// </remarks>
        public async Task ReloadCatalogAsync()
        {
            var content = new StringContent(string.Empty);
            await _httpClient.PostAsync("/rest/reload", content);
        }

        /// <summary>
        /// 重置所有存储、栅格和模式缓存
        /// </summary>
        /// <returns>表示异步操作的任务</returns>
        /// <remarks>
        /// 此操作清除所有缓存，但不重新加载目录。
        /// 在外部数据源发生更改时很有用。
        /// </remarks>
        public async Task ResetAsync()
        {
            var content = new StringContent(string.Empty);
            await _httpClient.PostAsync("/rest/reset", content);
        }
    }
}
