using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>WMTSStoreService 离线单元测试。含 E30：WMTSStore 属性名 CapabilitiesUrl（WMSStore 为 CapabilitiesURL），JSON 键同为 capabilitiesURL。</summary>
public class WMTSStoreServiceTests
{
    private const string Base = "/rest/workspaces/ws/wmtsstores";
    private readonly RecordingFakeClient _fake = new();
    private readonly WMTSStoreService _svc;

    public WMTSStoreServiceTests() => _svc = new WMTSStoreService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new WMTSStoreService(null!));

    [Fact]
    public async Task GetWMTSStoresAsync_ParsesWmtsStoresWrapper()
    {
        _fake.RespondGet("""{"wmtsStores":{"wmtsStore":[{"name":"osm","type":"wmts","capabilitiesURL":"http://r/capabilities"}]}}""");

        var result = await _svc.GetWMTSStoresAsync("ws");

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal($"{Base}.json", _fake.Last.Path);
        Assert.Equal("http://r/capabilities", result[0].CapabilitiesUrl); // E30：C# 属性名与 WMSStore 大小写不一致，JSON 键一致
    }

    [Fact]
    public async Task GetWMTSStoreAsync_SingleUnwrap()
    {
        _fake.RespondGet("""{"wmtsStore":{"name":"osm","username":"u","password":"p","maxConnections":6,"readTimeout":30,"connectTimeout":15,"href":"h"}}""");

        var s = await _svc.GetWMTSStoreAsync("ws", "osm");

        Assert.Equal($"{Base}/osm.json", _fake.Last!.Path);
        Assert.Equal("u", s.Username);
        Assert.Equal(6, s.MaxConnections);
    }

    [Fact]
    public async Task CreateWMTSStoreAsync_PostsWrappedWmtsStore()
    {
        await _svc.CreateWMTSStoreAsync("ws", new WMTSStore { Name = "osm", CapabilitiesUrl = "http://r" });

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal(Base, _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("osm", (string?)body["wmtsStore"]?["name"]);
        Assert.Equal("http://r", (string?)body["wmtsStore"]?["capabilitiesURL"]);
    }

    [Fact]
    public async Task UpdateWMTSStoreAsync_PutsWrapped()
    {
        await _svc.UpdateWMTSStoreAsync("ws", "osm", new WMTSStore { Name = "osm" });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal($"{Base}/osm", _fake.Last.Path);
    }

    [Fact]
    public async Task DeleteWMTSStoreAsync_RecurseTernary_KNOWN_ISSUE_E22()
    {
        await _svc.DeleteWMTSStoreAsync("ws", "osm");
        Assert.Equal($"{Base}/osm?recurse=false", _fake.Last!.Path);

        await _svc.DeleteWMTSStoreAsync("ws", "osm", true);
        Assert.Equal("DELETE", _fake.Last.Method);
        Assert.Equal($"{Base}/osm?recurse=true", _fake.Last.Path);
    }
}

/// <summary>WMTSLayerService 离线单元测试。</summary>
public class WMTSLayerServiceTests
{
    private const string Base = "/rest/workspaces/ws/wmtsstores/store/wmtslayers";
    private readonly RecordingFakeClient _fake = new();
    private readonly WMTSLayerService _svc;

    public WMTSLayerServiceTests() => _svc = new WMTSLayerService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new WMTSLayerService(null!));

    [Fact]
    public async Task GetWMTSLayersAsync_ParsesWmtsLayersWrapper()
    {
        _fake.RespondGet("""{"wmtsLayers":{"wmtsLayer":[{"name":"l","nativeName":"nl","advertised":true}]}}""");

        var result = await _svc.GetWMTSLayersAsync("ws", "store");

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal($"{Base}.json", _fake.Last.Path);
        Assert.True(result[0].Advertised); // WMTSLayer 特有 advertised 字段（bool?）
    }

    [Fact]
    public async Task GetWMTSLayerAsync_SingleUnwrap()
    {
        _fake.RespondGet("""{"wmtsLayer":{"name":"l","title":"t","srs":"EPSG:3857","store":{"@class":"wmtsStore","name":"store"}}}""");

        var l = await _svc.GetWMTSLayerAsync("ws", "store", "l");

        Assert.Equal($"{Base}/l.json", _fake.Last!.Path);
        Assert.Equal("EPSG:3857", l.Srs);
        Assert.Equal("wmtsStore", l.Store.Class);
    }

    [Fact]
    public async Task CreateWMTSLayerAsync_PostsWrapped()
    {
        await _svc.CreateWMTSLayerAsync("ws", "store", new WMTSLayer { Name = "l" });

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal(Base, _fake.Last.Path);
        Assert.Equal("l", (string?)Json.P(_fake.Last.Body)["wmtsLayer"]?["name"]);
    }

    [Fact]
    public async Task UpdateWMTSLayerAsync_PutsWrapped()
    {
        await _svc.UpdateWMTSLayerAsync("ws", "store", "l", new WMTSLayer { Name = "l" });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal($"{Base}/l", _fake.Last.Path);
    }

    [Fact]
    public async Task DeleteWMTSLayerAsync_RecurseTernary_KNOWN_ISSUE_E22()
    {
        await _svc.DeleteWMTSLayerAsync("ws", "store", "l");
        Assert.Equal($"{Base}/l?recurse=false", _fake.Last!.Path);

        await _svc.DeleteWMTSLayerAsync("ws", "store", "l", true);
        Assert.Equal("DELETE", _fake.Last.Method);
        Assert.Equal($"{Base}/l?recurse=true", _fake.Last.Path);
    }
}
