using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing user passwords
    /// </summary>
    public class PasswordService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the PasswordService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public PasswordService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Changes the password for the current user
        /// </summary>
        /// <param name="newPassword">New password</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task ChangePasswordAsync(string newPassword)
        {
            var request = new PasswordChangeRequest { NewPassword = newPassword };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync("/rest/security/self/password", content);
        }
    }
}
