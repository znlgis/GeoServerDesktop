using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IRoleService 的服务接口（M5 接口化）：与实现类 RoleService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IRoleService
    {
        /// <summary>
        /// Gets the list of all roles
        /// </summary>
        /// <returns>List of roles</returns>
        Task<RoleListWrapper> GetRolesAsync();

        /// <summary>
        /// Gets the roles for a specific user
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>List of roles for the user</returns>
        Task<RoleListWrapper> GetUserRolesAsync(string username);

        /// <summary>
        /// Associates a role with a user
        /// </summary>
        /// <param name="rolename">Role name</param>
        /// <param name="username">Username</param>
        /// <returns>表示异步操作的任务</returns>
        Task AssociateRoleAsync(string rolename, string username);

        /// <summary>
        /// Dissociates a role from a user
        /// </summary>
        /// <param name="rolename">Role name</param>
        /// <param name="username">Username</param>
        /// <returns>表示异步操作的任务</returns>
        Task DissociateRoleAsync(string rolename, string username);
    }
}
