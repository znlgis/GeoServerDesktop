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
        /// Creates a new FeatureTypeService instance
        /// </summary>
        /// <returns>FeatureTypeService instance</returns>
        public FeatureTypeService CreateFeatureTypeService()
        {
            return new FeatureTypeService(GetHttpClient());
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
        /// Creates a new NamespaceService instance
        /// </summary>
        /// <returns>NamespaceService instance</returns>
        public NamespaceService CreateNamespaceService()
        {
            return new NamespaceService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new CoverageStoreService instance
        /// </summary>
        /// <returns>CoverageStoreService instance</returns>
        public CoverageStoreService CreateCoverageStoreService()
        {
            return new CoverageStoreService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new CoverageService instance
        /// </summary>
        /// <returns>CoverageService instance</returns>
        public CoverageService CreateCoverageService()
        {
            return new CoverageService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new AboutService instance
        /// </summary>
        /// <returns>AboutService instance</returns>
        public AboutService CreateAboutService()
        {
            return new AboutService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new SettingsService instance
        /// </summary>
        /// <returns>SettingsService instance</returns>
        public SettingsService CreateSettingsService()
        {
            return new SettingsService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new ReloadService instance
        /// </summary>
        /// <returns>ReloadService instance</returns>
        public ReloadService CreateReloadService()
        {
            return new ReloadService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new WMSStoreService instance
        /// </summary>
        /// <returns>WMSStoreService instance</returns>
        public WMSStoreService CreateWMSStoreService()
        {
            return new WMSStoreService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new WMSLayerService instance
        /// </summary>
        /// <returns>WMSLayerService instance</returns>
        public WMSLayerService CreateWMSLayerService()
        {
            return new WMSLayerService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new WMTSStoreService instance
        /// </summary>
        /// <returns>WMTSStoreService instance</returns>
        public WMTSStoreService CreateWMTSStoreService()
        {
            return new WMTSStoreService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new WMTSLayerService instance
        /// </summary>
        /// <returns>WMTSLayerService instance</returns>
        public WMTSLayerService CreateWMTSLayerService()
        {
            return new WMTSLayerService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new LoggingService instance
        /// </summary>
        /// <returns>LoggingService instance</returns>
        public LoggingService CreateLoggingService()
        {
            return new LoggingService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new ResourceService instance
        /// </summary>
        /// <returns>ResourceService instance</returns>
        public ResourceService CreateResourceService()
        {
            return new ResourceService(GetHttpClient());
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
