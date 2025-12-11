using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// 关于和系统状态的视图模型
    /// </summary>
    public partial class AboutViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>
        /// GeoServer 版本
        /// </summary>
        [ObservableProperty]
        private string _geoServerVersion = "Loading...";

        /// <summary>
        /// GeoTools 版本
        /// </summary>
        [ObservableProperty]
        private string _geoToolsVersion = "Loading...";

        /// <summary>
        /// GeoWebCache 版本
        /// </summary>
        [ObservableProperty]
        private string _geoWebCacheVersion = "Loading...";

        /// <summary>
        /// 状态消息
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = "Ready";

        /// <summary>
        /// 是否正在加载
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// 初始化 AboutViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public AboutViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// 加载系统信息
        /// </summary>
        [RelayCommand]
        private async Task LoadSystemInfoAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = "Not connected to GeoServer";
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading system information...";

            try
            {
                var aboutService = _connectionService.GetAboutService();

                // 加载版本信息
                var versionInfo = await aboutService.GetVersionAsync();
                if (versionInfo?.About?.Resources != null)
                {
                    foreach (var resource in versionInfo.About.Resources)
                    {
                        switch (resource.Name)
                        {
                            case "GeoServer":
                                GeoServerVersion = resource.Version ?? "N/A";
                                break;
                            case "GeoTools":
                                GeoToolsVersion = resource.Version ?? "N/A";
                                break;
                            case "GeoWebCache":
                                GeoWebCacheVersion = resource.Version ?? "N/A";
                                break;
                        }
                    }
                }

                StatusMessage = "System information loaded successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load system information: {ex.Message}";
                GeoServerVersion = "Error";
                GeoToolsVersion = "Error";
                GeoWebCacheVersion = "Error";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 初始化命令 - 加载系统信息
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadSystemInfoAsync();
        }
    }
}
