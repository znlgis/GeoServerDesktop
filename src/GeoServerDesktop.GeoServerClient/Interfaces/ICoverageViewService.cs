using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// ICoverageViewService 的服务接口（M5 接口化）：与实现类 CoverageViewService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface ICoverageViewService
    {
        /// <summary>
        /// Gets the list of all coverage views in a workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <returns>List of coverage views</returns>
        Task<CoverageViewListWrapper> GetCoverageViewsAsync(string workspace);

        /// <summary>
        /// 获取特定覆盖范围的详细信息 view
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageView">Coverage view name</param>
        /// <returns>覆盖范围视图详细信息</returns>
        Task<CoverageViewWrapper> GetCoverageViewAsync(string workspace, string coverageView);

        /// <summary>
        /// 创建新的覆盖范围 view
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageView">Coverage view to create</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateCoverageViewAsync(string workspace, CoverageView coverageView);

        /// <summary>
        /// 更新现有的覆盖范围 view
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageViewName">Coverage view name</param>
        /// <param name="coverageView">Updated coverage view information</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateCoverageViewAsync(string workspace, string coverageViewName, CoverageView coverageView);

        /// <summary>
        /// 删除覆盖范围 view
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageView">Coverage view name to delete</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteCoverageViewAsync(string workspace, string coverageView);
    }
}
