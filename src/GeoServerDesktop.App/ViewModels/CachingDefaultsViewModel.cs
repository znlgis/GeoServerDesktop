using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// GeoWebCache 缓存默认配置的视图模型
    /// </summary>
    public partial class CachingDefaultsViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>是否启用缓存</summary>
        [ObservableProperty]
        private bool _isEnabled = true;

        /// <summary>默认缓存过期时间（秒，0 表示不过期）</summary>
        [ObservableProperty]
        private int _defaultExpireCache;

        /// <summary>默认客户端缓存过期时间（秒，0 表示不过期）</summary>
        [ObservableProperty]
        private int _defaultExpireClients;

        /// <summary>是否正在加载</summary>

        /// <summary>状态消息</summary>

        /// <summary>
        /// 初始化 CachingDefaultsViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public CachingDefaultsViewModel(IGeoServerConnectionService connectionService)
        : base(connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// 加载缓存默认设置
        /// </summary>
        /// <remarks>
        /// FIXED-E37（显式化，不再假装成功）：实测目标 GeoServer 3.0.1 上
        /// GET /gwc/rest/settings 返回 404（GWC 默认设置无 REST 端点），
        /// 且 GET /rest/settings.json 的 global 体中也无任何 gwc 相关字段可借用，
        /// 库层 GWCLayerService 亦无 defaults 方法——因此本页无法真实读写，
        /// 明确提示不可配置，而非此前 Task.Delay(100) 的“加载成功”假象。
        /// </remarks>
        [RelayCommand]
        private Task LoadSettingsAsync()
        {
            if (!HasConnection(L.StatusNotConnected)) return Task.CompletedTask;

            StatusMessage = L.StatusGwcNotConfigurable;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 保存缓存默认设置（同 Load：GWC defaults 在 3.0.1 无 REST 端点，不可保存，见 LoadSettingsAsync 注释）
        /// </summary>
        [RelayCommand]
        private Task SaveSettingsAsync()
        {
            if (!HasConnection(L.StatusNotConnected)) return Task.CompletedTask;

            StatusMessage = L.StatusGwcNotConfigurable;
            return Task.CompletedTask;
        }
    }
}
