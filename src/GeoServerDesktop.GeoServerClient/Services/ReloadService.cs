using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于重新加载和重置 GeoServer 目录和配置的服务
    /// </summary>
    public class ReloadService : ServiceBase
    {

        /// <summary>
        /// 初始化 ReloadService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public ReloadService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
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
            // FIXED-E18：原 new StringContent("") 隐式 Content-Type 为 text/plain。
            // 实测 3.0.1：/rest/reload 对无体/text/plain/application/json 均回 200；
            // 此处显式声明 application/json 空体，与 REST 语义一致、不依赖服务器宽容度。
            await PostContentAsync("/rest/reload", TextContent(string.Empty, "application/json"));
        }

        /// <summary>
        /// 重置所有存储、栅格和模式缓存
        /// </summary>
        /// <returns>表示异步操作的任务</returns>
        /// <remarks>
        /// 此操作清除所有缓存，但不重新加载目录。
        /// 在外部数据源发生更改时很有用。
        /// Warn：该端点会重置全部存储/栅格/模式缓存，影响正在服务的实例——集成测试仅以
        /// GET 探测存在性（实测 3.0.1 GET /rest/reset 回 405 Method Not Allowed，端点在位），
        /// 不实际 POST 调用；仅在明确安全（专用实例/维护窗口）时由调用方主动使用。
        /// </remarks>
        public async Task ResetAsync()
        {
            // FIXED-E18（同 ReloadCatalogAsync）：显式 application/json 空体。
            await PostContentAsync("/rest/reset", TextContent(string.Empty, "application/json"));
        }
    }
}
