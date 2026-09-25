using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IDiskQuotaService 的服务接口（M5 接口化）：与实现类 DiskQuotaService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IDiskQuotaService
    {
        /// <summary>
        /// 获取磁盘配额配置
        /// </summary>
        /// <returns>磁盘配额配置</returns>
        Task<DiskQuotaConfig> GetDiskQuotaAsync();

        /// <summary>
        /// 更新磁盘配额配置
        /// </summary>
        /// <param name="config">Updated disk quota configuration</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateDiskQuotaAsync(DiskQuotaConfig config);
    }
}
