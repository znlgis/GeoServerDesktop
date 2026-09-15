using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>LayerGroupService 离线单元测试（全局 + 工作空间 10 个方法）。删除无 recurse（清单 E25）。</summary>
public class LayerGroupServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly LayerGroupService _svc;

    public LayerGroupServiceTests() => _svc = new LayerGroupService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new LayerGroupService(null!));

    [Fact]
    public async Task GetLayerGroupsAsync_ParsesLayerGroupsWrapper()
    {
        _fake.RespondGet("""{"layerGroups":{"layerGroup":[{"name":"g1","mode":"LAYER"},{"name":"g2","mode":"TITLE"}]}}""");

        var result = await _svc.GetLayerGroupsAsync();

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/layergroups.json", _fake.Last.Path);
        Assert.Equal(2, result.Length);
        Assert.Equal("TITLE", result[1].Mode);
    }

    [Fact]
    public async Task GetLayerGroupAsync_ParsesPublishablesPublishedAndAbstractTxt()
    {
        // LayerGroup 摘要键为 "abstractTxt"（与 GeoServer LayerInfo JSON 一致，清单 E28 非缺陷项）
        _fake.RespondGet("""{"layerGroup":{"name":"sli","mode":"SINGLE","title":"t","abstractTxt":"a","workspace":{"name":"ws"},"publishables":{"published":[{"@type":"vector","name":"topp:states","href":"h1"},{"@type":"raster","name":"cov","href":"h2"}]},"href":"h"}}""");

        var lg = await _svc.GetLayerGroupAsync("sli");

        Assert.Equal("/rest/layergroups/sli.json", _fake.Last!.Path);
        Assert.Equal("a", lg.Abstract);
        Assert.Equal(2, lg.Publishables.Published.Length);
        Assert.Equal("vector", lg.Publishables.Published[0].Type); // @type
        Assert.Equal("raster", lg.Publishables.Published[1].Type);
        Assert.Equal("topp:states", lg.Publishables.Published[0].Name);
    }

    [Fact]
    public async Task CreateLayerGroupAsync_PostsWrappedLayerGroup()
    {
        await _svc.CreateLayerGroupAsync(new LayerGroup { Name = "g", Mode = "LAYER" });

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/layergroups", _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("g", (string?)body["layerGroup"]?["name"]);
        Assert.Equal("LAYER", (string?)body["layerGroup"]?["mode"]);
    }

    [Fact]
    public async Task UpdateLayerGroupAsync_PutsWrapped()
    {
        await _svc.UpdateLayerGroupAsync("g", new LayerGroup { Name = "g" });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/layergroups/g", _fake.Last.Path);
        Assert.Equal("g", (string?)Json.P(_fake.Last.Body)["layerGroup"]?["name"]);
    }

    [Fact]
    public async Task DeleteLayerGroupAsync_NoRecurse_KNOWN_ISSUE_E25()
    {
        // KNOWN-ISSUE E25：GeoServer 支持 recurse 但本方法不带（缺功能），固化现状
        await _svc.DeleteLayerGroupAsync("g");

        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/rest/layergroups/g", _fake.Last.Path);
    }

    [Fact]
    public async Task GetWorkspaceLayerGroupsAsync_ScopedPath()
    {
        _fake.RespondGet("""{"layerGroups":{"layerGroup":[{"name":"g"}]}}""");
        var result = await _svc.GetWorkspaceLayerGroupsAsync("ws");

        Assert.Equal("/rest/workspaces/ws/layergroups.json", _fake.Last!.Path);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetWorkspaceLayerGroupAsync_ScopedPath()
    {
        _fake.RespondGet("""{"layerGroup":{"name":"g","href":"h"}}""");
        var lg = await _svc.GetWorkspaceLayerGroupAsync("ws", "g");

        Assert.Equal("/rest/workspaces/ws/layergroups/g.json", _fake.Last!.Path);
        Assert.Equal("h", lg.Href);
    }

    [Fact]
    public async Task CreateWorkspaceLayerGroupAsync_PostsScoped()
    {
        await _svc.CreateWorkspaceLayerGroupAsync("ws", new LayerGroup { Name = "g" });

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/workspaces/ws/layergroups", _fake.Last.Path);
        Assert.Equal("g", (string?)Json.P(_fake.Last.Body)["layerGroup"]?["name"]);
    }

    [Fact]
    public async Task UpdateWorkspaceLayerGroupAsync_PutsScoped()
    {
        await _svc.UpdateWorkspaceLayerGroupAsync("ws", "g", new LayerGroup { Name = "g" });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/workspaces/ws/layergroups/g", _fake.Last.Path);
    }

    [Fact]
    public async Task DeleteWorkspaceLayerGroupAsync_DeletesScoped()
    {
        await _svc.DeleteWorkspaceLayerGroupAsync("ws", "g");

        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/rest/workspaces/ws/layergroups/g", _fake.Last.Path);
    }
}
