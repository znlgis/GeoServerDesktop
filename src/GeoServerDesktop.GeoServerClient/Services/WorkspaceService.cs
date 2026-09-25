using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于管理 GeoServer 工作空间的服务
    /// </summary>
    public class WorkspaceService : ServiceBase
    {

        /// <summary>
        /// 初始化 WorkspaceService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public WorkspaceService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// 获取所有工作空间的列表
        /// </summary>
        /// <returns>工作空间数组</returns>
        public async Task<Workspace[]> GetWorkspacesAsync()
        {
            var wrapper = await GetJsonAsync<WorkspaceListWrapper>("/rest/workspaces.json");
            return wrapper?.WorkspaceList?.Workspaces ?? Array.Empty<Workspace>();
        }

        /// <summary>
        /// 获取特定工作空间的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <returns>工作空间详细信息</returns>
        public async Task<Workspace> GetWorkspaceAsync(string workspaceName)
        {
            var wrapper = await GetJsonAsync<WorkspaceWrapper>($"/rest/workspaces/{Esc(workspaceName)}.json");
            return wrapper?.Workspace;
        }

        /// <summary>
        /// 创建新的工作空间
        /// </summary>
        /// <param name="workspaceName">要创建的工作空间名称</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateWorkspaceAsync(string workspaceName)
        {
            var workspace = new { workspace = new { name = workspaceName } };
            await PostJsonAsync("/rest/workspaces", workspace);
        }

        /// <summary>
        /// 更新工作空间
        /// </summary>
        /// <param name="workspaceName">要更新的工作空间名称</param>
        /// <param name="newWorkspaceName">工作空间的新名称</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateWorkspaceAsync(string workspaceName, string newWorkspaceName)
        {
            var workspace = new { workspace = new { name = newWorkspaceName } };
            await PutJsonAsync($"/rest/workspaces/{Esc(workspaceName)}", workspace);
        }

        /// <summary>
        /// 删除工作空间
        /// </summary>
        /// <param name="workspaceName">要删除的工作空间名称</param>
        /// <param name="recurse">是否递归删除工作空间中的所有资源</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteWorkspaceAsync(string workspaceName, bool recurse = false)
        {
            var path = $"/rest/workspaces/{Esc(workspaceName)}?recurse={Bool(recurse)}";
            await Http.DeleteAsync(path);
        }
    }
}
