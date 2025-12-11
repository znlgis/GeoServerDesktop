using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing users and groups
    /// </summary>
    public class UserGroupService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 UserGroupService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public UserGroupService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the list of user group services
        /// </summary>
        /// <returns>List of user group services</returns>
        public async Task<UserGroupServiceList> GetServicesAsync()
        {
            var response = await _httpClient.GetAsync("/rest/security/usergroup/services.json");
            return JsonConvert.DeserializeObject<UserGroupServiceList>(response);
        }

        /// <summary>
        /// Gets the list of all users
        /// </summary>
        /// <returns>List of users</returns>
        public async Task<UserListWrapper> GetUsersAsync()
        {
            var response = await _httpClient.GetAsync("/rest/security/usergroup/users.json");
            return JsonConvert.DeserializeObject<UserListWrapper>(response);
        }

        /// <summary>
        /// 获取特定用户的详细信息
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>用户详细信息</returns>
        public async Task<UserWrapper> GetUserAsync(string username)
        {
            var response = await _httpClient.GetAsync($"/rest/security/usergroup/users/{username}.json");
            return JsonConvert.DeserializeObject<UserWrapper>(response);
        }

        /// <summary>
        /// 创建新的用户
        /// </summary>
        /// <param name="user">User to create</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateUserAsync(User user)
        {
            var wrapper = new { user = user };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/rest/security/usergroup/users", content);
        }

        /// <summary>
        /// 更新现有的用户
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="user">Updated user information</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateUserAsync(string username, User user)
        {
            var wrapper = new { user = user };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/security/usergroup/users/{username}", content);
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteUserAsync(string username)
        {
            await _httpClient.DeleteAsync($"/rest/security/usergroup/users/{username}");
        }

        /// <summary>
        /// Gets the list of all groups
        /// </summary>
        /// <returns>List of groups</returns>
        public async Task<GroupListWrapper> GetGroupsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/security/usergroup/groups.json");
            return JsonConvert.DeserializeObject<GroupListWrapper>(response);
        }

        /// <summary>
        /// 获取特定组的详细信息
        /// </summary>
        /// <param name="groupname">Group name</param>
        /// <returns>组详细信息</returns>
        public async Task<GroupWrapper> GetGroupAsync(string groupname)
        {
            var response = await _httpClient.GetAsync($"/rest/security/usergroup/groups/{groupname}.json");
            return JsonConvert.DeserializeObject<GroupWrapper>(response);
        }

        /// <summary>
        /// 创建新的组
        /// </summary>
        /// <param name="group">Group to create</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateGroupAsync(UserGroup group)
        {
            var wrapper = new { group = group };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/rest/security/usergroup/groups", content);
        }

        /// <summary>
        /// 删除组
        /// </summary>
        /// <param name="groupname">Group name</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteGroupAsync(string groupname)
        {
            await _httpClient.DeleteAsync($"/rest/security/usergroup/groups/{groupname}");
        }
    }
}
