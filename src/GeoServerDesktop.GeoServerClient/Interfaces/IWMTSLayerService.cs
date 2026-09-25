using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IWMTSLayerService 的服务接口（M5 接口化）：与实现类 WMTSLayerService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IWMTSLayerService
    {
        /// <summary>
        /// Gets a list of WMTS layers in a WMTS store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStoreName">WMTS 存储的名称</param>
        /// <returns>WMTS 图层数组</returns>
        Task<WMTSLayer[]> GetWMTSLayersAsync(string workspaceName, string wmtsStoreName);

        /// <summary>
        /// 获取特定 WMTS 图层的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStoreName">WMTS 存储的名称</param>
        /// <param name="wmtsLayerName">WMTS 图层的名称</param>
        /// <returns>WMTS 图层详细信息</returns>
        Task<WMTSLayer> GetWMTSLayerAsync(string workspaceName, string wmtsStoreName, string wmtsLayerName);

        /// <summary>
        /// Publishes a new WMTS layer from the cascaded WMTS store
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStoreName">WMTS 存储的名称</param>
        /// <param name="wmtsLayer">WMTS 图层配置</param>
        /// <returns>表示异步操作的任务</returns>
        /// <remarks>
        /// The nativeName should match a layer name available in the remote WMTS service.
        /// </remarks>
        Task CreateWMTSLayerAsync(string workspaceName, string wmtsStoreName, WMTSLayer wmtsLayer);

        /// <summary>
        /// 更新现有的 WMTS 图层
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStoreName">WMTS 存储的名称</param>
        /// <param name="wmtsLayerName">WMTS 图层的名称</param>
        /// <param name="wmtsLayer">Updated WMTS 图层配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateWMTSLayerAsync(string workspaceName, string wmtsStoreName, string wmtsLayerName, WMTSLayer wmtsLayer);

        /// <summary>
        /// 删除 WMTS 图层
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmtsStoreName">WMTS 存储的名称</param>
        /// <param name="wmtsLayerName">WMTS 图层的名称</param>
        /// <param name="recurse">Whether to recursively delete all resources associated with the WMTS layer</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteWMTSLayerAsync(string workspaceName, string wmtsStoreName, string wmtsLayerName, bool recurse = false);
    }
}
