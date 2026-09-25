using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于管理 GeoServer 图层组的服务
    /// </summary>
    public class LayerGroupService : ServiceBase
    {

        /// <summary>
        /// 初始化 LayerGroupService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public LayerGroupService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// 获取所有图层组的列表
        /// </summary>
        /// <returns>图层组数组</returns>
        public async Task<LayerGroup[]> GetLayerGroupsAsync()
        {
            var wrapper = await GetJsonAsync<LayerGroupListWrapper>("/rest/layergroups.json");
            return wrapper?.LayerGroupList?.LayerGroups ?? Array.Empty<LayerGroup>();
        }

        /// <summary>
        /// 获取特定图层组的详细信息
        /// </summary>
        /// <param name="layerGroupName">图层组的名称</param>
        /// <returns>图层组详细信息</returns>
        public async Task<LayerGroup> GetLayerGroupAsync(string layerGroupName)
        {
            var wrapper = await GetJsonAsync<LayerGroupWrapper>($"/rest/layergroups/{Esc(layerGroupName)}.json");
            return wrapper?.LayerGroup;
        }

        /// <summary>
        /// 创建新的图层组
        /// </summary>
        /// <param name="layerGroup">图层组配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateLayerGroupAsync(LayerGroup layerGroup)
        {
            var wrapper = new { layerGroup = layerGroup };
            await PostJsonAsync("/rest/layergroups", wrapper);
        }

        /// <summary>
        /// 更新现有的图层组
        /// </summary>
        /// <param name="layerGroupName">图层组的名称</param>
        /// <param name="layerGroup">更新后的图层组配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateLayerGroupAsync(string layerGroupName, LayerGroup layerGroup)
        {
            var wrapper = new { layerGroup = layerGroup };
            await PutJsonAsync($"/rest/layergroups/{Esc(layerGroupName)}", wrapper);
        }

        /// <summary>
        /// 删除图层组
        /// </summary>
        /// <param name="layerGroupName">要删除的图层组名称</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteLayerGroupAsync(string layerGroupName)
        {
            await Http.DeleteAsync($"/rest/layergroups/{Esc(layerGroupName)}");
        }

        /// <summary>
        /// 获取特定工作空间中的图层组列表
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <returns>工作空间中的图层组数组</returns>
        public async Task<LayerGroup[]> GetWorkspaceLayerGroupsAsync(string workspaceName)
        {
            var wrapper = await GetJsonAsync<LayerGroupListWrapper>($"/rest/workspaces/{Esc(workspaceName)}/layergroups.json");
            return wrapper?.LayerGroupList?.LayerGroups ?? Array.Empty<LayerGroup>();
        }

        /// <summary>
        /// 获取工作空间中特定图层组的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="layerGroupName">图层组的名称</param>
        /// <returns>图层组详细信息</returns>
        public async Task<LayerGroup> GetWorkspaceLayerGroupAsync(string workspaceName, string layerGroupName)
        {
            var wrapper = await GetJsonAsync<LayerGroupWrapper>($"/rest/workspaces/{Esc(workspaceName)}/layergroups/{Esc(layerGroupName)}.json");
            return wrapper?.LayerGroup;
        }

        /// <summary>
        /// 在特定工作空间中创建新的图层组
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="layerGroup">图层组配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateWorkspaceLayerGroupAsync(string workspaceName, LayerGroup layerGroup)
        {
            var wrapper = new { layerGroup = layerGroup };
            await PostJsonAsync($"/rest/workspaces/{Esc(workspaceName)}/layergroups", wrapper);
        }

        /// <summary>
        /// 更新工作空间中的现有图层组
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="layerGroupName">图层组的名称</param>
        /// <param name="layerGroup">更新后的图层组配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateWorkspaceLayerGroupAsync(string workspaceName, string layerGroupName, LayerGroup layerGroup)
        {
            var wrapper = new { layerGroup = layerGroup };
            await PutJsonAsync($"/rest/workspaces/{Esc(workspaceName)}/layergroups/{Esc(layerGroupName)}", wrapper);
        }

        /// <summary>
        /// 从工作空间中删除图层组
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="layerGroupName">要删除的图层组名称</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteWorkspaceLayerGroupAsync(string workspaceName, string layerGroupName)
        {
            await Http.DeleteAsync($"/rest/workspaces/{Esc(workspaceName)}/layergroups/{Esc(layerGroupName)}");
        }
    }
}
