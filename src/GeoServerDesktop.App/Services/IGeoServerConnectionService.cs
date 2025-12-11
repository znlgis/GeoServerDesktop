using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.GeoServerClient.Services;
using System;

namespace GeoServerDesktop.App.Services
{
    /// <summary>
    /// 用于管理 GeoServer 连接的接口
    /// </summary>
    public interface IGeoServerConnectionService
    {
        /// <summary>
        /// 获取当前是否已建立连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 获取当前的连接选项
        /// </summary>
        GeoServerClientOptions? CurrentOptions { get; }

        /// <summary>
        /// 当连接状态发生变化时触发的事件
        /// </summary>
        event EventHandler<bool>? ConnectionStatusChanged;

        /// <summary>
        /// 连接到 GeoServer 实例
        /// </summary>
        /// <param name="options">连接选项</param>
        void Connect(GeoServerClientOptions options);

        /// <summary>
        /// 断开与当前 GeoServer 实例的连接
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 获取工作空间服务
        /// </summary>
        /// <returns>WorkspaceService 实例</returns>
        WorkspaceService GetWorkspaceService();

        /// <summary>
        /// 获取数据存储服务
        /// </summary>
        /// <returns>DataStoreService 实例</returns>
        DataStoreService GetDataStoreService();

        /// <summary>
        /// 获取图层服务
        /// </summary>
        /// <returns>LayerService 实例</returns>
        LayerService GetLayerService();

        /// <summary>
        /// 获取样式服务
        /// </summary>
        /// <returns>StyleService 实例</returns>
        StyleService GetStyleService();

        /// <summary>
        /// 获取图层组服务
        /// </summary>
        /// <returns>LayerGroupService 实例</returns>
        LayerGroupService GetLayerGroupService();

        /// <summary>
        /// 获取要素类型服务
        /// </summary>
        /// <returns>FeatureTypeService 实例</returns>
        FeatureTypeService GetFeatureTypeService();

        /// <summary>
        /// 获取预览服务
        /// </summary>
        /// <returns>PreviewService 实例</returns>
        PreviewService GetPreviewService();

        /// <summary>
        /// 获取关于服务
        /// </summary>
        /// <returns>AboutService 实例</returns>
        AboutService GetAboutService();

        /// <summary>
        /// 获取 GeoServer 全局设置服务
        /// </summary>
        /// <returns>GeoServer SettingsService 实例</returns>
        GeoServerClient.Services.SettingsService GetGlobalSettingsService();

        /// <summary>
        /// 获取日志服务
        /// </summary>
        /// <returns>LoggingService 实例</returns>
        LoggingService GetLoggingService();
    }
}
