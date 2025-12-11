using System;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Services;

namespace GeoServerDesktop.GeoServerClient.Configuration
{
    /// <summary>
    /// 用于创建 GeoServer 客户端服务实例的工厂类
    /// </summary>
    public class GeoServerClientFactory
    {
        private readonly GeoServerClientOptions _options;
        private IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 GeoServerClientFactory 类的新实例
        /// </summary>
        /// <param name="options">配置选项</param>
        public GeoServerClientFactory(GeoServerClientOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// 获取或创建 HTTP 客户端实例
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
        /// 创建新的 WorkspaceService 实例
        /// </summary>
        /// <returns>WorkspaceService 实例</returns>
        public WorkspaceService CreateWorkspaceService()
        {
            return new WorkspaceService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 DataStoreService 实例
        /// </summary>
        /// <returns>DataStoreService 实例</returns>
        public DataStoreService CreateDataStoreService()
        {
            return new DataStoreService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 LayerService 实例
        /// </summary>
        /// <returns>LayerService 实例</returns>
        public LayerService CreateLayerService()
        {
            return new LayerService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 StyleService 实例
        /// </summary>
        /// <returns>StyleService 实例</returns>
        public StyleService CreateStyleService()
        {
            return new StyleService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 LayerGroupService 实例
        /// </summary>
        /// <returns>LayerGroupService 实例</returns>
        public LayerGroupService CreateLayerGroupService()
        {
            return new LayerGroupService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 FeatureTypeService 实例
        /// </summary>
        /// <returns>FeatureTypeService 实例</returns>
        public FeatureTypeService CreateFeatureTypeService()
        {
            return new FeatureTypeService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 PreviewService 实例
        /// </summary>
        /// <returns>PreviewService 实例</returns>
        public PreviewService CreatePreviewService()
        {
            return new PreviewService(_options.BaseUrl);
        }

        /// <summary>
        /// 创建新的 NamespaceService 实例
        /// </summary>
        /// <returns>NamespaceService 实例</returns>
        public NamespaceService CreateNamespaceService()
        {
            return new NamespaceService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 CoverageStoreService 实例
        /// </summary>
        /// <returns>CoverageStoreService 实例</returns>
        public CoverageStoreService CreateCoverageStoreService()
        {
            return new CoverageStoreService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 CoverageService 实例
        /// </summary>
        /// <returns>CoverageService 实例</returns>
        public CoverageService CreateCoverageService()
        {
            return new CoverageService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 AboutService 实例
        /// </summary>
        /// <returns>AboutService 实例</returns>
        public AboutService CreateAboutService()
        {
            return new AboutService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 SettingsService 实例
        /// </summary>
        /// <returns>SettingsService 实例</returns>
        public SettingsService CreateSettingsService()
        {
            return new SettingsService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 ReloadService 实例
        /// </summary>
        /// <returns>ReloadService 实例</returns>
        public ReloadService CreateReloadService()
        {
            return new ReloadService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 WMSStoreService 实例
        /// </summary>
        /// <returns>WMSStoreService 实例</returns>
        public WMSStoreService CreateWMSStoreService()
        {
            return new WMSStoreService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 WMSLayerService 实例
        /// </summary>
        /// <returns>WMSLayerService 实例</returns>
        public WMSLayerService CreateWMSLayerService()
        {
            return new WMSLayerService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 WMTSStoreService 实例
        /// </summary>
        /// <returns>WMTSStoreService 实例</returns>
        public WMTSStoreService CreateWMTSStoreService()
        {
            return new WMTSStoreService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 WMTSLayerService 实例
        /// </summary>
        /// <returns>WMTSLayerService 实例</returns>
        public WMTSLayerService CreateWMTSLayerService()
        {
            return new WMTSLayerService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 LoggingService 实例
        /// </summary>
        /// <returns>LoggingService 实例</returns>
        public LoggingService CreateLoggingService()
        {
            return new LoggingService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 ResourceService 实例
        /// </summary>
        /// <returns>ResourceService 实例</returns>
        public ResourceService CreateResourceService()
        {
            return new ResourceService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 WMSSettingsService 实例
        /// </summary>
        /// <returns>WMSSettingsService 实例</returns>
        public WMSSettingsService CreateWMSSettingsService()
        {
            return new WMSSettingsService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 WFSSettingsService 实例
        /// </summary>
        /// <returns>WFSSettingsService 实例</returns>
        public WFSSettingsService CreateWFSSettingsService()
        {
            return new WFSSettingsService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 WCSSettingsService 实例
        /// </summary>
        /// <returns>WCSSettingsService 实例</returns>
        public WCSSettingsService CreateWCSSettingsService()
        {
            return new WCSSettingsService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 WMTSSettingsService 实例
        /// </summary>
        /// <returns>WMTSSettingsService 实例</returns>
        public WMTSSettingsService CreateWMTSSettingsService()
        {
            return new WMTSSettingsService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 SecurityService 实例
        /// </summary>
        /// <returns>SecurityService 实例</returns>
        public SecurityService CreateSecurityService()
        {
            return new SecurityService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 UserGroupService 实例
        /// </summary>
        /// <returns>UserGroupService 实例</returns>
        public UserGroupService CreateUserGroupService()
        {
            return new UserGroupService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 RoleService 实例
        /// </summary>
        /// <returns>RoleService 实例</returns>
        public RoleService CreateRoleService()
        {
            return new RoleService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 FontService 实例
        /// </summary>
        /// <returns>FontService 实例</returns>
        public FontService CreateFontService()
        {
            return new FontService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 TemplateService 实例
        /// </summary>
        /// <returns>TemplateService 实例</returns>
        public TemplateService CreateTemplateService()
        {
            return new TemplateService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 GWCLayerService 实例
        /// </summary>
        /// <returns>GWCLayerService 实例</returns>
        public GWCLayerService CreateGWCLayerService()
        {
            return new GWCLayerService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 DiskQuotaService 实例
        /// </summary>
        /// <returns>DiskQuotaService 实例</returns>
        public DiskQuotaService CreateDiskQuotaService()
        {
            return new DiskQuotaService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 GridsetService 实例
        /// </summary>
        /// <returns>GridsetService 实例</returns>
        public GridsetService CreateGridsetService()
        {
            return new GridsetService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 ImporterService 实例
        /// </summary>
        /// <returns>ImporterService 实例</returns>
        public ImporterService CreateImporterService()
        {
            return new ImporterService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 MonitoringService 实例
        /// </summary>
        /// <returns>MonitoringService 实例</returns>
        public MonitoringService CreateMonitoringService()
        {
            return new MonitoringService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 TransformService 实例
        /// </summary>
        /// <returns>TransformService 实例</returns>
        public TransformService CreateTransformService()
        {
            return new TransformService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 URLCheckService 实例
        /// </summary>
        /// <returns>URLCheckService 实例</returns>
        public URLCheckService CreateURLCheckService()
        {
            return new URLCheckService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 AuthenticationFilterService 实例
        /// </summary>
        /// <returns>AuthenticationFilterService 实例</returns>
        public AuthenticationFilterService CreateAuthenticationFilterService()
        {
            return new AuthenticationFilterService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 AuthenticationProviderService 实例
        /// </summary>
        /// <returns>AuthenticationProviderService 实例</returns>
        public AuthenticationProviderService CreateAuthenticationProviderService()
        {
            return new AuthenticationProviderService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 FilterChainService 实例
        /// </summary>
        /// <returns>FilterChainService 实例</returns>
        public FilterChainService CreateFilterChainService()
        {
            return new FilterChainService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 PasswordService 实例
        /// </summary>
        /// <returns>PasswordService 实例</returns>
        public PasswordService CreatePasswordService()
        {
            return new PasswordService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 KeystoreService 实例
        /// </summary>
        /// <returns>KeystoreService 实例</returns>
        public KeystoreService CreateKeystoreService()
        {
            return new KeystoreService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 BlobstoreService 实例
        /// </summary>
        /// <returns>BlobstoreService 实例</returns>
        public BlobstoreService CreateBlobstoreService()
        {
            return new BlobstoreService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 StructuredCoverageService 实例
        /// </summary>
        /// <returns>StructuredCoverageService 实例</returns>
        public StructuredCoverageService CreateStructuredCoverageService()
        {
            return new StructuredCoverageService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 CoverageViewService 实例
        /// </summary>
        /// <returns>CoverageViewService 实例</returns>
        public CoverageViewService CreateCoverageViewService()
        {
            return new CoverageViewService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 WPSSettingsService 实例
        /// </summary>
        /// <returns>WPSSettingsService 实例</returns>
        public WPSSettingsService CreateWPSSettingsService()
        {
            return new WPSSettingsService(GetHttpClient());
        }

        /// <summary>
        /// 创建新的 CSWSettingsService 实例
        /// </summary>
        /// <returns>CSWSettingsService 实例</returns>
        public CSWSettingsService CreateCSWSettingsService()
        {
            return new CSWSettingsService(GetHttpClient());
        }

        /// <summary>
        /// 释放工厂及其资源
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
            _httpClient = null;
        }
    }
}
