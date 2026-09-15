using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>LayerService 离线单元测试。GeoServer 不允许直接 POST 创建 layer，故无 Create 方法（符合官方，非缺陷）。</summary>
public class LayerServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly LayerService _svc;

    public LayerServiceTests() => _svc = new LayerService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new LayerService(null!));

    [Fact]
    public async Task GetLayersAsync_ParsesLayersWrapper()
    {
        _fake.RespondGet("""{"layers":{"layer":[{"name":"topp:states","type":"VECTOR"},{"name":"r","type":"RASTER"}]}}""");

        var result = await _svc.GetLayersAsync();

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/layers.json", _fake.Last.Path);
        Assert.Equal(2, result.Length);
        Assert.Equal("VECTOR", result[0].Type);
    }

    [Fact]
    public async Task GetLayerAsync_ParsesResourceAtClassAndDefaultStyle()
    {
        // Layer.resource.@class 区分 featureType/coverage 等资源类型
        _fake.RespondGet("""{"layer":{"name":"states","type":"VECTOR","defaultStyle":{"name":"polygon","href":"dh"},"resource":{"@class":"featureType","name":"states","href":"rh"},"href":"h"}}""");

        var layer = await _svc.GetLayerAsync("states");

        Assert.Equal("/rest/layers/states.json", _fake.Last!.Path);
        Assert.Equal("polygon", layer.DefaultStyle.Name);
        Assert.Equal("featureType", layer.Resource.Class);
        Assert.Equal("rh", layer.Resource.Href);
    }

    [Fact]
    public async Task UpdateLayerAsync_PutsWrappedLayer()
    {
        await _svc.UpdateLayerAsync("states", new Layer { Name = "states", Type = "VECTOR" });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/layers/states", _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("states", (string?)body["layer"]?["name"]);
        Assert.Equal("VECTOR", (string?)body["layer"]?["type"]);
    }

    [Fact]
    public async Task DeleteLayerAsync_RecurseToLowerInvariant()
    {
        await _svc.DeleteLayerAsync("states");
        Assert.Equal("/rest/layers/states?recurse=false", _fake.Last!.Path);

        await _svc.DeleteLayerAsync("states", true);
        Assert.Equal("DELETE", _fake.Last.Method);
        Assert.Equal("/rest/layers/states?recurse=true", _fake.Last.Path);
    }

    [Fact]
    public async Task GetWorkspaceLayersAsync_UsesWorkspaceScopedPath()
    {
        _fake.RespondGet("""{"layers":{"layer":[{"name":"a"}]}}""");

        var result = await _svc.GetWorkspaceLayersAsync("ws");

        Assert.Equal("/rest/workspaces/ws/layers.json", _fake.Last!.Path);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetWorkspaceLayerAsync_UsesWorkspaceScopedPath()
    {
        _fake.RespondGet("""{"layer":{"name":"a","href":"h"}}""");

        var layer = await _svc.GetWorkspaceLayerAsync("ws", "a");

        Assert.Equal("/rest/workspaces/ws/layers/a.json", _fake.Last!.Path);
        Assert.Equal("h", layer.Href);
    }
}
