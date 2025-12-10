using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Services;
using System;

namespace GeoServerDesktop.GeoServerClient.Configuration
{
    /// <summary>
    /// Factory for creating GeoServer client service instances
    /// </summary>
    public class GeoServerClientFactory
    {
        private readonly GeoServerClientOptions _options;
        private IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the GeoServerClientFactory class
        /// </summary>
        /// <param name="options">Configuration options</param>
        public GeoServerClientFactory(GeoServerClientOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Gets or creates the HTTP client instance
        /// </summary>
        private IGeoServerHttpClient GetHttpClient()
        {
            if (_httpClient == null)
            {
                _httpClient = new GeoServerHttpClient(_options);
            }
            return _httpClient;
        }

        /// <summary>
        /// Creates a new WorkspaceService instance
        /// </summary>
        /// <returns>WorkspaceService instance</returns>
        public WorkspaceService CreateWorkspaceService()
        {
            return new WorkspaceService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new DataStoreService instance
        /// </summary>
        /// <returns>DataStoreService instance</returns>
        public DataStoreService CreateDataStoreService()
        {
            return new DataStoreService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new LayerService instance
        /// </summary>
        /// <returns>LayerService instance</returns>
        public LayerService CreateLayerService()
        {
            return new LayerService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new StyleService instance
        /// </summary>
        /// <returns>StyleService instance</returns>
        public StyleService CreateStyleService()
        {
            return new StyleService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new LayerGroupService instance
        /// </summary>
        /// <returns>LayerGroupService instance</returns>
        public LayerGroupService CreateLayerGroupService()
        {
            return new LayerGroupService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new PreviewService instance
        /// </summary>
        /// <returns>PreviewService instance</returns>
        public PreviewService CreatePreviewService()
        {
            return new PreviewService(_options.BaseUrl);
        }

        /// <summary>
        /// Disposes the factory and its resources
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
            _httpClient = null;
        }
    }
}
