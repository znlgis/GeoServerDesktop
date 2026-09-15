using System;
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.GeoServerClient.Services;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>GeoServerClientFactory 离线单元测试（D 组）：45 个 Create*、null options、Dispose 后可再建。</summary>
public class FactoryTests
{
    private static GeoServerClientOptions Options() =>
        new() { BaseUrl = "http://localhost:8080/geoserver", Username = "admin", Password = "geoserver" };

    [Fact]
    public void Ctor_NullOptions_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new GeoServerClientFactory(null!));
        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public void All45CreateMethods_ReturnNonNullInstances()
    {
        // 全部 45 个工厂方法返回非 null 的具体服务类型实例（D 组覆盖清单）
        using var f = new GeoServerClientFactory(Options());

        Assert.NotNull(f.CreateWorkspaceService());
        Assert.NotNull(f.CreateDataStoreService());
        Assert.NotNull(f.CreateLayerService());
        Assert.NotNull(f.CreateStyleService());
        Assert.NotNull(f.CreateLayerGroupService());
        Assert.NotNull(f.CreateFeatureTypeService());
        Assert.NotNull(f.CreatePreviewService());
        Assert.NotNull(f.CreateNamespaceService());
        Assert.NotNull(f.CreateCoverageStoreService());
        Assert.NotNull(f.CreateCoverageService());
        Assert.NotNull(f.CreateAboutService());
        Assert.NotNull(f.CreateSettingsService());
        Assert.NotNull(f.CreateReloadService());
        Assert.NotNull(f.CreateWMSStoreService());
        Assert.NotNull(f.CreateWMSLayerService());
        Assert.NotNull(f.CreateWMTSStoreService());
        Assert.NotNull(f.CreateWMTSLayerService());
        Assert.NotNull(f.CreateLoggingService());
        Assert.NotNull(f.CreateResourceService());
        Assert.NotNull(f.CreateWMSSettingsService());
        Assert.NotNull(f.CreateWFSSettingsService());
        Assert.NotNull(f.CreateWCSSettingsService());
        Assert.NotNull(f.CreateWMTSSettingsService());
        Assert.NotNull(f.CreateSecurityService());
        Assert.NotNull(f.CreateUserGroupService());
        Assert.NotNull(f.CreateRoleService());
        Assert.NotNull(f.CreateFontService());
        Assert.NotNull(f.CreateTemplateService());
        Assert.NotNull(f.CreateGWCLayerService());
        Assert.NotNull(f.CreateDiskQuotaService());
        Assert.NotNull(f.CreateGridsetService());
        Assert.NotNull(f.CreateImporterService());
        Assert.NotNull(f.CreateMonitoringService());
        Assert.NotNull(f.CreateTransformService());
        Assert.NotNull(f.CreateURLCheckService());
        Assert.NotNull(f.CreateAuthenticationFilterService());
        Assert.NotNull(f.CreateAuthenticationProviderService());
        Assert.NotNull(f.CreateFilterChainService());
        Assert.NotNull(f.CreatePasswordService());
        Assert.NotNull(f.CreateKeystoreService());
        Assert.NotNull(f.CreateBlobstoreService());
        Assert.NotNull(f.CreateStructuredCoverageService());
        Assert.NotNull(f.CreateCoverageViewService());
        Assert.NotNull(f.CreateWPSSettingsService());
        Assert.NotNull(f.CreateCSWSettingsService());
    }

    [Fact]
    public void CreateMethods_ReturnSpecificTypesAndFreshInstances()
    {
        using var f = new GeoServerClientFactory(Options());

        // 返回具体类型（非接口，清单 C 组注意点）且每次新建
        WorkspaceService a = f.CreateWorkspaceService();
        WorkspaceService b = f.CreateWorkspaceService();
        Assert.NotSame(a, b);
        Assert.IsType<WorkspaceService>(a);
        Assert.IsType<PreviewService>(f.CreatePreviewService());
    }

    [Fact]
    public void CreatePreviewService_UsesOptionsBaseUrlAndTrimsInCtor()
    {
        // CreatePreviewService 直接透传 options.BaseUrl（尾斜杠由 PreviewService 构造函数裁剪）
        using var f = new GeoServerClientFactory(new GeoServerClientOptions { BaseUrl = "http://host:8080/geoserver/" });
        var p = f.CreatePreviewService();
        Assert.Equal("http://host:8080/geoserver/wms?service=WMS&version=1.1.0&request=GetCapabilities",
            p.GetCapabilitiesUrl());
    }

    [Fact]
    public void AfterDispose_CanCreateAgain()
    {
        // 清单：Dispose 释放并置空内部 client；再 Create 时 GetHttpClient() 会重新 new，工厂仍可复用
        var f = new GeoServerClientFactory(Options());
        var first = f.CreateWorkspaceService();
        Assert.NotNull(first);

        f.Dispose();

        Assert.NotNull(f.CreateWorkspaceService());
        Assert.NotNull(f.CreateStyleService());
        f.Dispose(); // 二次 Dispose 幂等
    }
}
