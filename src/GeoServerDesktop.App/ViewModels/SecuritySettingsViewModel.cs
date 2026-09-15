using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;

namespace GeoServerDesktop.App.ViewModels;

/// <summary>
/// GeoServer 安全设置的视图模型（展示服务层访问控制规则）
/// </summary>
public partial class SecuritySettingsViewModel : ViewModelBase
{
    private readonly IGeoServerConnectionService _connectionService;

    /// <summary>
    /// 服务层访问控制规则列表（以 "resource: role => access" 形式展示）
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _aclRules = new();

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// 状态消息
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// 初始化 SecuritySettingsViewModel 类的新实例
    /// </summary>
    /// <param name="connectionService">GeoServer 连接服务</param>
    public SecuritySettingsViewModel(IGeoServerConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    /// <summary>
    /// 加载安全设置命令
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
        StatusMessage = L.StatusLoadingSecuritySettings;

        try
        {
            var service = _connectionService.GetSecurityService();

            // 加载服务层（services）和图层层（layers）的 ACL 规则
            AclRules.Clear();

            foreach (var resource in new[] { "services", "layers" })
            {
                try
                {
                    var acl = await service.GetACLAsync(resource);
                    if (acl?.Rules != null)
                    {
                        foreach (var rule in acl.Rules)
                        {
                            AclRules.Add($"[{resource}] {rule.Role} => {rule.Access}");
                        }
                    }
                }
                catch
                {
                    // 某个资源不存在时跳过
                }
            }

            // FIXED-E36：原为硬编码中文串，英文态下本地化不生效；改为本地化键
            StatusMessage = AclRules.Count > 0
                ? string.Format(L.StatusAclRulesLoaded, AclRules.Count)
                : L.StatusNoAclRules;
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(L.StatusSecuritySettingsLoadFailed, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
