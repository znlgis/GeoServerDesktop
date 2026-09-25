using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// ISecurityService 的服务接口（M5 接口化）：与实现类 SecurityService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface ISecurityService
    {
        /// <summary>
        /// Gets the access control list for a specific resource（无 .json 后缀，FIXED-E19）
        /// </summary>
        /// <param name="resource">Resource path</param>
        /// <returns>Security ACL for the resource</returns>
        Task<SecurityACL> GetACLAsync(string resource);

        /// <summary>
        /// Sets the access control list for a specific resource。
        /// 请求体按 3.0.1 实际契约：catalog 模式发 <c>{"mode":"..."}</c>；其余资源发扁平 map
        /// （RawRules 优先，其次由 Rules 转译）。
        /// </summary>
        /// <param name="resource">Resource path</param>
        /// <param name="acl">Access control list to set</param>
        /// <returns>表示异步操作的任务</returns>
        Task SetACLAsync(string resource, SecurityACL acl);

        /// <summary>
        /// Deletes the access control list for a specific resource
        /// </summary>
        /// <param name="resource">Resource path</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteACLAsync(string resource);
    }
}
