using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IWMSLayerService 的服务接口（M5 接口化）：与实现类 WMSLayerService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IWMSLayerService
    {
        /// <summary>
        /// Gets a list of WMS layers in a WMS store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStoreName">WMS 存储的名称</param>
        /// <returns>WMS 图层数组</returns>
        Task<WMSLayer[]> GetWMSLayersAsync(string workspaceName, string wmsStoreName);

        /// <summary>
        /// 获取特定 WMS 图层的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStoreName">WMS 存储的名称</param>
        /// <param name="wmsLayerName">WMS 图层的名称</param>
        /// <returns>WMS 图层详细信息</returns>
        Task<WMSLayer> GetWMSLayerAsync(string workspaceName, string wmsStoreName, string wmsLayerName);

        /// <summary>
        /// 创建新的 WMS 图层 (publishes a layer from remote WMS)
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStoreName">WMS 存储的名称</param>
        /// <param name="wmsLayer">WMS 图层配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateWMSLayerAsync(string workspaceName, string wmsStoreName, WMSLayer wmsLayer);

        /// <summary>
        /// 更新现有的 WMS 图层
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStoreName">WMS 存储的名称</param>
        /// <param name="wmsLayerName">WMS 图层的名称</param>
        /// <param name="wmsLayer">Updated WMS 图层配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateWMSLayerAsync(string workspaceName, string wmsStoreName, string wmsLayerName, WMSLayer wmsLayer);

        /// <summary>
        /// 删除 WMS 图层
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStoreName">WMS 存储的名称</param>
        /// <param name="wmsLayerName">WMS 图层的名称</param>
        /// <param name="recurse">Whether to recursively delete all resources associated with the WMS layer</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteWMSLayerAsync(string workspaceName, string wmsStoreName, string wmsLayerName, bool recurse = false);
    }
}
