using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IAboutService 的服务接口（M5 接口化）：与实现类 AboutService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IAboutService
    {
        /// <summary>
        /// 获取 GeoServer 版本信息
        /// </summary>
        /// <returns>版本信息，包括 GeoServer、GeoTools 和 GeoWebCache 版本</returns>
        Task<VersionInfoWrapper> GetVersionAsync();

        /// <summary>
        /// 获取已安装的 GeoServer 模块及其版本的列表
        /// </summary>
        /// <returns>所有已安装模块的清单信息</returns>
        Task<ManifestsWrapper> GetManifestsAsync();

        /// <summary>
        /// 获取当前系统状态，包括内存使用情况和 JVM 信息
        /// </summary>
        /// <returns>系统状态信息</returns>
        Task<SystemStatusWrapper> GetSystemStatusAsync();
    }
}
