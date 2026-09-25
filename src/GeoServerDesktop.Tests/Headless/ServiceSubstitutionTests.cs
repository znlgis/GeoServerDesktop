using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.App.Composition;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GeoServerDesktop.Tests.Headless;

/// <summary>
/// 扩展点验证（M5 收尾）：接口化 + DI 之后，服务抽象可以在不改动任何产品代码的前提下被替换——
/// 这正是规划里"插件系统技术验证"要证明的那件事。
/// 本用例以容器覆盖注册一个假 <see cref="ILayerService"/>，驱动真实
/// <see cref="LayersManagementViewModel"/> 走完整命令流，断言其只经接口取数。
/// 说明：正式的插件机制（外部程序集加载、清单、版本与沙箱策略）不在本次范围，
/// 待有真实第三方扩展需求时再立项；此处固化的注入点是它的前置条件。
/// </summary>
public sealed class ServiceSubstitutionTests
{
    /// <summary>假图层服务：只实现接口，不经网络。</summary>
    private sealed class FakeLayerService : ILayerService
    {
        public static readonly FakeLayerService Instance = new();
        public int Calls;

        public Task<Layer[]> GetLayersAsync()
        {
            Calls++;
            return Task.FromResult(new[]
            {
                new Layer { Name = "plugin:alpha", Type = "VECTOR" },
                new Layer { Name = "plugin:beta", Type = "RASTER" },
            });
        }

        public Task<Layer> GetLayerAsync(string layerName) =>
            Task.FromResult(new Layer { Name = layerName, Type = "VECTOR" });

        public Task UpdateLayerAsync(string layerName, Layer layer) => Task.CompletedTask;
        public Task DeleteLayerAsync(string layerName, bool recurse = false) => Task.CompletedTask;
        public Task<Layer[]> GetWorkspaceLayersAsync(string workspaceName) => GetLayersAsync();
        public Task<Layer> GetWorkspaceLayerAsync(string workspaceName, string layerName) =>
            GetLayerAsync(layerName);
    }

    /// <summary>假连接服务：只为LayersManagementViewModel提供 ILayerService 通道，其余成员未触达。</summary>
    private sealed class SubstitutedConnection : IGeoServerConnectionService
    {
        public FakeLayerService Layers { get; } = new FakeLayerService();
        public bool IsConnected => true;
        public GeoServerDesktop.GeoServerClient.Configuration.GeoServerClientOptions? CurrentOptions => null;
        public event EventHandler<bool>? ConnectionStatusChanged;
        public void Connect(GeoServerDesktop.GeoServerClient.Configuration.GeoServerClientOptions options) { }
        public void Disconnect() { ConnectionStatusChanged?.Invoke(this, false); }

