using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing security access control lists
    /// </summary>
    public class SecurityService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the SecurityService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public SecurityService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the access control list for a specific resource
        /// </summary>
        /// <param name="resource">Resource path</param>
        /// <returns>Security ACL for the resource</returns>
        public async Task<SecurityACL> GetACLAsync(string resource)
        {
            var response = await _httpClient.GetAsync($"/rest/security/acl/{resource}.json");
            return JsonConvert.DeserializeObject<SecurityACL>(response);
        }

        /// <summary>
        /// Sets the access control list for a specific resource
        /// </summary>
        /// <param name="resource">Resource path</param>
        /// <param name="acl">Access control list to set</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task SetACLAsync(string resource, SecurityACL acl)
        {
            var json = JsonConvert.SerializeObject(acl);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/security/acl/{resource}", content);
        }

        /// <summary>
        /// Deletes the access control list for a specific resource
        /// </summary>
        /// <param name="resource">Resource path</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteACLAsync(string resource)
        {
            await _httpClient.DeleteAsync($"/rest/security/acl/{resource}");
        }
    }
}
