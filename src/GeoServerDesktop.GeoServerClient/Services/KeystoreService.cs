using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing SSL/TLS keystores
    /// </summary>
    public class KeystoreService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 KeystoreService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public KeystoreService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the keystore information
        /// </summary>
        /// <returns>Keystore information</returns>
        public async Task<KeystoreInfo> GetKeystoreInfoAsync()
        {
            var response = await _httpClient.GetAsync("/rest/security/keystore.json");
            return JsonConvert.DeserializeObject<KeystoreInfo>(response);
        }

        /// <summary>
        /// Uploads a certificate to the keystore
        /// </summary>
        /// <param name="alias">Certificate alias</param>
        /// <param name="certificateData">Certificate data (PEM or DER format)</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UploadCertificateAsync(string alias, byte[] certificateData)
        {
            var content = new ByteArrayContent(certificateData);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-pkcs12");
            await _httpClient.PutAsync($"/rest/security/keystore/{alias}", content);
        }

        /// <summary>
        /// Deletes a certificate from the keystore
        /// </summary>
        /// <param name="alias">Certificate alias to delete</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteCertificateAsync(string alias)
        {
            await _httpClient.DeleteAsync($"/rest/security/keystore/{alias}");
        }
    }
}
