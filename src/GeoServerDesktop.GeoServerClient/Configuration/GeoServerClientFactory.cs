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
        /// Creates a new WMSSettingsService instance
        /// </summary>
        /// <returns>WMSSettingsService instance</returns>
        public WMSSettingsService CreateWMSSettingsService()
        {
            return new WMSSettingsService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new WFSSettingsService instance
        /// </summary>
        /// <returns>WFSSettingsService instance</returns>
        public WFSSettingsService CreateWFSSettingsService()
        {
            return new WFSSettingsService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new WCSSettingsService instance
        /// </summary>
        /// <returns>WCSSettingsService instance</returns>
        public WCSSettingsService CreateWCSSettingsService()
        {
            return new WCSSettingsService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new WMTSSettingsService instance
        /// </summary>
        /// <returns>WMTSSettingsService instance</returns>
        public WMTSSettingsService CreateWMTSSettingsService()
        {
            return new WMTSSettingsService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new SecurityService instance
        /// </summary>
        /// <returns>SecurityService instance</returns>
        public SecurityService CreateSecurityService()
        {
            return new SecurityService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new UserGroupService instance
        /// </summary>
        /// <returns>UserGroupService instance</returns>
        public UserGroupService CreateUserGroupService()
        {
            return new UserGroupService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new RoleService instance
        /// </summary>
        /// <returns>RoleService instance</returns>
        public RoleService CreateRoleService()
        {
            return new RoleService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new FontService instance
        /// </summary>
        /// <returns>FontService instance</returns>
        public FontService CreateFontService()
        {
            return new FontService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new TemplateService instance
        /// </summary>
        /// <returns>TemplateService instance</returns>
        public TemplateService CreateTemplateService()
        {
            return new TemplateService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new GWCLayerService instance
        /// </summary>
        /// <returns>GWCLayerService instance</returns>
        public GWCLayerService CreateGWCLayerService()
        {
            return new GWCLayerService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new DiskQuotaService instance
        /// </summary>
        /// <returns>DiskQuotaService instance</returns>
        public DiskQuotaService CreateDiskQuotaService()
        {
            return new DiskQuotaService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new GridsetService instance
        /// </summary>
        /// <returns>GridsetService instance</returns>
        public GridsetService CreateGridsetService()
        {
            return new GridsetService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new ImporterService instance
        /// </summary>
        /// <returns>ImporterService instance</returns>
        public ImporterService CreateImporterService()
        {
            return new ImporterService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new MonitoringService instance
        /// </summary>
        /// <returns>MonitoringService instance</returns>
        public MonitoringService CreateMonitoringService()
        {
            return new MonitoringService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new TransformService instance
        /// </summary>
        /// <returns>TransformService instance</returns>
        public TransformService CreateTransformService()
        {
            return new TransformService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new URLCheckService instance
        /// </summary>
        /// <returns>URLCheckService instance</returns>
        public URLCheckService CreateURLCheckService()
        {
            return new URLCheckService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new AuthenticationFilterService instance
        /// </summary>
        /// <returns>AuthenticationFilterService instance</returns>
        public AuthenticationFilterService CreateAuthenticationFilterService()
        {
            return new AuthenticationFilterService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new AuthenticationProviderService instance
        /// </summary>
        /// <returns>AuthenticationProviderService instance</returns>
        public AuthenticationProviderService CreateAuthenticationProviderService()
        {
            return new AuthenticationProviderService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new FilterChainService instance
        /// </summary>
        /// <returns>FilterChainService instance</returns>
        public FilterChainService CreateFilterChainService()
        {
            return new FilterChainService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new PasswordService instance
        /// </summary>
        /// <returns>PasswordService instance</returns>
        public PasswordService CreatePasswordService()
        {
            return new PasswordService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new KeystoreService instance
        /// </summary>
        /// <returns>KeystoreService instance</returns>
        public KeystoreService CreateKeystoreService()
        {
            return new KeystoreService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new BlobstoreService instance
        /// </summary>
        /// <returns>BlobstoreService instance</returns>
        public BlobstoreService CreateBlobstoreService()
        {
            return new BlobstoreService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new StructuredCoverageService instance
        /// </summary>
        /// <returns>StructuredCoverageService instance</returns>
        public StructuredCoverageService CreateStructuredCoverageService()
        {
            return new StructuredCoverageService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new CoverageViewService instance
        /// </summary>
        /// <returns>CoverageViewService instance</returns>
        public CoverageViewService CreateCoverageViewService()
        {
            return new CoverageViewService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new WPSSettingsService instance
        /// </summary>
        /// <returns>WPSSettingsService instance</returns>
        public WPSSettingsService CreateWPSSettingsService()
        {
            return new WPSSettingsService(GetHttpClient());
        }

        /// <summary>
        /// Creates a new CSWSettingsService instance
        /// </summary>
        /// <returns>CSWSettingsService instance</returns>
        public CSWSettingsService CreateCSWSettingsService()
        {
            return new CSWSettingsService(GetHttpClient());
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
