using System;
using System.Collections.Generic;
using System.Linq;
using GeoServerDesktop.App.Composition;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GeoServerDesktop.Tests.Headless;

/// <summary>
/// 组合根（M5 DI）离线测试：容器可构建、MainWindowViewModel 由容器提供、
/// 子 VM 惰性解析且为单例（跨访问稳定、跨导航状态驻留）、
/// 每个 ViewModelBase 子类都必须完成注册（新增 VM 漏注册即失败），
/// MainWindowViewModel 的 24 个子 VM 属性全部与容器实例一致。
/// 不触网：连接服务只注册不建连。
/// </summary>
public sealed class CompositionTests
{
    private static ServiceProvider Container() => AppServices.BuildDefault();

    [Fact]
    public void Container_ResolvesMainWindowViewModel_And_Infrastructure()
    {
        using var sp = Container();
        var vm = sp.GetRequiredService<MainWindowViewModel>();
        Assert.NotNull(vm);
        Assert.Same(vm, sp.GetRequiredService<MainWindowViewModel>());
        Assert.NotNull(sp.GetRequiredService<IGeoServerConnectionService>());
        Assert.NotNull(sp.GetRequiredService<ISettingsService>());
        Assert.False(sp.GetRequiredService<IGeoServerConnectionService>().IsConnected);
    }

    [Fact]
    public void SubViewModels_AreLazySingletons_MatchingContainerInstances()
    {
        using var sp = Container();
        var vm = sp.GetRequiredService<MainWindowViewModel>();

        var probes = new (Func<ViewModelBase> fromVm, Type serviceType)[]
        {
            (() => vm.DashboardViewModel, typeof(DashboardViewModel)),
            (() => vm.MapPreviewViewModel, typeof(MapPreviewViewModel)),
            (() => vm.WorkspaceManagementViewModel, typeof(WorkspaceManagementViewModel)),
            (() => vm.StyleManagementViewModel, typeof(StyleManagementViewModel)),
            (() => vm.StoresManagementViewModel, typeof(StoresManagementViewModel)),
            (() => vm.LayersManagementViewModel, typeof(LayersManagementViewModel)),
            (() => vm.LayerGroupsManagementViewModel, typeof(LayerGroupsManagementViewModel)),
            (() => vm.AboutViewModel, typeof(AboutViewModel)),
            (() => vm.WmsSettingsViewModel, typeof(WMSSettingsViewModel)),
            (() => vm.WfsSettingsViewModel, typeof(WFSSettingsViewModel)),
            (() => vm.WcsSettingsViewModel, typeof(WCSSettingsViewModel)),
            (() => vm.GlobalSettingsViewModel, typeof(GlobalSettingsViewModel)),
            (() => vm.LoggingViewModel, typeof(LoggingViewModel)),
            (() => vm.CachingDefaultsViewModel, typeof(CachingDefaultsViewModel)),
            (() => vm.GridsetsViewModel, typeof(GridsetsViewModel)),
            (() => vm.DiskQuotaViewModel, typeof(DiskQuotaViewModel)),
            (() => vm.SecuritySettingsViewModel, typeof(SecuritySettingsViewModel)),
            (() => vm.UsersGroupsRolesViewModel, typeof(UsersGroupsRolesViewModel)),
            (() => vm.ImportWizardViewModel, typeof(ImportWizardViewModel)),
            (() => vm.SldEditorViewModel, typeof(SldEditorViewModel)),
            (() => vm.StyleLibraryViewModel, typeof(StyleLibraryViewModel)),
            (() => vm.BatchOperationsViewModel, typeof(BatchOperationsViewModel)),
            (() => vm.WorkspaceMigrationViewModel, typeof(WorkspaceMigrationViewModel)),
            (() => vm.SettingsSyncViewModel, typeof(SettingsSyncViewModel)),
        };

        Assert.Equal(24, probes.Length);
        foreach (var (fromVm, type) in probes)
        {
            var first = fromVm();
            Assert.NotNull(first);
            Assert.Same(first, fromVm());                          // 惰性缓存稳定
            Assert.Same(sp.GetRequiredService(type), first);       // 与容器单例一致
        }
    }

    [Fact]
    public void EveryViewModelInApp_IsRegisteredInContainer()
    {
        using var sp = Container();
        var vms = typeof(MainWindowViewModel).Assembly.GetTypes()
            .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract
                     && typeof(ViewModelBase).IsAssignableFrom(t)
                     && t.Name.EndsWith("ViewModel"))
            .ToList();

        Assert.NotEmpty(vms);
        var unregistered = vms.Where(t => sp.GetService(t) == null).Select(t => t.Name).ToList();
        Assert.True(unregistered.Count == 0,
            "未注册到容器的 ViewModel：" + string.Join(", ", unregistered));
    }

    [Fact]
    public void WelcomeView_IsDashboard_NotPlaceholder()
    {
        using var sp = Container();
        var vm = sp.GetRequiredService<MainWindowViewModel>();
        Assert.IsType<DashboardViewModel>(vm.CurrentView);
    }
}
