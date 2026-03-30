using System;
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.GeoServerClient.Services;

namespace GeoServerDesktop.App.Services
{
    /// <summary>
    /// 用于管理 GeoServer 连接的服务
    /// </summary>
    public class GeoServerConnectionService : IGeoServerConnectionService
    {
        private GeoServerClientFactory? _factory;

        /// <summary>
        /// 获取当前是否已建立连接
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// 获取当前的连接选项
        /// </summary>
        public GeoServerClientOptions? CurrentOptions { get; private set; }

        /// <summary>
        /// 当连接状态发生变化时触发的事件
        /// </summary>
        public event EventHandler<bool>? ConnectionStatusChanged;

        /// <summary>
        /// 连接到 GeoServer 实例
        /// </summary>
        /// <param name="options">连接选项</param>
        public void Connect(GeoServerClientOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            Disconnect();

            CurrentOptions = options;
            _factory = new GeoServerClientFactory(options);
            IsConnected = true;

            ConnectionStatusChanged?.Invoke(this, true);
        }

        /// <summary>
        /// 断开与当前 GeoServer 实例的连接
        /// </summary>
        public void Disconnect()
        {
            if (_factory != null)
            {
                _factory.Dispose();
                _factory = null;
            }

            IsConnected = false;
            CurrentOptions = null;

            ConnectionStatusChanged?.Invoke(this, false);
        }

        /// <summary>
        /// 获取工作空间服务
        /// </summary>
        /// <returns>WorkspaceService 实例</returns>
        public WorkspaceService GetWorkspaceService()
        {
            EnsureConnected();
            return _factory!.CreateWorkspaceService();
        }

        /// <summary>
        /// 获取数据存储服务
        /// </summary>
        /// <returns>DataStoreService 实例</returns>
        public DataStoreService GetDataStoreService()
        {
            EnsureConnected();
            return _factory!.CreateDataStoreService();
        }

        /// <summary>
        /// 获取图层服务
        /// </summary>
        /// <returns>LayerService 实例</returns>
        public LayerService GetLayerService()
        {
            EnsureConnected();
            return _factory!.CreateLayerService();
        }

        /// <summary>
        /// 获取样式服务
        /// </summary>
        /// <returns>StyleService 实例</returns>
        public StyleService GetStyleService()
        {
            EnsureConnected();
            return _factory!.CreateStyleService();
        }

        /// <summary>
        /// 获取图层组服务
        /// </summary>
        /// <returns>LayerGroupService 实例</returns>
        public LayerGroupService GetLayerGroupService()
        {
            EnsureConnected();
            return _factory!.CreateLayerGroupService();
        }

        /// <summary>
        /// 获取要素类型服务
        /// </summary>
        /// <returns>FeatureTypeService 实例</returns>
        public FeatureTypeService GetFeatureTypeService()
        {
            EnsureConnected();
            return _factory!.CreateFeatureTypeService();
        }

        /// <summary>
        /// 获取预览服务
        /// </summary>
        /// <returns>PreviewService 实例</returns>
        public PreviewService GetPreviewService()
        {
            EnsureConnected();
            return _factory!.CreatePreviewService();
        }

        /// <summary>
        /// 获取关于服务
        /// </summary>
        /// <returns>AboutService 实例</returns>
        public AboutService GetAboutService()
        {
            EnsureConnected();
            return _factory!.CreateAboutService();
        }

        /// <summary>
        /// 获取 GeoServer 全局设置服务
        /// </summary>
        /// <returns>GeoServer SettingsService 实例</returns>
        public GeoServerClient.Services.SettingsService GetGlobalSettingsService()
        {
            EnsureConnected();
            return _factory!.CreateSettingsService();
        }

        /// <summary>
        /// 获取日志服务
        /// </summary>
        /// <returns>LoggingService 实例</returns>
        public LoggingService GetLoggingService()
        {
            EnsureConnected();
            return _factory!.CreateLoggingService();
        }

        /// <summary>
        /// 获取 WMS 服务设置服务
        /// </summary>
        /// <returns>WMSSettingsService 实例</returns>
        public WMSSettingsService GetWMSSettingsService()
        {
            EnsureConnected();
            return _factory!.CreateWMSSettingsService();
        }

        /// <summary>
        /// 获取 WFS 服务设置服务
        /// </summary>
        /// <returns>WFSSettingsService 实例</returns>
        public WFSSettingsService GetWFSSettingsService()
        {
            EnsureConnected();
            return _factory!.CreateWFSSettingsService();
        }

        /// <summary>
        /// 获取 WCS 服务设置服务
        /// </summary>
        /// <returns>WCSSettingsService 实例</returns>
        public WCSSettingsService GetWCSSettingsService()
        {
            EnsureConnected();
            return _factory!.CreateWCSSettingsService();
        }

        /// <summary>
        /// 获取磁盘配额服务
        /// </summary>
        /// <returns>DiskQuotaService 实例</returns>
        public DiskQuotaService GetDiskQuotaService()
        {
            EnsureConnected();
            return _factory!.CreateDiskQuotaService();
        }

        /// <summary>
        /// 获取格网集服务
        /// </summary>
        /// <returns>GridsetService 实例</returns>
        public GridsetService GetGridsetService()
        {
            EnsureConnected();
            return _factory!.CreateGridsetService();
        }

        /// <summary>
        /// 获取安全访问控制服务
        /// </summary>
        /// <returns>SecurityService 实例</returns>
        public SecurityService GetSecurityService()
        {
            EnsureConnected();
            return _factory!.CreateSecurityService();
        }

        /// <summary>
        /// 获取用户组服务
        /// </summary>
        /// <returns>UserGroupService 实例</returns>
        public UserGroupService GetUserGroupService()
        {
            EnsureConnected();
            return _factory!.CreateUserGroupService();
        }

        /// <summary>
        /// 获取角色服务
        /// </summary>
        /// <returns>RoleService 实例</returns>
        public RoleService GetRoleService()
        {
            EnsureConnected();
            return _factory!.CreateRoleService();
        }

        /// <summary>
        /// 确保已连接到 GeoServer 实例
        /// </summary>
        private void EnsureConnected()
        {
            if (!IsConnected || _factory == null)
                throw new InvalidOperationException("Not connected to a GeoServer instance");
        }
    }
}
