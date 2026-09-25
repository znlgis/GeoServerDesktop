using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IWMTSStoreService 的服务接口（M5 接口化）：与实现类 WMTSStoreService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IWMTSStoreService
    {
        /// <summary>
        /// 获取工作空间中的 WMTS 存储列表
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <returns>WMTS 存储数组</returns>
        Task<WMTSStore[]> GetWMTSStoresAsync(string workspaceName);

        /// <summary>
        /// 获取特定 WMTS 存储的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStoreName">WMTS 存储的名称</param>
        /// <returns>WMTS 存储详细信息</returns>
        Task<WMTSStore> GetWMTSStoreAsync(string workspaceName, string wmtsStoreName);

        /// <summary>
        /// 创建新的 WMTS 存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStore">WMTS 存储配置</param>
        /// <returns>表示异步操作的任务</returns>
        /// <remarks>
        /// The WMTS store enables cascading of a remote WMTS service.
        /// The capabilitiesURL should point to a valid WMTS GetCapabilities document.
        /// </remarks>
        Task CreateWMTSStoreAsync(string workspaceName, WMTSStore wmtsStore);

        /// <summary>
        /// 更新现有的 WMTS 存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStoreName">WMTS 存储的名称</param>
        /// <param name="wmtsStore">Updated WMTS 存储配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateWMTSStoreAsync(string workspaceName, string wmtsStoreName, WMTSStore wmtsStore);

        /// <summary>
        /// 删除 WMTS 存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStoreName">WMTS 存储的名称</param>
        /// <param name="recurse">Whether to recursively delete all WMTS layers in the store</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteWMTSStoreAsync(string workspaceName, string wmtsStoreName, bool recurse = false);
    }
}
