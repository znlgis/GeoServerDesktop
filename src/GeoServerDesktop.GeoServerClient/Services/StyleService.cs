using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于管理 GeoServer 样式的服务
    /// </summary>
    public class StyleService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 StyleService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public StyleService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// 获取所有样式的列表
        /// </summary>
        /// <returns>样式数组</returns>
        public async Task<Style[]> GetStylesAsync()
        {
            var response = await _httpClient.GetAsync("/rest/styles.json");
            var wrapper = JsonConvert.DeserializeObject<StyleListWrapper>(response);
            return wrapper?.StyleList?.Styles ?? new Style[0];
        }

        /// <summary>
        /// 获取特定样式的详细信息
        /// </summary>
        /// <param name="styleName">样式的名称</param>
        /// <returns>样式详细信息</returns>
        public async Task<Style> GetStyleAsync(string styleName)
        {
            var response = await _httpClient.GetAsync($"/rest/styles/{styleName}.json");
            var wrapper = JsonConvert.DeserializeObject<StyleWrapper>(response);
            return wrapper?.Style;
        }

        /// <summary>
        /// 获取特定样式的 SLD 内容
        /// </summary>
        /// <param name="styleName">样式的名称</param>
        /// <returns>SLD 内容字符串</returns>
        public async Task<string> GetStyleSldAsync(string styleName)
        {
            var response = await _httpClient.GetAsync($"/rest/styles/{styleName}.sld");
            return response;
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
            var json = JsonConvert.SerializeObject(style);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/rest/styles", content);

            // 然后上传 SLD 内容
            var sldContentData = new StringContent(sldContent, Encoding.UTF8, "application/vnd.ogc.sld+xml");
            await _httpClient.PutAsync($"/rest/styles/{styleName}", sldContentData);
        }

        /// <summary>
        /// 更新现有样式
        /// </summary>
        /// <param name="styleName">样式的名称</param>
        /// <param name="sldContent">更新后的 SLD 内容</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateStyleAsync(string styleName, string sldContent)
        {
            var content = new StringContent(sldContent, Encoding.UTF8, "application/vnd.ogc.sld+xml");
            await _httpClient.PutAsync($"/rest/styles/{styleName}", content);
        }

        /// <summary>
        /// 删除样式
        /// </summary>
        /// <param name="styleName">要删除的样式名称</param>
        /// <param name="purge">是否从磁盘清除样式文件</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteStyleAsync(string styleName, bool purge = false)
        {
            var path = $"/rest/styles/{styleName}?purge={purge.ToString().ToLower()}";
            await _httpClient.DeleteAsync(path);
        }

        /// <summary>
        /// 获取特定工作空间中的样式列表
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <returns>工作空间中的样式数组</returns>
        public async Task<Style[]> GetWorkspaceStylesAsync(string workspaceName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/styles.json");
            var wrapper = JsonConvert.DeserializeObject<StyleListWrapper>(response);
            return wrapper?.StyleList?.Styles ?? new Style[0];
        }

        /// <summary>
        /// 获取工作空间中特定样式的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="styleName">样式的名称</param>
        /// <returns>样式详细信息</returns>
        public async Task<Style> GetWorkspaceStyleAsync(string workspaceName, string styleName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/styles/{styleName}.json");
            var wrapper = JsonConvert.DeserializeObject<StyleWrapper>(response);
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
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/styles/{styleName}.sld");
            return response;
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
            var json = JsonConvert.SerializeObject(style);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/workspaces/{workspaceName}/styles", content);

            // 然后上传 SLD 内容
            var sldContentData = new StringContent(sldContent, Encoding.UTF8, "application/vnd.ogc.sld+xml");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/styles/{styleName}", sldContentData);
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
            var content = new StringContent(sldContent, Encoding.UTF8, "application/vnd.ogc.sld+xml");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/styles/{styleName}", content);
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
            var path = $"/rest/workspaces/{workspaceName}/styles/{styleName}?purge={purge.ToString().ToLower()}";
            await _httpClient.DeleteAsync(path);
        }
    }
}
