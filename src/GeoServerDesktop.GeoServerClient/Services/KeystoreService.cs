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
        /// Initializes a new instance of the KeystoreService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
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
        /// <returns>Task representing the asynchronous operation</returns>
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
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteCertificateAsync(string alias)
        {
            await _httpClient.DeleteAsync($"/rest/security/keystore/{alias}");
        }
    }
}
