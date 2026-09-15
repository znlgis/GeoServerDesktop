using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>GWCLayerService 离线单元测试。FIXED-E8：layers.json 为顶层裸数组；FIXED-E9：单体根为实现类名
/// {"GeoServerLayer":{...}}；FIXED-E12：seed/truncate 同路由 POST /gwc/rest/seed/{layer} + XStream XML
/// （zoomStart/zoomStop 必填），masstruncate 按 MassTruncateController 源码以 XML 体提交。</summary>
public class GWCLayerServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly GWCLayerService _svc;

    public GWCLayerServiceTests() => _svc = new GWCLayerService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new GWCLayerService(null!));

    [Fact]
    public async Task GetLayersAsync_RawJsonArray_FIXED_E8()
    {
        // 实测（3.0.1/GWC 2.0.1）：/gwc/rest/layers.json 顶层就是 ["ws:layer",...]，无任何包装键
        _fake.RespondGet("""["topp:states","ws2:l2"]""");
        var w = await _svc.GetLayersAsync();

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/gwc/rest/layers.json", _fake.Last.Path);
        Assert.Equal(2, w.Layers.Count);
        Assert.Equal("topp:states", w.Layers[0]);
    }

    [Fact]
    public async Task GetLayerAsync_ParsesGeoServerLayerRoot_EscapedPath_FIXED_E9()
    {
        // 单体实测根为实现类名 {"GeoServerLayer":{...}}；层名含 ":" 走 URL 转义
        _fake.RespondGet("""{"GeoServerLayer":{"name":"topp:states","id":"topp:states","enabled":true,"mimeFormats":["image/png"],"gridSubsets":[{"gridSetName":"EPSG:4326","extent":{"coords":[-180.0,-90.0,180.0,90.0]}}],"metaWidthHeight":[4,4],"expireCache":0,"expireClients":0}}""");

        var l = await _svc.GetLayerAsync("topp:states");

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/gwc/rest/layers/topp%3Astates.json", _fake.Last.Path);
        Assert.Equal("topp:states", l.Name);
        Assert.True(l.Enabled);
        Assert.Equal("EPSG:4326", l.GridSubsets[0].GridSetName);
        Assert.Equal(new[] { 4, 4 }, l.MetaWidthHeight);
    }

    [Fact]
    public async Task SeedLayerAsync_PostsXmlBodyToSeedRoute_FIXED_E12()
    {
        var req = new SeedRequest
        {
            Config = new SeedRequestConfig { Name = "topp:states", GridSetId = "EPSG:4326", ZoomStart = 0, ZoomStop = 3, Format = "image/png", Type = "seed", ThreadCount = 2 }
        };
        await _svc.SeedLayerAsync("topp:states", req);

        Assert.Equal("POST", _fake.Last!.Method);
        // 路由 /gwc/rest/seed/{layer}（无 .json 后缀；.xml/.json 后缀同 handler）
        Assert.Equal("/gwc/rest/seed/topp%3Astates", _fake.Last.Path);
        Assert.NotNull(_fake.Last.ContentType);
        Assert.Contains("application/xml", _fake.Last.ContentType);
        Assert.Contains("<seedRequest>", _fake.Last.Body);
        Assert.Contains("<zoomStart>0</zoomStart>", _fake.Last.Body);
        Assert.Contains("<zoomStop>3</zoomStop>", _fake.Last.Body); // 必填项
        Assert.Contains("<type>seed</type>", _fake.Last.Body);
    }

    [Fact]
    public async Task TruncateLayer_UsesSameSeedRouteWithTruncateType_FIXED_E12()
    {
        // seed/truncate 同路由：type=truncate 的 seedRequest XML
        var req = new SeedRequest
        {
            Config = new SeedRequestConfig { Name = "l", GridSetId = "g", ZoomStart = 0, ZoomStop = 1, Type = "truncate" }
        };
        await _svc.SeedLayerAsync("l", req);

        Assert.Equal("/gwc/rest/seed/l", _fake.Last!.Path);
        Assert.Contains("<type>truncate</type>", _fake.Last.Body);
    }

    [Fact]
    public async Task TruncateAllLayersAsync_PostsXmlMassTruncateRequest_FIXED_E12()
    {
        // MassTruncateController（gwc-rest）以 XStream DomDriver 读 XML：空 <massTruncateRequest/> 即全清
        await _svc.TruncateAllLayersAsync();

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/gwc/rest/masstruncate", _fake.Last.Path);
        Assert.Contains("application/xml", _fake.Last.ContentType);
        Assert.Contains("<massTruncateRequest/>", _fake.Last.Body);
    }

    [Fact]
    public void NoSingleLayerTruncateMethod_CurrentSurfacePin()
    {
        // 单图层 truncate 复用 SeedLayerAsync（type=truncate），不设独立方法——固化方法面
        Assert.Null(typeof(GWCLayerService).GetMethod("TruncateLayerAsync"));
    }
}
