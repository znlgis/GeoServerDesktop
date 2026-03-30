using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// WCS 服务设置的视图模型
    /// </summary>
    public partial class WCSSettingsViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        [ObservableProperty] private bool _isEnabled;
        [ObservableProperty] private string _serviceName = string.Empty;
        [ObservableProperty] private string _title = string.Empty;
        [ObservableProperty] private string _abstract = string.Empty;
        [ObservableProperty] private string _maintainer = string.Empty;
        [ObservableProperty] private string _onlineResource = string.Empty;
        [ObservableProperty] private bool _citeCompliant;
        [ObservableProperty] private int _maxInputMemory;
        [ObservableProperty] private int _maxOutputMemory;
        [ObservableProperty] private bool _subsamplingEnabled;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _statusMessage = "Ready";

        /// <summary>
        /// 初始化 WCSSettingsViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public WCSSettingsViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// 加载 WCS 设置
        /// </summary>
        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            if (!_connectionService.IsConnected) { StatusMessage = "Not connected to GeoServer"; return; }
            IsLoading = true;
            StatusMessage = "Loading WCS settings...";
            try
            {
                var service = _connectionService.GetWCSSettingsService();
                var settings = await service.GetWCSSettingsAsync();
                if (settings?.WCS != null)
                {
                    IsEnabled = settings.WCS.Enabled ?? false;
                    ServiceName = settings.WCS.Name ?? string.Empty;
                    Title = settings.WCS.Title ?? string.Empty;
                    Abstract = settings.WCS.Abstract ?? string.Empty;
                    Maintainer = settings.WCS.Maintainer ?? string.Empty;
                    OnlineResource = settings.WCS.OnlineResource ?? string.Empty;
                    CiteCompliant = settings.WCS.CiteCompliant ?? false;
                    MaxInputMemory = settings.WCS.MaxInputMemory ?? 0;
                    MaxOutputMemory = settings.WCS.MaxOutputMemory ?? 0;
                    SubsamplingEnabled = settings.WCS.SubsamplingEnabled ?? false;
                }
                StatusMessage = "WCS settings loaded";
            }
            catch (Exception ex) { StatusMessage = $"Failed to load WCS settings: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        /// <summary>
        /// 保存 WCS 设置
        /// </summary>
        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            if (!_connectionService.IsConnected) { StatusMessage = "Not connected to GeoServer"; return; }
            IsLoading = true;
            StatusMessage = "Saving WCS settings...";
            try
            {
                var service = _connectionService.GetWCSSettingsService();
                var settings = new WCSSettings
                {
                    WCS = new WCSServiceConfig
                    {
                        Enabled = IsEnabled,
                        Name = ServiceName,
                        Title = Title,
                        Abstract = Abstract,
                        Maintainer = Maintainer,
                        OnlineResource = OnlineResource,
                        CiteCompliant = CiteCompliant,
                        MaxInputMemory = MaxInputMemory,
                        MaxOutputMemory = MaxOutputMemory,
                        SubsamplingEnabled = SubsamplingEnabled
                    }
                };
                await service.UpdateWCSSettingsAsync(settings);
                StatusMessage = "WCS settings saved successfully";
            }
            catch (Exception ex) { StatusMessage = $"Failed to save WCS settings: {ex.Message}"; }
            finally { IsLoading = false; }
        }
    }
}
