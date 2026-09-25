using Microsoft.Extensions.DependencyInjection;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.App.ViewModels;

namespace GeoServerDesktop.App.Composition;

/// <summary>
/// 应用级服务注册（M5 架构收敛）：连接/设置等基础服务与全部子 ViewModel 集中登记，
/// 由 <see cref="MainWindowViewModel"/> 按需惰性解析（避免构造期一次性 new 出 24 个子 VM
/// 并各自持有未连接的客户端实例）。
///
/// 约定：
/// ① 子 VM 一律单例——保持与原实现相同的跨导航状态驻留（列表、选中项、状态消息不丢）；
/// ② ViewModel 只依赖抽象（<see cref="IGeoServerConnectionService"/> / <see cref="ISettingsService"/>），
///    便于测试替身与未来插件系统注入；
/// ③ <see cref="MainWindowViewModel"/> 注册为工厂形式，把容器自身交给它做子 VM 惰性解析入口
///    （唯一的 composition-root 例外，其余类型均为构造注入）。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>注册应用服务与视图模型。</summary>
    /// <param name="services">服务集合。</param>
    /// <returns>同一服务集合，便于链式调用。</returns>
    public static IServiceCollection AddGeoServerDesktopUi(this IServiceCollection services)
    {
        // ── 基础设施 ──
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IGeoServerConnectionService, GeoServerConnectionService>();

        // ── 子 ViewModel（单例，保留跨导航状态） ──
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<MapPreviewViewModel>();
        services.AddSingleton<WorkspaceManagementViewModel>();
        services.AddSingleton<StoresManagementViewModel>();
        services.AddSingleton<LayersManagementViewModel>();
        services.AddSingleton<LayerGroupsManagementViewModel>();
        services.AddSingleton<StyleManagementViewModel>();
        services.AddSingleton<StyleLibraryViewModel>();
        services.AddSingleton<SldEditorViewModel>();
        services.AddSingleton<ImportWizardViewModel>();
        services.AddSingleton<BatchOperationsViewModel>();
        services.AddSingleton<WorkspaceMigrationViewModel>();
        services.AddSingleton<SettingsSyncViewModel>();
        services.AddSingleton<AboutViewModel>();
        services.AddSingleton<GlobalSettingsViewModel>();
        services.AddSingleton<LoggingViewModel>();
        services.AddSingleton<WMSSettingsViewModel>();
        services.AddSingleton<WFSSettingsViewModel>();
        services.AddSingleton<WCSSettingsViewModel>();
        services.AddSingleton<CachingDefaultsViewModel>();
        services.AddSingleton<GridsetsViewModel>();
        services.AddSingleton<DiskQuotaViewModel>();
        services.AddSingleton<SecuritySettingsViewModel>();
        services.AddSingleton<UsersGroupsRolesViewModel>();

        // ── 组合根 ──
        services.AddSingleton(sp => new MainWindowViewModel(sp));
        return services;
    }
}

/// <summary>
/// 应用容器构建入口（composition root）。
/// </summary>
public static class AppServices
{
    /// <summary>
    /// 构建默认容器（注册全部应用服务与视图模型）。
    /// </summary>
    /// <returns>服务提供者。</returns>
    public static ServiceProvider BuildDefault()
    {
        var sc = new ServiceCollection();
        sc.AddGeoServerDesktopUi();
        return sc.BuildServiceProvider();
    }
}
