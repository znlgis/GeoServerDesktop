using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IReloadService 的服务接口（M5 接口化）：与实现类 ReloadService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IReloadService
    {
        /// <summary>
        /// 从文件系统重新加载 GeoServer 目录
        /// </summary>
        /// <returns>表示异步操作的任务</returns>
        /// <remarks>
        /// 此操作从数据目录重新加载目录及其所有资源。
        /// 在手动修改配置文件后很有用。
        /// </remarks>
        Task ReloadCatalogAsync();

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
        Task ResetAsync();
    }
}
