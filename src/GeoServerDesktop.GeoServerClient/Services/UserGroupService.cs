using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing users and groups。
    /// FIXED-E7（路由族，3.0.1 源码 UsersRestController @RequestMapping("/rest/security/usergroup") 逐一确认）：
    ///  - GET  /users.json、/groups.json（列表，users 为对象数组、groups 为字符串数组）；
    ///  - 单用户无 GET 详情路由（GET users/{name}.json 实测 404）→ 由列表 + GET /user/{name}/groups.json 合成；
    ///  - 创建 POST /users（体 {"user":{...}}，enabled 必须显式否则 500）；更新 POST /user/{user}（实测 200；PUT 405/404）；
    ///    删除 DELETE /user/{user}（实测 200；复数 /users/{user} 404）；
    ///  - 组：POST /groups 实测 405，创建走 POST /group/{group}（实测 201）；删除 DELETE /group/{group}（实测 200）；
    ///    组详情无独立路由 → 由 GET /group/{group}/users.json 合成成员列表；
    ///  - 服务列表路由为 /rest/security/usergroupservices.json（UserGroupServiceController），旧 /usergroup/services 404。
    /// </summary>
    public class UserGroupService : ServiceBase, IUserGroupService
    {

        /// <summary>
        /// 初始化 UserGroupService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public UserGroupService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// Gets the list of user group services（FIXED-E7：3.0.1 路由 /rest/security/usergroupservices.json）
        /// </summary>
        /// <returns>List of user group services</returns>
        public async Task<UserGroupServiceList> GetServicesAsync()
        {
            var response = await Http.GetAsync("/rest/security/usergroupservices.json");
            return UserGroupServiceList.Parse(response);
        }

        /// <summary>
        /// Gets the list of all users。
        /// FIXED-E7：3.0.1 users.json 直接返回 <c>{"users":[{"enabled","password","userName"},...]}</c> 对象数组。
        /// </summary>
        /// <returns>List of users</returns>
        public async Task<UserListWrapper> GetUsersAsync()
        {
            var response = await Http.GetAsync("/rest/security/usergroup/users.json");
            return JsonConvert.DeserializeObject<UserListWrapper>(response);
        }

        /// <summary>
        /// 获取特定用户的详细信息。
        /// FIXED-E7：3.0.1 UsersRestController 无单用户 GET 路由（实测 GET users/{name}.json → 404），
        /// 因此从 users 列表中筛出该用户，并用 GET /user/{name}/groups.json 补组信息。
        /// 用户不存在时 <see cref="UserWrapper.User"/> 为 null。
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>用户详细信息</returns>
        public async Task<UserWrapper> GetUserAsync(string username)
        {
            var users = await GetUsersAsync();
            User match = null;
            if (users?.Users != null)
                foreach (var u in users.Users)
                    if (u != null && string.Equals(u.UserName, username, StringComparison.Ordinal)) { match = u; break; }
            if (match != null)
            {
                var gw = await GetJsonAsync<GroupListWrapper>($"/rest/security/usergroup/user/{Esc(username)}/groups.json");
                match.Groups = gw?.Groups;
            }
            return new UserWrapper { User = match };
        }

        /// <summary>
        /// 创建新的用户（POST /users，体 {"user":{...}}）。
        /// FIXED-E7：enabled 必须显式提供，否则 GeoServer 3.0.1 NPE → 500；null 时按默认 true 补齐。
        /// </summary>
        /// <param name="user">User to create</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateUserAsync(User user)
        {
            if (user != null && user.Enabled == null) user.Enabled = true;
            await PostJsonAsync("/rest/security/usergroup/users", new { user });
        }

        /// <summary>
        /// 更新现有的用户。
        /// FIXED-E7：3.0.1 更新路由为 POST /user/{user}（实测 200；PUT /users/{user} 404、PUT /user/{user} 405）。
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="user">Updated user information</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateUserAsync(string username, User user)
        {
            if (user != null && user.Enabled == null) user.Enabled = true;
            await PostJsonAsync($"/rest/security/usergroup/user/{Esc(username)}", new { user });
        }

        /// <summary>
        /// 删除用户（FIXED-E7：单数路由 DELETE /user/{user}，实测 200；复数 404）。
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteUserAsync(string username)
        {
            await Http.DeleteAsync($"/rest/security/usergroup/user/{Esc(username)}");
        }

        /// <summary>
        /// Gets the list of all groups（实测 3.0.1 groups.json 为 {"groups":["str",...]} 字符串数组）。
        /// </summary>
        /// <returns>List of groups</returns>
        public async Task<GroupListWrapper> GetGroupsAsync()
        {
            var response = await Http.GetAsync("/rest/security/usergroup/groups.json");
            return JsonConvert.DeserializeObject<GroupListWrapper>(response);
        }

        /// <summary>
        /// 获取特定组的详细信息。
        /// FIXED-E7（明确化）：3.0.1 UsersRestController 源码中组详情仅有 GET /group/{group}/users（成员列表），
        /// 且无任何组属性（enabled 等）读取路由；实测该成员路由对任意组名均回 Tomcat 400
        /// （/group/admin/users.json、/group/everyone/users.json 等一致），组详情通道在 3.0.1 实际不可用。
        /// 保留方法签名、恒抛 <see cref="NotSupportedException"/>。
        /// </summary>
        /// <param name="groupname">Group name</param>
        public Task<GroupWrapper> GetGroupAsync(string groupname) =>
            throw new NotSupportedException(
                "GeoServer 3.0.1 组详情无可用 REST 路由（/group/{group}/users 实测对任意组恒 400，且无组属性端点）。");

        /// <summary>
        /// 创建新的组（FIXED-E7：POST /group/{groupName}，实测 201；集合 POST /groups 实测 405）。
        /// </summary>
        /// <param name="group">Group to create</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateGroupAsync(UserGroup group)
        {
            if (group != null && group.Enabled == null) group.Enabled = true;
            await PostJsonAsync($"/rest/security/usergroup/group/{Esc(group?.GroupName)}", new { group });
        }

        /// <summary>
        /// 删除组（FIXED-E7：单数路由 DELETE /group/{group}，实测 200）。
        /// </summary>
        /// <param name="groupname">Group name</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteGroupAsync(string groupname)
        {
            await Http.DeleteAsync($"/rest/security/usergroup/group/{Esc(groupname)}");
        }
    }
}
