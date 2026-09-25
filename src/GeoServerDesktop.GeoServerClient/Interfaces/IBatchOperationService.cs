using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IBatchOperationService 的服务接口（M5 接口化）：与实现类 BatchOperationService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IBatchOperationService
    {
        /// <summary>
        /// 批量设置图层默认样式（PUT 局部更新：layer.name + defaultStyle）。
        /// </summary>
        /// <param name="qualifiedLayerNames">图层 qualified 名列表（workspace:layer，与 /rest/layers.json 一致）。</param>
        /// <param name="styleName">默认样式名（全局样式裸名；工作空间样式可带 workspace/ 前缀由调用方解析，此处直接透传名字）。</param>
        /// <returns>批量结果。</returns>
        Task<BatchResult> SetLayersDefaultStyleAsync(IEnumerable<string> qualifiedLayerNames, string styleName);

        /// <summary>
        /// 批量启用/禁用存储（dataStore 或 coverageStore 的 enabled 键；见类注释实测结论②——
        /// 图层级启停无 REST 通道，存储级启停即为对用户可用的"批量启停"面）。
        /// qualified 名（workspace:store）的目标先按 dataStore 尝试，404 时回落 coverageStore。
        /// </summary>
        /// <param name="targets">存储目标列表（Workspace 或 qualified Name）。</param>
        /// <param name="enabled">true 启用，false 禁用。</param>
        /// <returns>批量结果。</returns>
        Task<BatchResult> SetStoresEnabledAsync(IEnumerable<BatchTarget> targets, bool enabled);

        /// <summary>
        /// 批量删除图层。
        /// </summary>
        /// <param name="qualifiedLayerNames">图层 qualified 名列表。</param>
        /// <param name="recurse">是否级联删除关联资源。</param>
        /// <returns>批量结果。</returns>
        Task<BatchResult> DeleteLayersAsync(IEnumerable<string> qualifiedLayerNames, bool recurse = false);

        /// <summary>
        /// 批量删除样式（全局样式与 workspace/ 前缀的工作空间样式混合；引用中的样式按 403 失败项上报）。
        /// </summary>
        /// <param name="targets">样式目标列表。</param>
        /// <param name="purge">是否同时清除磁盘上的样式文件。</param>
        /// <returns>批量结果。</returns>
        Task<BatchResult> DeleteStylesAsync(IEnumerable<BatchTarget> targets, bool purge = false);

        /// <summary>
        /// 批量删除工作空间（recurse=true 级联其下存储/图层/工作空间样式/图层组，并回收孤立命名空间）。
        /// </summary>
        /// <param name="workspaceNames">工作空间名列表。</param>
        /// <param name="recurse">是否递归删除工作空间内全部资源。</param>
        /// <returns>批量结果。</returns>
        Task<BatchResult> DeleteWorkspacesAsync(IEnumerable<string> workspaceNames, bool recurse = true);
    }
}
