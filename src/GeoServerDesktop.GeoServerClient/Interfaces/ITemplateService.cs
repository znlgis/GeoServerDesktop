using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// ITemplateService 的服务接口（M5 接口化）：与实现类 TemplateService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface ITemplateService
    {
        /// <summary>
        /// Gets the list of available templates
        /// </summary>
        /// <returns>List of templates</returns>
        Task<TemplateListWrapper> GetTemplatesAsync();

        /// <summary>
        /// Gets a specific template content
        /// </summary>
        /// <param name="templateName">Template name</param>
        /// <returns>Template content</returns>
        Task<string> GetTemplateAsync(string templateName);

        /// <summary>
        /// Creates a new template
        /// </summary>
        /// <param name="templateName">Template name</param>
        /// <param name="content">Template content</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateTemplateAsync(string templateName, string content);

        /// <summary>
        /// Deletes a template
        /// </summary>
        /// <param name="templateName">Template name</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteTemplateAsync(string templateName);
    }
}
