using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing feature templates
    /// </summary>
    public class TemplateService : ServiceBase
    {

        /// <summary>
        /// 初始化 TemplateService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public TemplateService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// Gets the list of available templates
        /// </summary>
        /// <returns>List of templates</returns>
        public async Task<TemplateListWrapper> GetTemplatesAsync()
        {
            var response = await Http.GetAsync("/rest/templates.json");
            // FIXED-E7：3.0.1 根为类全名 {"org.geoserver.rest.catalog.TemplateInfos":...}（空态值为 ""），
            // 直接反序列化取不到根；改用容错 Parse。
            return TemplateListWrapper.Parse(response);
        }

        /// <summary>
        /// Gets a specific template content
        /// </summary>
        /// <param name="templateName">Template name</param>
        /// <returns>Template content</returns>
        public async Task<string> GetTemplateAsync(string templateName)
        {
            return await Http.GetAsync($"/rest/templates/{templateName}");
        }

        /// <summary>
        /// Creates a new template
        /// </summary>
        /// <param name="templateName">Template name</param>
        /// <param name="content">Template content</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateTemplateAsync(string templateName, string content)
        {
            await PostContentAsync($"/rest/templates/{templateName}", TextContent(content, "text/plain"));
        }

        /// <summary>
        /// Deletes a template
        /// </summary>
        /// <param name="templateName">Template name</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteTemplateAsync(string templateName)
        {
            await Http.DeleteAsync($"/rest/templates/{templateName}");
        }
    }
}
