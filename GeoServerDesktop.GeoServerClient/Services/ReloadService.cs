using GeoServerDesktop.GeoServerClient.Http;
using System.Net.Http;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for reloading and resetting GeoServer catalog and configuration
    /// </summary>
    public class ReloadService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the ReloadService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public ReloadService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Reloads the GeoServer catalog from the file system
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        /// <remarks>
        /// This operation reloads the catalog and all its resources from the data directory.
        /// It is useful after manual modifications to configuration files.
        /// </remarks>
        public async Task ReloadCatalogAsync()
        {
            var content = new StringContent(string.Empty);
            await _httpClient.PostAsync("/rest/reload", content);
        }

        /// <summary>
        /// Resets all store, raster, and schema caches
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        /// <remarks>
        /// This operation clears all caches but does not reload the catalog.
        /// It is useful when external data sources have changed.
        /// </remarks>
        public async Task ResetAsync()
        {
            var content = new StringContent(string.Empty);
            await _httpClient.PostAsync("/rest/reset", content);
        }
    }
}
