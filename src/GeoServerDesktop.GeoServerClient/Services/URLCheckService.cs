using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing URL validation checks
    /// </summary>
    public class URLCheckService : ServiceBase
    {

        /// <summary>
        /// 初始化 URLCheckService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public URLCheckService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// Gets the list of URL validation checks
        /// </summary>
        /// <returns>List of URL checks</returns>
        public async Task<URLCheckListWrapper> GetURLChecksAsync()
        {
            var response = await Http.GetAsync("/rest/urlchecks.json");
            // FIXED-E7：3.0.1 根键为 urlChecks 且空态为 {"urlChecks":""}，改用容错 Parse
            return URLCheckListWrapper.Parse(response);
        }

        /// <summary>
        /// Creates a new URL validation check。
        /// 说明（E20 维持基线）：3.0.1 UrlCheckController（org.geoserver.rest.security）POST /rest/urlchecks
        /// 以 @RequestBody AbstractUrlCheck（XStream 具体实现类根）接收，默认安装无任何样例，实测
        /// {"urlCheck":{...}} 报 "Cannot construct type"——请求体形态无法在不引入私有实现类的前提下固化，
        /// 故维持当前未包装对象请求体基线，创建可用性以真实环境验证为准。
        /// </summary>
        /// <param name="check">URL check to create</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateURLCheckAsync(URLCheck check)
        {
            await PostJsonAsync("/rest/urlchecks", check);
        }

        /// <summary>
        /// Deletes a URL validation check
        /// </summary>
        /// <param name="checkName">Check name</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteURLCheckAsync(string checkName)
        {
            await Http.DeleteAsync($"/rest/urlchecks/{checkName}");
        }
    }
}
