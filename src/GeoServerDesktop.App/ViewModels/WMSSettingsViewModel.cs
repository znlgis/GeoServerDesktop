using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// WMS 服务设置的视图模型
    /// </summary>
    public partial class WMSSettingsViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>是否启用 WMS 服务</summary>
        [ObservableProperty]
        private bool _isEnabled;

        /// <summary>服务名称</summary>
        [ObservableProperty]
        private string _serviceName = string.Empty;

        /// <summary>服务标题</summary>
        [ObservableProperty]
        private string _title = string.Empty;

        /// <summary>服务摘要</summary>
        [ObservableProperty]
        private string _abstract = string.Empty;

        /// <summary>维护者</summary>
        [ObservableProperty]
        private string _maintainer = string.Empty;

        /// <summary>在线资源 URL</summary>
        [ObservableProperty]
        private string _onlineResource = string.Empty;

        /// <summary>是否符合引用规范</summary>
        [ObservableProperty]
        private bool _citeCompliant;

        /// <summary>是否正在加载</summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>状态消息</summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>
        /// 初始化 WMSSettingsViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public WMSSettingsViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// 加载 WMS 设置
        /// </summary>
        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = L.StatusNotConnected;
                return;
            }

            IsLoading = true;
            StatusMessage = L.StatusLoadingWmsSettings;

            try
            {
                var service = _connectionService.GetWMSSettingsService();
                var settings = await service.GetWMSSettingsAsync();

                if (settings?.WMS != null)
                {
                    IsEnabled = settings.WMS.Enabled ?? false;
                    ServiceName = settings.WMS.Name ?? string.Empty;
                    Title = settings.WMS.Title ?? string.Empty;
                    Abstract = settings.WMS.Abstract ?? string.Empty;
                    Maintainer = settings.WMS.Maintainer ?? string.Empty;
                    OnlineResource = settings.WMS.OnlineResource ?? string.Empty;
                    CiteCompliant = settings.WMS.CiteCompliant ?? false;
                }

                StatusMessage = L.StatusWmsSettingsLoaded;
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusWmsSettingsLoadFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 保存 WMS 设置
        /// </summary>
        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = L.StatusNotConnected;
                return;
            }

            IsLoading = true;
            StatusMessage = L.StatusSavingWmsSettings;

            try
            {
                var service = _connectionService.GetWMSSettingsService();
                var settings = new WMSSettings
                {
                    WMS = new WMSServiceConfig
                    {
                        Enabled = IsEnabled,
                        Name = ServiceName,
                        Title = Title,
                        Abstract = Abstract,
                        Maintainer = Maintainer,
                        OnlineResource = OnlineResource,
                        CiteCompliant = CiteCompliant
                    }
                };

                await service.UpdateWMSSettingsAsync(settings);
                StatusMessage = L.StatusWmsSettingsSaved;
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusWmsSettingsSaveFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
