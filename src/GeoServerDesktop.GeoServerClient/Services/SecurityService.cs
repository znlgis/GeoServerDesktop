using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing security access control lists。
    /// FIXED-E19（3.0.1 实测）：ACL 路由无 .json 后缀（GET /rest/security/acl/catalog 回 {"mode":"HIDE"}；
    /// acl/{layers|services|rest} 回扁平 {"&lt;资源模式&gt;":"逗号分隔角色"} map）；带 .json 后缀实测 404。
    /// 模型改用 <see cref="SecurityACL.Parse(string)"/> 承接两种形态。
    /// </summary>
    public class SecurityService : ServiceBase
    {

        /// <summary>
        /// 初始化 SecurityService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public SecurityService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// Gets the access control list for a specific resource（无 .json 后缀，FIXED-E19）
        /// </summary>
        /// <param name="resource">Resource path</param>
        /// <returns>Security ACL for the resource</returns>
        public async Task<SecurityACL> GetACLAsync(string resource)
        {
            var response = await Http.GetAsync($"/rest/security/acl/{resource}");
            return SecurityACL.Parse(response);
        }

        /// <summary>
        /// Sets the access control list for a specific resource。
        /// 请求体按 3.0.1 实际契约：catalog 模式发 <c>{"mode":"..."}</c>；其余资源发扁平 map
        /// （RawRules 优先，其次由 Rules 转译）。
        /// </summary>
        /// <param name="resource">Resource path</param>
        /// <param name="acl">Access control list to set</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task SetACLAsync(string resource, SecurityACL acl)
        {
            string json;
            if (acl == null) json = "{}";
            else if (acl.Mode != null) json = ToJson(new { mode = acl.Mode });
            else
            {
                var map = acl.RawRules;
                if (map == null)
                {
                    map = new Dictionary<string, string>();
                    if (acl.Rules != null)
                        foreach (var r in acl.Rules)
                            if (r != null && r.Role != null) map[r.Role] = r.Access;
                }
                json = ToJson(map);
            }
            await PostContentAsync($"/rest/security/acl/{resource}", TextContent(json, "application/json"));
        }

        /// <summary>
        /// Deletes the access control list for a specific resource
        /// </summary>
        /// <param name="resource">Resource path</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteACLAsync(string resource)
        {
            await Http.DeleteAsync($"/rest/security/acl/{resource}");
        }
    }
}
