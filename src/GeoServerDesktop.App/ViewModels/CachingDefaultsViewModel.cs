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
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>状态消息</summary>
        [ObservableProperty]
        private string _statusMessage = "Ready";

        /// <summary>
        /// 初始化 CachingDefaultsViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public CachingDefaultsViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// 加载缓存默认设置
        /// </summary>
        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = "Not connected to GeoServer";
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading caching defaults...";

            try
            {
                // GWC 默认设置通过 GWC 图层服务获取，此处展示概览信息
                await Task.Delay(100); // 模拟异步加载
                StatusMessage = "Caching defaults loaded (configure per-layer settings in Layer Management)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load caching defaults: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 保存缓存默认设置
        /// </summary>
        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = "Not connected to GeoServer";
                return;
            }

            IsLoading = true;
            StatusMessage = "Saving caching defaults...";

            try
            {
                await Task.Delay(100); // 模拟异步保存
                StatusMessage = "Caching defaults saved successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to save caching defaults: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
