using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// GeoServer 全局设置的视图模型
    /// </summary>
    public partial class GlobalSettingsViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>联系人姓名</summary>
        [ObservableProperty]
        private string _contactPerson = string.Empty;

        /// <summary>联系人组织</summary>
        [ObservableProperty]
        private string _contactOrganization = string.Empty;

        /// <summary>联系人邮箱</summary>
        [ObservableProperty]
        private string _contactEmail = string.Empty;

        /// <summary>在线资源 URL</summary>
        [ObservableProperty]
        private string _onlineResource = string.Empty;

        /// <summary>代理基础 URL</summary>
        [ObservableProperty]
        private string _proxyBaseUrl = string.Empty;

        /// <summary>是否启用详细异常信息</summary>
        [ObservableProperty]
        private bool _verboseExceptions;

        /// <summary>是否正在加载</summary>

        /// <summary>状态消息</summary>

        /// <summary>
        /// 初始化 GlobalSettingsViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public GlobalSettingsViewModel(IGeoServerConnectionService connectionService)
        : base(connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// 加载全局设置
        /// </summary>
        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            if (!HasConnection(L.StatusNotConnected)) return;

            IsLoading = true;
            StatusMessage = L.StatusLoadingGlobalSettings;

            try
            {
                var service = _connectionService.GetGlobalSettingsService();
                var globalSettings = await service.GetGlobalSettingsAsync();

                if (globalSettings?.Settings != null)
                {
                    var settings = globalSettings.Settings;
                    OnlineResource = settings.OnlineResource ?? string.Empty;
                    ProxyBaseUrl = settings.ProxyBaseUrl ?? string.Empty;
                    VerboseExceptions = settings.VerboseExceptions ?? false;

                    if (settings.Contact != null)
                    {
                        ContactPerson = settings.Contact.ContactPerson ?? string.Empty;
                        ContactOrganization = settings.Contact.ContactOrganization ?? string.Empty;
                        ContactEmail = settings.Contact.ContactEmail ?? string.Empty;
                    }
                }

                StatusMessage = L.StatusGlobalSettingsLoaded;
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusGlobalSettingsLoadFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 保存全局设置
        /// </summary>
        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            if (!HasConnection(L.StatusNotConnected)) return;

            IsLoading = true;
            StatusMessage = L.StatusSavingGlobalSettings;

            try
            {
                var service = _connectionService.GetGlobalSettingsService();

                // FIXED：PUT /rest/settings 为整包替换语义（实测缺失键会被重置为服务器默认值），
                // 原“新建对象整包 PUT”会丢 contact 其余字段与 ExtensionData 外的服务器键。
                // 改为 GET 先读 → 仅改本页负责的字段 → 原对象回写 PUT；
                // 模型外服务器键由 GlobalSettingsInfo/Settings/ContactInfo 的 [JsonExtensionData] 保障往返。
                var globalSettings = await service.GetGlobalSettingsAsync() ?? new GlobalSettings();
                if (globalSettings.Settings == null)
                {
                    globalSettings.Settings = new Settings();
                }
                var settings = globalSettings.Settings;

                settings.OnlineResource = OnlineResource;
                settings.ProxyBaseUrl = ProxyBaseUrl;
                settings.VerboseExceptions = VerboseExceptions;

                if (settings.Contact == null)
                {
                    settings.Contact = new ContactInfo();
                }
                settings.Contact.ContactPerson = ContactPerson;
                settings.Contact.ContactOrganization = ContactOrganization;
                settings.Contact.ContactEmail = ContactEmail;

                await service.UpdateGlobalSettingsAsync(globalSettings);
                StatusMessage = L.StatusGlobalSettingsSaved;
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusGlobalSettingsSaveFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
