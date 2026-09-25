using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IURLCheckService 的服务接口（M5 接口化）：与实现类 URLCheckService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IURLCheckService
    {
        /// <summary>
        /// Gets the list of URL validation checks
        /// </summary>
        /// <returns>List of URL checks</returns>
        Task<URLCheckListWrapper> GetURLChecksAsync();

        /// <summary>
        /// Creates a new URL validation check。
        /// 说明（E20 维持基线）：3.0.1 UrlCheckController（org.geoserver.rest.security）POST /rest/urlchecks
        /// 以 @RequestBody AbstractUrlCheck（XStream 具体实现类根）接收，默认安装无任何样例，实测
        /// {"urlCheck":{...}} 报 "Cannot construct type"——请求体形态无法在不引入私有实现类的前提下固化，
        /// 故维持当前未包装对象请求体基线，创建可用性以真实环境验证为准。
        /// </summary>
        /// <param name="check">URL check to create</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateURLCheckAsync(URLCheck check);

        /// <summary>
        /// Deletes a URL validation check
        /// </summary>
        /// <param name="checkName">Check name</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteURLCheckAsync(string checkName);
    }
}
