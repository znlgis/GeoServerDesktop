using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for retrieving GeoServer system information
    /// </summary>
    public class AboutService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the AboutService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public AboutService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets GeoServer version information
        /// </summary>
        /// <returns>Version information including GeoServer, GeoTools, and GeoWebCache versions</returns>
        public async Task<VersionInfoWrapper> GetVersionAsync()
        {
            var response = await _httpClient.GetAsync("/rest/about/version.json");
            return JsonConvert.DeserializeObject<VersionInfoWrapper>(response);
        }

        /// <summary>
        /// Gets the list of installed GeoServer modules and their versions
        /// </summary>
        /// <returns>Manifest information for all installed modules</returns>
        public async Task<ManifestsWrapper> GetManifestsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/about/manifests.json");
            return JsonConvert.DeserializeObject<ManifestsWrapper>(response);
        }

        /// <summary>
        /// Gets current system status including memory usage and JVM information
        /// </summary>
        /// <returns>System status information</returns>
        public async Task<SystemStatusWrapper> GetSystemStatusAsync()
        {
            var response = await _httpClient.GetAsync("/rest/about/system-status.json");
            return JsonConvert.DeserializeObject<SystemStatusWrapper>(response);
        }
    }
}
