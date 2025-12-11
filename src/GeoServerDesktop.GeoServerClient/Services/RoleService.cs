using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing security roles
    /// </summary>
    public class RoleService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 RoleService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public RoleService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the list of all roles
        /// </summary>
        /// <returns>List of roles</returns>
        public async Task<RoleListWrapper> GetRolesAsync()
        {
            var response = await _httpClient.GetAsync("/rest/security/roles.json");
            return JsonConvert.DeserializeObject<RoleListWrapper>(response);
        }

        /// <summary>
        /// Gets the roles for a specific user
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>List of roles for the user</returns>
        public async Task<RoleListWrapper> GetUserRolesAsync(string username)
        {
            var response = await _httpClient.GetAsync($"/rest/security/roles/user/{username}.json");
            return JsonConvert.DeserializeObject<RoleListWrapper>(response);
        }

        /// <summary>
        /// Associates a role with a user
        /// </summary>
        /// <param name="rolename">Role name</param>
        /// <param name="username">Username</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task AssociateRoleAsync(string rolename, string username)
        {
            await _httpClient.PostAsync($"/rest/security/roles/role/{rolename}/user/{username}", null);
        }

        /// <summary>
        /// Dissociates a role from a user
        /// </summary>
        /// <param name="rolename">Role name</param>
        /// <param name="username">Username</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DissociateRoleAsync(string rolename, string username)
        {
            await _httpClient.DeleteAsync($"/rest/security/roles/role/{rolename}/user/{username}");
        }
    }
}
