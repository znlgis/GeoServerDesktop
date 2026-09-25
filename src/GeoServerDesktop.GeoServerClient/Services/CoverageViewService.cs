using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing coverage views
    /// </summary>
    public class CoverageViewService : ServiceBase, ICoverageViewService
    {

        /// <summary>
        /// 初始化 CoverageViewService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public CoverageViewService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// Gets the list of all coverage views in a workspace
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <returns>List of coverage views</returns>
        public async Task<CoverageViewListWrapper> GetCoverageViewsAsync(string workspace)
        {
            var response = await Http.GetAsync($"/rest/workspaces/{Esc(workspace)}/coverageviews.json");
            return JsonConvert.DeserializeObject<CoverageViewListWrapper>(response);
        }

        /// <summary>
        /// 获取特定覆盖范围的详细信息 view
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageView">Coverage view name</param>
        /// <returns>覆盖范围视图详细信息</returns>
        public async Task<CoverageViewWrapper> GetCoverageViewAsync(string workspace, string coverageView)
        {
            var response = await Http.GetAsync($"/rest/workspaces/{Esc(workspace)}/coverageviews/{Esc(coverageView)}.json");
            return JsonConvert.DeserializeObject<CoverageViewWrapper>(response);
        }

        /// <summary>
        /// 创建新的覆盖范围 view
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageView">Coverage view to create</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateCoverageViewAsync(string workspace, CoverageView coverageView)
        {
            var wrapper = new { coverageView = coverageView };
            await PostJsonAsync($"/rest/workspaces/{Esc(workspace)}/coverageviews", wrapper);
        }

        /// <summary>
        /// 更新现有的覆盖范围 view
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageViewName">Coverage view name</param>
        /// <param name="coverageView">Updated coverage view information</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateCoverageViewAsync(string workspace, string coverageViewName, CoverageView coverageView)
        {
            var wrapper = new { coverageView = coverageView };
            await PutJsonAsync($"/rest/workspaces/{Esc(workspace)}/coverageviews/{Esc(coverageViewName)}", wrapper);
        }

        /// <summary>
        /// 删除覆盖范围 view
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="coverageView">Coverage view name to delete</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteCoverageViewAsync(string workspace, string coverageView)
        {
            await Http.DeleteAsync($"/rest/workspaces/{Esc(workspace)}/coverageviews/{Esc(coverageView)}");
        }
    }
}
