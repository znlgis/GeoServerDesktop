using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// ILayerGroupService 的服务接口（M5 接口化）：与实现类 LayerGroupService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface ILayerGroupService
    {
        /// <summary>
        /// 获取所有图层组的列表
        /// </summary>
        /// <returns>图层组数组</returns>
        Task<LayerGroup[]> GetLayerGroupsAsync();

        /// <summary>
        /// 获取特定图层组的详细信息
        /// </summary>
        /// <param name="layerGroupName">图层组的名称</param>
        /// <returns>图层组详细信息</returns>
        Task<LayerGroup> GetLayerGroupAsync(string layerGroupName);

        /// <summary>
        /// 创建新的图层组
        /// </summary>
        /// <param name="layerGroup">图层组配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateLayerGroupAsync(LayerGroup layerGroup);

        /// <summary>
        /// 更新现有的图层组
        /// </summary>
        /// <param name="layerGroupName">图层组的名称</param>
        /// <param name="layerGroup">更新后的图层组配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateLayerGroupAsync(string layerGroupName, LayerGroup layerGroup);

        /// <summary>
        /// 删除图层组
        /// </summary>
        /// <param name="layerGroupName">要删除的图层组名称</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteLayerGroupAsync(string layerGroupName);

        /// <summary>
        /// 获取特定工作空间中的图层组列表
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <returns>工作空间中的图层组数组</returns>
        Task<LayerGroup[]> GetWorkspaceLayerGroupsAsync(string workspaceName);

        /// <summary>
        /// 获取工作空间中特定图层组的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="layerGroupName">图层组的名称</param>
        /// <returns>图层组详细信息</returns>
        Task<LayerGroup> GetWorkspaceLayerGroupAsync(string workspaceName, string layerGroupName);

        /// <summary>
        /// 在特定工作空间中创建新的图层组
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="layerGroup">图层组配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateWorkspaceLayerGroupAsync(string workspaceName, LayerGroup layerGroup);

        /// <summary>
        /// 更新工作空间中的现有图层组
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="layerGroupName">图层组的名称</param>
        /// <param name="layerGroup">更新后的图层组配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateWorkspaceLayerGroupAsync(string workspaceName, string layerGroupName, LayerGroup layerGroup);

        /// <summary>
        /// 从工作空间中删除图层组
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="layerGroupName">要删除的图层组名称</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteWorkspaceLayerGroupAsync(string workspaceName, string layerGroupName);
    }
}
