using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于管理用户密码的服务
    /// </summary>
    public class PasswordService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 PasswordService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public PasswordService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// 更改当前用户的密码
        /// </summary>
        /// <param name="newPassword">新密码</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task ChangePasswordAsync(string newPassword)
        {
            var request = new PasswordChangeRequest { NewPassword = newPassword };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync("/rest/security/self/password", content);
        }
    }
}
