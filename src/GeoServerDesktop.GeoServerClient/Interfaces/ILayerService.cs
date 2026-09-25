using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// ILayerService 的服务接口（M5 接口化）：与实现类 LayerService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface ILayerService
    {
        /// <summary>
        /// 获取所有图层的列表
        /// </summary>
        /// <returns>图层数组</returns>
        Task<Layer[]> GetLayersAsync();

        /// <summary>
        /// 获取特定图层的详细信息
        /// </summary>
        /// <param name="layerName">图层的名称</param>
        /// <returns>图层详细信息</returns>
        Task<Layer> GetLayerAsync(string layerName);

        /// <summary>
        /// 更新现有的图层
        /// </summary>
        /// <param name="layerName">图层的名称</param>
        /// <param name="layer">更新后的图层配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateLayerAsync(string layerName, Layer layer);

        /// <summary>
        /// 删除图层
        /// </summary>
        /// <param name="layerName">要删除的图层名称</param>
        /// <param name="recurse">是否递归删除与图层相关的所有资源</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteLayerAsync(string layerName, bool recurse = false);

        /// <summary>
        /// 获取特定工作空间中的图层列表
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <returns>工作空间中的图层数组</returns>
        Task<Layer[]> GetWorkspaceLayersAsync(string workspaceName);

        /// <summary>
        /// 获取工作空间中特定图层的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="layerName">图层的名称</param>
        /// <returns>图层详细信息</returns>
        Task<Layer> GetWorkspaceLayerAsync(string workspaceName, string layerName);
    }
}
