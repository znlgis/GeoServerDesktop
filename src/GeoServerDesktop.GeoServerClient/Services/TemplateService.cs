using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing feature templates
    /// </summary>
    public class TemplateService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 TemplateService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public TemplateService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the list of available templates
        /// </summary>
        /// <returns>List of templates</returns>
        public async Task<TemplateListWrapper> GetTemplatesAsync()
        {
            var response = await _httpClient.GetAsync("/rest/templates.json");
            return JsonConvert.DeserializeObject<TemplateListWrapper>(response);
        }

        /// <summary>
        /// Gets a specific template content
        /// </summary>
        /// <param name="templateName">Template name</param>
        /// <returns>Template content</returns>
        public async Task<string> GetTemplateAsync(string templateName)
        {
            return await _httpClient.GetAsync($"/rest/templates/{templateName}");
        }

        /// <summary>
        /// Creates a new template
        /// </summary>
        /// <param name="templateName">Template name</param>
        /// <param name="content">Template content</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateTemplateAsync(string templateName, string content)
        {
            var stringContent = new StringContent(content, Encoding.UTF8, "text/plain");
            await _httpClient.PostAsync($"/rest/templates/{templateName}", stringContent);
        }

        /// <summary>
        /// Deletes a template
        /// </summary>
        /// <param name="templateName">Template name</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteTemplateAsync(string templateName)
        {
            await _httpClient.DeleteAsync($"/rest/templates/{templateName}");
        }
    }
}
