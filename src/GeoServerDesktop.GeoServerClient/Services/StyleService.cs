using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于管理 GeoServer 样式的服务
    /// </summary>
    public class StyleService : ServiceBase
    {

        /// <summary>
        /// 初始化 StyleService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public StyleService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// 获取所有样式的列表
        /// </summary>
        /// <returns>样式数组</returns>
        public async Task<Style[]> GetStylesAsync()
        {
            var wrapper = await GetJsonAsync<StyleListWrapper>("/rest/styles.json");
            return wrapper?.StyleList?.Styles ?? Array.Empty<Style>();
        }

        /// <summary>
        /// 获取特定样式的详细信息
        /// </summary>
        /// <param name="styleName">样式的名称</param>
        /// <returns>样式详细信息</returns>
        public async Task<Style> GetStyleAsync(string styleName)
        {
            var wrapper = await GetJsonAsync<StyleWrapper>($"/rest/styles/{Esc(styleName)}.json");
            return wrapper?.Style;
        }

        /// <summary>
        /// 获取特定样式的 SLD 内容
        /// </summary>
        /// <param name="styleName">样式的名称</param>
        /// <returns>SLD 内容字符串</returns>
        public async Task<string> GetStyleSldAsync(string styleName)
        {
            return await GetAsync($"/rest/styles/{Esc(styleName)}.sld");
        }

        /// <summary>
        /// 创建新样式
        /// </summary>
        /// <param name="styleName">要创建的样式名称</param>
        /// <param name="sldContent">样式的 SLD 内容</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateStyleAsync(string styleName, string sldContent)
        {
            // 首先创建样式元数据
            var style = new { style = new { name = styleName, filename = $"{styleName}.sld" } };
            await PostJsonAsync("/rest/styles", style);

            // 然后上传 SLD 内容
            await PutContentAsync($"/rest/styles/{Esc(styleName)}", TextContent(sldContent, "application/vnd.ogc.sld+xml"));
        }

        /// <summary>
        /// 更新现有样式
        /// </summary>
        /// <param name="styleName">样式的名称</param>
        /// <param name="sldContent">更新后的 SLD 内容</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateStyleAsync(string styleName, string sldContent)
        {
            await PutContentAsync($"/rest/styles/{Esc(styleName)}", TextContent(sldContent, "application/vnd.ogc.sld+xml"));
        }

        /// <summary>
        /// 删除样式
        /// </summary>
        /// <param name="styleName">要删除的样式名称</param>
        /// <param name="purge">是否从磁盘清除样式文件</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteStyleAsync(string styleName, bool purge = false)
        {
            var path = $"/rest/styles/{Esc(styleName)}?purge={Bool(purge)}";
            await Http.DeleteAsync(path);
        }

        /// <summary>
        /// 获取特定工作空间中的样式列表
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <returns>工作空间中的样式数组</returns>
        public async Task<Style[]> GetWorkspaceStylesAsync(string workspaceName)
        {
            var wrapper = await GetJsonAsync<StyleListWrapper>($"/rest/workspaces/{Esc(workspaceName)}/styles.json");
            return wrapper?.StyleList?.Styles ?? Array.Empty<Style>();
        }

        /// <summary>
        /// 获取工作空间中特定样式的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="styleName">样式的名称</param>
        /// <returns>样式详细信息</returns>
        public async Task<Style> GetWorkspaceStyleAsync(string workspaceName, string styleName)
        {
            var wrapper = await GetJsonAsync<StyleWrapper>($"/rest/workspaces/{Esc(workspaceName)}/styles/{Esc(styleName)}.json");
            return wrapper?.Style;
        }

        /// <summary>
        /// 获取工作空间中特定样式的 SLD 内容
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="styleName">样式的名称</param>
        /// <returns>SLD 内容字符串</returns>
        public async Task<string> GetWorkspaceStyleSldAsync(string workspaceName, string styleName)
        {
            return await GetAsync($"/rest/workspaces/{Esc(workspaceName)}/styles/{Esc(styleName)}.sld");
        }

        /// <summary>
        /// 在特定工作空间中创建新样式
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="styleName">要创建的样式名称</param>
        /// <param name="sldContent">样式的 SLD 内容</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateWorkspaceStyleAsync(string workspaceName, string styleName, string sldContent)
        {
            // 首先创建样式元数据
            var style = new { style = new { name = styleName, filename = $"{styleName}.sld" } };
            await PostJsonAsync($"/rest/workspaces/{Esc(workspaceName)}/styles", style);

            // 然后上传 SLD 内容
            await PutContentAsync($"/rest/workspaces/{Esc(workspaceName)}/styles/{Esc(styleName)}", TextContent(sldContent, "application/vnd.ogc.sld+xml"));
        }

        /// <summary>
        /// 更新工作空间中的现有样式
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="styleName">样式的名称</param>
        /// <param name="sldContent">更新后的 SLD 内容</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateWorkspaceStyleAsync(string workspaceName, string styleName, string sldContent)
        {
            await PutContentAsync($"/rest/workspaces/{Esc(workspaceName)}/styles/{Esc(styleName)}", TextContent(sldContent, "application/vnd.ogc.sld+xml"));
        }

        /// <summary>
        /// 从工作空间中删除样式
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="styleName">要删除的样式名称</param>
        /// <param name="purge">是否从磁盘清除样式文件</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteWorkspaceStyleAsync(string workspaceName, string styleName, bool purge = false)
        {
            var path = $"/rest/workspaces/{Esc(workspaceName)}/styles/{Esc(styleName)}?purge={Bool(purge)}";
            await Http.DeleteAsync(path);
        }
    }
}
