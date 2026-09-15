using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>WMSStoreService（级联 WMS 存储）离线单元测试。</summary>
public class WMSStoreServiceTests
{
    private const string Base = "/rest/workspaces/ws/wmsstores";
    private readonly RecordingFakeClient _fake = new();
    private readonly WMSStoreService _svc;

    public WMSStoreServiceTests() => _svc = new WMSStoreService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new WMSStoreService(null!));

    [Fact]
    public async Task GetWMSStoresAsync_ParsesWmsStoresWrapper()
    {
        _fake.RespondGet("""{"wmsStores":{"wmsStore":[{"name":"ogc","type":"wms","capabilitiesURL":"http://remote/wms"}]}}""");

        var result = await _svc.GetWMSStoresAsync("ws");

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal($"{Base}.json", _fake.Last.Path);
        Assert.Single(result);
        Assert.Equal("http://remote/wms", result[0].CapabilitiesURL); // WMSStore 属性名为 CapabilitiesURL（注意与 WMTSStore 的大小写差异，E30）
    }

    [Fact]
    public async Task GetWMSStoreAsync_ParsesAuthAndTimeoutFields()
    {
        _fake.RespondGet("""{"wmsStore":{"name":"ogc","description":"d","username":"u","password":"p","maxConnections":3,"readTimeout":60,"connectTimeout":90,"enabled":true,"href":"h"}}""");

        var s = await _svc.GetWMSStoreAsync("ws", "ogc");

        Assert.Equal($"{Base}/ogc.json", _fake.Last!.Path);
        Assert.Equal("u", s.Username);
        Assert.Equal("p", s.Password);
        Assert.Equal(3, s.MaxConnections);
        Assert.Equal(60, s.ReadTimeout);
        Assert.Equal(90, s.ConnectTimeout);
    }

    [Fact]
    public async Task CreateWMSStoreAsync_PostsWrappedWmsStore()
    {
        await _svc.CreateWMSStoreAsync("ws", new WMSStore { Name = "ogc", CapabilitiesURL = "http://r" });

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal(Base, _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("ogc", (string?)body["wmsStore"]?["name"]);
        Assert.Equal("http://r", (string?)body["wmsStore"]?["capabilitiesURL"]);
    }

    [Fact]
    public async Task UpdateWMSStoreAsync_PutsWrapped()
    {
        await _svc.UpdateWMSStoreAsync("ws", "ogc", new WMSStore { Name = "ogc" });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal($"{Base}/ogc", _fake.Last.Path);
        Assert.Equal("ogc", (string?)Json.P(_fake.Last.Body)["wmsStore"]?["name"]);
    }

    [Fact]
    public async Task DeleteWMSStoreAsync_RecurseTernary_KNOWN_ISSUE_E22()
    {
        // KNOWN-ISSUE E22（风格项）：三元式 recurse
        await _svc.DeleteWMSStoreAsync("ws", "ogc");
        Assert.Equal($"{Base}/ogc?recurse=false", _fake.Last!.Path);

        await _svc.DeleteWMSStoreAsync("ws", "ogc", true);
        Assert.Equal("DELETE", _fake.Last.Method);
        Assert.Equal($"{Base}/ogc?recurse=true", _fake.Last.Path);
    }
}

/// <summary>WMSLayerService（级联 WMS 图层）离线单元测试。</summary>
public class WMSLayerServiceTests
{
    private const string Base = "/rest/workspaces/ws/wmsstores/store/wmslayers";
    private readonly RecordingFakeClient _fake = new();
    private readonly WMSLayerService _svc;

    public WMSLayerServiceTests() => _svc = new WMSLayerService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new WMSLayerService(null!));

    [Fact]
    public async Task GetWMSLayersAsync_ParsesWmsLayersWrapper()
    {
        _fake.RespondGet("""{"wmsLayers":{"wmsLayer":[{"name":"n","nativeName":"rn","srs":"EPSG:4326"}]}}""");

        var result = await _svc.GetWMSLayersAsync("ws", "store");

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal($"{Base}.json", _fake.Last.Path);
        Assert.Equal("rn", result[0].NativeName);
    }

    [Fact]
    public async Task GetWMSLayerAsync_SingleUnwrap()
    {
        _fake.RespondGet("""{"wmsLayer":{"name":"n","title":"t","abstract":"a","enabled":false,"store":{"@class":"remoteWMS","name":"store"},"href":"h"}}""");

        var l = await _svc.GetWMSLayerAsync("ws", "store", "n");

        Assert.Equal($"{Base}/n.json", _fake.Last!.Path);
        Assert.Equal("a", l.Abstract);
        Assert.False(l.Enabled);
        Assert.Equal("remoteWMS", l.Store.Class);
    }

    [Fact]
    public async Task CreateWMSLayerAsync_PostsWrapped()
    {
        await _svc.CreateWMSLayerAsync("ws", "store", new WMSLayer { Name = "n" });

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal(Base, _fake.Last.Path);
        Assert.Equal("n", (string?)Json.P(_fake.Last.Body)["wmsLayer"]?["name"]);
    }

    [Fact]
    public async Task UpdateWMSLayerAsync_PutsWrapped()
    {
        await _svc.UpdateWMSLayerAsync("ws", "store", "n", new WMSLayer { Name = "n" });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal($"{Base}/n", _fake.Last.Path);
    }

    [Fact]
    public async Task DeleteWMSLayerAsync_RecurseTernary_KNOWN_ISSUE_E22()
    {
        await _svc.DeleteWMSLayerAsync("ws", "store", "n");
        Assert.Equal($"{Base}/n?recurse=false", _fake.Last!.Path);

        await _svc.DeleteWMSLayerAsync("ws", "store", "n", true);
        Assert.Equal("DELETE", _fake.Last.Method);
        Assert.Equal($"{Base}/n?recurse=true", _fake.Last.Path);
    }
}
