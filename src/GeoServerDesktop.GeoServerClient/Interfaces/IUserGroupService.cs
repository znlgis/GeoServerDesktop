using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IUserGroupService 的服务接口（M5 接口化）：与实现类 UserGroupService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IUserGroupService
    {
        /// <summary>
        /// Gets the list of user group services（FIXED-E7：3.0.1 路由 /rest/security/usergroupservices.json）
        /// </summary>
        /// <returns>List of user group services</returns>
        Task<UserGroupServiceList> GetServicesAsync();

        /// <summary>
        /// Gets the list of all users。
        /// FIXED-E7：3.0.1 users.json 直接返回 <c>{"users":[{"enabled","password","userName"},...]}</c> 对象数组。
        /// </summary>
        /// <returns>List of users</returns>
        Task<UserListWrapper> GetUsersAsync();

        /// <summary>
        /// 获取特定用户的详细信息。
        /// FIXED-E7：3.0.1 UsersRestController 无单用户 GET 路由（实测 GET users/{name}.json → 404），
        /// 因此从 users 列表中筛出该用户，并用 GET /user/{name}/groups.json 补组信息。
        /// 用户不存在时 <see cref="UserWrapper.User"/> 为 null。
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>用户详细信息</returns>
        Task<UserWrapper> GetUserAsync(string username);

        /// <summary>
        /// 创建新的用户（POST /users，体 {"user":{...}}）。
        /// FIXED-E7：enabled 必须显式提供，否则 GeoServer 3.0.1 NPE → 500；null 时按默认 true 补齐。
        /// </summary>
        /// <param name="user">User to create</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateUserAsync(User user);

        /// <summary>
        /// 更新现有的用户。
        /// FIXED-E7：3.0.1 更新路由为 POST /user/{user}（实测 200；PUT /users/{user} 404、PUT /user/{user} 405）。
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="user">Updated user information</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateUserAsync(string username, User user);

        /// <summary>
        /// 删除用户（FIXED-E7：单数路由 DELETE /user/{user}，实测 200；复数 404）。
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteUserAsync(string username);

        /// <summary>
        /// Gets the list of all groups（实测 3.0.1 groups.json 为 {"groups":["str",...]} 字符串数组）。
        /// </summary>
        /// <returns>List of groups</returns>
        Task<GroupListWrapper> GetGroupsAsync();

        /// <summary>
        /// 获取特定组的详细信息。
        /// FIXED-E7（明确化）：3.0.1 UsersRestController 源码中组详情仅有 GET /group/{group}/users（成员列表），
        /// 且无任何组属性（enabled 等）读取路由；实测该成员路由对任意组名均回 Tomcat 400
        /// （/group/admin/users.json、/group/everyone/users.json 等一致），组详情通道在 3.0.1 实际不可用。
        /// 保留方法签名、恒抛 <see cref="NotSupportedException"/>。
        /// </summary>
        /// <param name="groupname">Group name</param>
        Task<GroupWrapper> GetGroupAsync(string groupname);

        /// <summary>
        /// 创建新的组（FIXED-E7：POST /group/{groupName}，实测 201；集合 POST /groups 实测 405）。
        /// </summary>
        /// <param name="group">Group to create</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateGroupAsync(UserGroup group);

        /// <summary>
        /// 删除组（FIXED-E7：单数路由 DELETE /group/{group}，实测 200）。
        /// </summary>
        /// <param name="groupname">Group name</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteGroupAsync(string groupname);
    }
}
