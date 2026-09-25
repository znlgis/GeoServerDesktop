using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IWMSStoreService 的服务接口（M5 接口化）：与实现类 WMSStoreService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IWMSStoreService
    {
        /// <summary>
        /// 获取工作空间中的 WMS 存储列表
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <returns>WMS 存储数组</returns>
        Task<WMSStore[]> GetWMSStoresAsync(string workspaceName);

        /// <summary>
        /// 获取特定 WMS 存储的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStoreName">WMS 存储的名称</param>
        /// <returns>WMS 存储详细信息</returns>
        Task<WMSStore> GetWMSStoreAsync(string workspaceName, string wmsStoreName);

        /// <summary>
        /// 创建新的 WMS 存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStore">WMS 存储配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateWMSStoreAsync(string workspaceName, WMSStore wmsStore);

        /// <summary>
        /// 更新现有的 WMS 存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStoreName">WMS 存储的名称</param>
        /// <param name="wmsStore">Updated WMS 存储配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateWMSStoreAsync(string workspaceName, string wmsStoreName, WMSStore wmsStore);

        /// <summary>
        /// 删除 WMS 存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStoreName">WMS 存储的名称</param>
        /// <param name="recurse">Whether to recursively delete all WMS layers in the store</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteWMSStoreAsync(string workspaceName, string wmsStoreName, bool recurse = false);
    }
}
