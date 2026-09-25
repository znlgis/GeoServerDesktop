using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IWorkspaceService 的服务接口（M5 接口化）：与实现类 WorkspaceService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IWorkspaceService
    {
        /// <summary>
        /// 获取所有工作空间的列表
        /// </summary>
        /// <returns>工作空间数组</returns>
        Task<Workspace[]> GetWorkspacesAsync();

        /// <summary>
        /// 获取特定工作空间的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <returns>工作空间详细信息</returns>
        Task<Workspace> GetWorkspaceAsync(string workspaceName);

        /// <summary>
        /// 创建新的工作空间
        /// </summary>
        /// <param name="workspaceName">要创建的工作空间名称</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateWorkspaceAsync(string workspaceName);

        /// <summary>
        /// 更新工作空间
        /// </summary>
        /// <param name="workspaceName">要更新的工作空间名称</param>
        /// <param name="newWorkspaceName">工作空间的新名称</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateWorkspaceAsync(string workspaceName, string newWorkspaceName);

        /// <summary>
        /// 删除工作空间
        /// </summary>
        /// <param name="workspaceName">要删除的工作空间名称</param>
        /// <param name="recurse">是否递归删除工作空间中的所有资源</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteWorkspaceAsync(string workspaceName, bool recurse = false);
    }
}
