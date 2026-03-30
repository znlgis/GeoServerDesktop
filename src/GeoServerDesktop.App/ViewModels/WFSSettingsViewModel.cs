using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// WFS 服务设置的视图模型
    /// </summary>
    public partial class WFSSettingsViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        [ObservableProperty] private bool _isEnabled;
        [ObservableProperty] private string _serviceName = string.Empty;
        [ObservableProperty] private string _title = string.Empty;
        [ObservableProperty] private string _abstract = string.Empty;
        [ObservableProperty] private string _maintainer = string.Empty;
        [ObservableProperty] private string _onlineResource = string.Empty;
        [ObservableProperty] private bool _citeCompliant;
        [ObservableProperty] private int _maxFeatures;
        [ObservableProperty] private string _serviceLevel = string.Empty;
        [ObservableProperty] private bool _featureBounding;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _statusMessage = string.Empty;

        /// <summary>
        /// 初始化 WFSSettingsViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public WFSSettingsViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// 加载 WFS 设置
        /// </summary>
        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            if (!_connectionService.IsConnected) { StatusMessage = L.StatusNotConnected; return; }
            IsLoading = true;
            StatusMessage = L.StatusLoadingWfsSettings;
            try
            {
                var service = _connectionService.GetWFSSettingsService();
                var settings = await service.GetWFSSettingsAsync();
                if (settings?.WFS != null)
                {
                    IsEnabled = settings.WFS.Enabled ?? false;
                    ServiceName = settings.WFS.Name ?? string.Empty;
                    Title = settings.WFS.Title ?? string.Empty;
                    Abstract = settings.WFS.Abstract ?? string.Empty;
                    Maintainer = settings.WFS.Maintainer ?? string.Empty;
                    OnlineResource = settings.WFS.OnlineResource ?? string.Empty;
                    CiteCompliant = settings.WFS.CiteCompliant ?? false;
                    MaxFeatures = settings.WFS.MaxFeatures ?? 0;
                    ServiceLevel = settings.WFS.ServiceLevel ?? string.Empty;
                    FeatureBounding = settings.WFS.FeatureBounding ?? false;
                }
                StatusMessage = L.StatusWfsSettingsLoaded;
            }
            catch (Exception ex) { StatusMessage = string.Format(L.StatusWfsSettingsLoadFailed, ex.Message); }
            finally { IsLoading = false; }
        }

        /// <summary>
        /// 保存 WFS 设置
        /// </summary>
        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            if (!_connectionService.IsConnected) { StatusMessage = L.StatusNotConnected; return; }
            IsLoading = true;
            StatusMessage = L.StatusSavingWfsSettings;
            try
            {
                var service = _connectionService.GetWFSSettingsService();
                var settings = new WFSSettings
                {
                    WFS = new WFSServiceConfig
                    {
                        Enabled = IsEnabled,
                        Name = ServiceName,
                        Title = Title,
                        Abstract = Abstract,
                        Maintainer = Maintainer,
                        OnlineResource = OnlineResource,
                        CiteCompliant = CiteCompliant,
                        MaxFeatures = MaxFeatures,
                        ServiceLevel = ServiceLevel,
                        FeatureBounding = FeatureBounding
                    }
                };
                await service.UpdateWFSSettingsAsync(settings);
                StatusMessage = L.StatusWfsSettingsSaved;
            }
            catch (Exception ex) { StatusMessage = string.Format(L.StatusWfsSettingsSaveFailed, ex.Message); }
            finally { IsLoading = false; }
        }
    }
}