        public IWorkspaceService GetWorkspaceService() => throw new NotSupportedException("本用例不涉及");
        public IDataStoreService GetDataStoreService() => throw new NotSupportedException("本用例不涉及");
        public ICoverageStoreService GetCoverageStoreService() => throw new NotSupportedException("本用例不涉及");
        public ILayerService GetLayerService() => Layers;
        public IStyleService GetStyleService() => throw new NotSupportedException("本用例不涉及");
        public ILayerGroupService GetLayerGroupService() => throw new NotSupportedException("本用例不涉及");
        public IFeatureTypeService GetFeatureTypeService() => throw new NotSupportedException("本用例不涉及");
        public IPreviewService GetPreviewService() => throw new NotSupportedException("本用例不涉及");
        public IAboutService GetAboutService() => throw new NotSupportedException("本用例不涉及");
        public GeoServerDesktop.GeoServerClient.Services.ISettingsService GetGlobalSettingsService() => throw new NotSupportedException("本用例不涉及");
        public ILoggingService GetLoggingService() => throw new NotSupportedException("本用例不涉及");
        public IWMSSettingsService GetWMSSettingsService() => throw new NotSupportedException("本用例不涉及");
        public IWFSSettingsService GetWFSSettingsService() => throw new NotSupportedException("本用例不涉及");
        public IWCSSettingsService GetWCSSettingsService() => throw new NotSupportedException("本用例不涉及");
        public IDiskQuotaService GetDiskQuotaService() => throw new NotSupportedException("本用例不涉及");
        public IGridsetService GetGridsetService() => throw new NotSupportedException("本用例不涉及");
        public ISecurityService GetSecurityService() => throw new NotSupportedException("本用例不涉及");
        public IUserGroupService GetUserGroupService() => throw new NotSupportedException("本用例不涉及");
        public IRoleService GetRoleService() => throw new NotSupportedException("本用例不涉及");
        public IResourceService GetResourceService() => throw new NotSupportedException("本用例不涉及");
        public IImportWizardService GetImportWizardService() => throw new NotSupportedException("本用例不涉及");
        public IStyleUsageService GetStyleUsageService() => throw new NotSupportedException("本用例不涉及");
        public IBatchOperationService GetBatchOperationService() => throw new NotSupportedException("本用例不涉及");
        public GeoServerDesktop.GeoServerClient.Migration.IWorkspaceMigrationService GetWorkspaceMigrationService() => throw new NotSupportedException("本用例不涉及");
        public GeoServerDesktop.GeoServerClient.Migration.ISettingsCompareService GetSettingsCompareService() => throw new NotSupportedException("本用例不涉及");
    }

    [Fact]
    public async Task ServiceInterface_CanBeSubstitutedThroughContainer_NoServerNeeded()
    {
        var sc = new ServiceCollection();
        sc.AddGeoServerDesktopUi();
        var sub = new SubstitutedConnection();
        sc.AddSingleton<IGeoServerConnectionService>(sub);
        using var sp = sc.BuildServiceProvider();

        var vm = sp.GetRequiredService<LayersManagementViewModel>();
        // 图层 VM 需先有选中工作空间；用"全部工作空间"哨兵走 ILayerService.GetLayersAsync 通道
        vm.SelectedWorkspace = "All Workspaces";
        await vm.LoadLayersCommand.ExecuteAsync(null);

        // 数据全部来自被替换的服务实现（未触网）：接口即注入点
        Assert.True(sub.Layers.Calls > 0, "假服务未被调用，说明 VM 未走接口通道");
        Assert.Contains(vm.Layers, l => l.Name == "plugin:alpha");
        Assert.Contains(vm.Layers, l => l.Name == "plugin:beta");
        Assert.Equal(2, vm.Layers.Count(l => l.Name.StartsWith("plugin:", StringComparison.Ordinal)));
    }

    [Fact]
    public void EveryServiceGetter_ReturnsInterfaceType()
    {
        var seam = typeof(IGeoServerConnectionService);
        var getters = seam.GetMethods().Where(m => m.Name.StartsWith("Get") && m.Name.EndsWith("Service")).ToList();
        Assert.True(getters.Count >= 20, "接缝 getter 数量异常：" + getters.Count);

        var concrete = getters.Where(m => !m.ReturnType.Name.StartsWith("I")).Select(m => m.Name).ToList();
        Assert.True(concrete.Count == 0, "接缝仍暴露具体类型：" + string.Join(", ", concrete));
    }

    [Fact]
    public void MainWindowViewModel_Works_WithExternallyProvidedContainer()
    {
        // 组合根可被整体替换（插件宿主场景）：外部容器注册自定义连接服务后，
        // MainWindowViewModel 惰性解析出的子 VM 共享同一实例
        var sc = new ServiceCollection();
        sc.AddGeoServerDesktopUi();
        sc.AddSingleton<IGeoServerConnectionService>(new SubstitutedConnection());
        using var sp = sc.BuildServiceProvider();

        var shell = new MainWindowViewModel(sp);
        Assert.Same(sp.GetRequiredService<LayersManagementViewModel>(), shell.LayersManagementViewModel);
        Assert.Same(sp.GetRequiredService<StyleLibraryViewModel>(), shell.StyleLibraryViewModel);
    }
}
