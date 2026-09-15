using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>OGC 服务设置类服务（WMS/WFS/WCS/WMTS/WPS/CSW）离线单元测试合集。
/// 注意 "abstrct" 拼写：GeoServer ServiceDTO 官方自身的已知拼写错误，客户端有意对齐（清单 E28，勿"修正"）。</summary>
public class OgcSettingsServiceTests
{
    [Fact]
    public void AllCtors_NullClient_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new WMSSettingsService(null!));
        Assert.Throws<ArgumentNullException>(() => new WFSSettingsService(null!));
        Assert.Throws<ArgumentNullException>(() => new WCSSettingsService(null!));
        Assert.Throws<ArgumentNullException>(() => new WMTSSettingsService(null!));
        Assert.Throws<ArgumentNullException>(() => new WPSSettingsService(null!));
        Assert.Throws<ArgumentNullException>(() => new CSWSettingsService(null!));
    }

    // ---------- WMS ----------

    [Fact]
    public async Task Wms_GetParsesWrapperWithAbstrctSpelling_KNOWN_ISSUE_E28_PIN()
    {
        var fake = new RecordingFakeClient();
        var svc = new WMSSettingsService(fake);
        // 真实 GeoServer 返回键即为 "abstrct"（官方拼写错误），映射到 Abstract 属性
        fake.RespondGet("""{"wms":{"enabled":true,"name":"WMS","title":"t","abstrct":"a","maintainer":"m","onlineResource":"o","citeCompliant":false,"schemaBaseURL":"s","verbose":false,"maxRequestMemory":0,"maxRenderingErrors":0,"dynamicStylingDisabled":false,"srs":{"string":["EPSG:4326","EPSG:3857"]},"watermark":{"enabled":false,"position":"BOTTOM_CENTER","transparency":0,"url":""},"metadataLink":{"type":"ISO19115:2003","metadataType":"other","content":"c"}}}""");

        var s = await svc.GetWMSSettingsAsync();

        Assert.Equal("GET", fake.Last!.Method);
        Assert.Equal("/rest/services/wms/settings.json", fake.Last.Path);
        Assert.True(s.WMS.Enabled);
        Assert.Equal("a", s.WMS.Abstract); // "abstrct" → Abstract
        Assert.Equal(new[] { "EPSG:4326", "EPSG:3857" }, s.WMS.SRS.Strings);
        Assert.Equal(0, s.WMS.MaxRequestMemory);
        Assert.False(s.WMS.Watermark.Enabled);
        Assert.Equal("ISO19115:2003", s.WMS.MetadataLink.Type);
    }

    [Fact]
    public async Task Wms_UpdatePutsWrappedWithAbstrctKeyPin_KNOWN_ISSUE_E28_PIN()
    {
        var fake = new RecordingFakeClient();
        var svc = new WMSSettingsService(fake);
        await svc.UpdateWMSSettingsAsync(new WMSSettings { WMS = new WMSServiceConfig { Abstract = "abc", Name = "WMS" } });

        Assert.Equal("PUT", fake.Last!.Method);
        Assert.Equal("/rest/services/wms/settings", fake.Last.Path);
        var body = Json.P(fake.Last.Body);
        Assert.Equal("abc", (string?)body["wms"]?["abstrct"]); // 固化：请求体键为 "abstrct" 而非 "abstract"
        Assert.Null(body["wms"]?["abstract"]);
    }

    [Fact]
    public async Task Wms_WorkspaceScopedGetAndPutPaths()
    {
        var fake = new RecordingFakeClient();
        var svc = new WMSSettingsService(fake);
        fake.RespondGet("""{"wms":{"enabled":false}}""");

        var s = await svc.GetWorkspaceWMSSettingsAsync("ws");
        Assert.Equal("/rest/services/wms/workspaces/ws/settings.json", fake.Last!.Path);
        Assert.False(s.WMS.Enabled);

        await svc.UpdateWorkspaceWMSSettingsAsync("ws", new WMSSettings { WMS = new WMSServiceConfig { Name = "x" } });
        Assert.Equal("PUT", fake.Last.Method);
        Assert.Equal("/rest/services/wms/workspaces/ws/settings", fake.Last.Path);
    }

    // ---------- WFS ----------

    [Fact]
    public async Task Wfs_GetParsesWfsRootAndExtraFields()
    {
        var fake = new RecordingFakeClient();
        var svc = new WFSSettingsService(fake);
        fake.RespondGet("""{"wfs":{"enabled":true,"abstrct":"a","maxFeatures":100,"serviceLevel":"COMPLETE","featureBounding":true,"gml2":{"overrideGMLAttributes":false,"srsNameStyle":"URN"},"gml3":{"overrideGMLAttributes":true,"srsNameStyle":"XML"},"canonicalSchemaLocation":false,"encodeFeatureMember":false,"hitsIgnoreMaxFeatures":false}}""");

        var s = await svc.GetWFSSettingsAsync();

        Assert.Equal("/rest/services/wfs/settings.json", fake.Last!.Path);
        Assert.Equal(100, s.WFS.MaxFeatures);
        Assert.Equal("COMPLETE", s.WFS.ServiceLevel);
        Assert.True(s.WFS.FeatureBounding);
        Assert.Equal("URN", s.WFS.GML2.SrsNameStyle);
        Assert.True(s.WFS.GML3.OverrideGMLAttributes);
        Assert.Equal("a", s.WFS.Abstract); // abstrct
    }

    [Fact]
    public async Task Wfs_UpdateAndWorkspaceScopedPaths()
    {
        var fake = new RecordingFakeClient();
        var svc = new WFSSettingsService(fake);

        await svc.UpdateWFSSettingsAsync(new WFSSettings { WFS = new WFSServiceConfig() });
        Assert.Equal("PUT", fake.Last!.Method);
        Assert.Equal("/rest/services/wfs/settings", fake.Last.Path);
        Assert.Equal("wfs", Assert.Single(Json.P(fake.Last.Body).Properties()).Name);

        fake.RespondGet("""{"wfs":{"name":"WFS"}}""");
        var s = await svc.GetWorkspaceWFSSettingsAsync("ws");
        Assert.Equal("/rest/services/wfs/workspaces/ws/settings.json", fake.Last.Path);
        Assert.Equal("WFS", s.WFS.Name);

        await svc.UpdateWorkspaceWFSSettingsAsync("ws", new WFSSettings { WFS = new WFSServiceConfig() });
        Assert.Equal("/rest/services/wfs/workspaces/ws/settings", fake.Last.Path);
    }

    // ---------- WCS ----------

    [Fact]
    public async Task Wcs_GetParsesWcsRootAndExtraFields()
    {
        var fake = new RecordingFakeClient();
        var svc = new WCSSettingsService(fake);
        fake.RespondGet("""{"wcs":{"enabled":true,"abstrct":"a","maxInputMemory":0,"maxOutputMemory":0,"subsamplingEnabled":true,"overviewPolicy":"SKIP","resourceConsumptionLimits":{"maxDimensions":2,"maxRequestSizePixels":10000}}}""");

        var s = await svc.GetWCSSettingsAsync();

        Assert.Equal("/rest/services/wcs/settings.json", fake.Last!.Path);
        Assert.True(s.WCS.SubsamplingEnabled);
        Assert.Equal("SKIP", s.WCS.OverviewPolicy);
        Assert.Equal(2, s.WCS.ResourceConsumptionLimits.MaxDimensions);
        Assert.Equal(10000, s.WCS.ResourceConsumptionLimits.MaxRequestSizePixels);
    }

    [Fact]
    public async Task Wcs_UpdateAndWorkspaceScopedPaths()
    {
        var fake = new RecordingFakeClient();
        var svc = new WCSSettingsService(fake);

        await svc.UpdateWCSSettingsAsync(new WCSSettings { WCS = new WCSServiceConfig() });
        Assert.Equal("/rest/services/wcs/settings", fake.Last!.Path);
        Assert.Equal("wcs", Assert.Single(Json.P(fake.Last.Body).Properties()).Name);

        fake.RespondGet("""{"wcs":{"name":"WCS"}}""");
        var s = await svc.GetWorkspaceWCSSettingsAsync("ws");
        Assert.Equal("/rest/services/wcs/workspaces/ws/settings.json", fake.Last.Path);
        Assert.Equal("WCS", s.WCS.Name);

        await svc.UpdateWorkspaceWCSSettingsAsync("ws", new WCSSettings { WCS = new WCSServiceConfig() });
        Assert.Equal("/rest/services/wcs/workspaces/ws/settings", fake.Last.Path);
    }

    // ---------- WMTS ----------

    [Fact]
    public async Task Wmts_GetParsesWmtsRoot_FIXED_E15()
    {
        var fake = new RecordingFakeClient();
        var svc = new WMTSSettingsService(fake);
        // FIXED-E15（判为误报）：3.0.1 实测 GET /rest/services/wmts/settings.json 根就是 {"wmts":{...}}，
        // 与模型一致（curl 复核：GET→PUT 同根往返 200 且字段回读一致），E15 预期的根 "settings" 不存在。
        fake.RespondGet("""{"wmts":{"enabled":true,"name":"Tiles","title":"Tile Services","abstrct":"a","citeCompliant":false,"onlineResource":"https://geoserver.org","verbose":false,"keywords":{"string":["TILESET","WMTS","GEOSERVER"]}}}""");

        var s = await svc.GetWMTSSettingsAsync();

        Assert.Equal("/rest/services/wmts/settings.json", fake.Last!.Path);
        Assert.NotNull(s.WMTS);
        Assert.Equal("Tiles", s.WMTS.Name);
        Assert.Equal("a", s.WMTS.Abstract); // abstrct 拼写族（E28）同样适用 WMTS
        Assert.True(s.WMTS.Enabled);
    }

    [Fact]
    public async Task Wmts_UpdatePutsWmtsRoot_FIXED_E15()
    {
        var fake = new RecordingFakeClient();
        var svc = new WMTSSettingsService(fake);
        await svc.UpdateWMTSSettingsAsync(new WMTSSettings { WMTS = new WMTSServiceConfig { Name = "WMTS" } });

        Assert.Equal("PUT", fake.Last!.Method);
        Assert.Equal("/rest/services/wmts/settings", fake.Last.Path);
        // 根 "wmts" 即服务端期望形态（实测 PUT 200 + 回读生效，往返基线见 ServiceSettingsIT）
        Assert.Equal("wmts", Assert.Single(Json.P(fake.Last.Body).Properties()).Name);
    }

    // ---------- WPS ----------

    [Fact]
    public async Task Wps_GetAndPutRootIsWps()
    {
        var fake = new RecordingFakeClient();
        var svc = new WPSSettingsService(fake);
        fake.RespondGet("""{"wps":{"enabled":true,"name":"WPS","abstrct":"a","connectionTimeout":300,"resourceExpirationTimeout":3600,"maxSynchronousProcesses":4,"maxAsynchronousProcesses":8,"citeCompliant":false,"schemaBaseURL":"s"}}""");

        var s = await svc.GetSettingsAsync();

        Assert.Equal("GET", fake.Last!.Method);
        Assert.Equal("/rest/services/wps/settings.json", fake.Last.Path);
        Assert.Equal(300, s.WPS.ConnectionTimeout);
        Assert.Equal(8, s.WPS.MaxAsynchronousProcesses);
        Assert.Equal("a", s.WPS.Abstract);

        await svc.UpdateSettingsAsync(new WPSSettings { WPS = new WPSServiceConfig { Name = "WPS" } });
        Assert.Equal("PUT", fake.Last.Method);
        Assert.Equal("/rest/services/wps/settings", fake.Last.Path);
        Assert.Equal("wps", Assert.Single(Json.P(fake.Last.Body).Properties()).Name);
    }

    // ---------- CSW ----------

    [Fact]
    public async Task Csw_GetAndPutRootIsCsw()
    {
        var fake = new RecordingFakeClient();
        var svc = new CSWSettingsService(fake);
        fake.RespondGet("""{"csw":{"enabled":true,"name":"CSW","abstrct":"a","maintainer":"m","onlineResource":"o","schemaBaseURL":"s"}}""");

        var s = await svc.GetSettingsAsync();

        Assert.Equal("/rest/services/csw/settings.json", fake.Last!.Path);
        Assert.Equal("CSW", s.CSW.Name);
        Assert.Equal("a", s.CSW.Abstract);

        await svc.UpdateSettingsAsync(new CSWSettings { CSW = new CSWServiceConfig { Name = "CSW" } });
        Assert.Equal("PUT", fake.Last.Method);
        Assert.Equal("/rest/services/csw/settings", fake.Last.Path);
        Assert.Equal("csw", Assert.Single(Json.P(fake.Last.Body).Properties()).Name);
    }
}
