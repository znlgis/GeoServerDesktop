using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>
/// BatchOperationService 离线单元测试（M4）：批量改默认样式/批量启停存储（404 回落 coverageStore）/
/// 批量删除（图层/样式/工作空间，级联参数）的 URL、请求体、响应三元断言 + 部分成功语义。
/// 实测契约基线：PUT 局部更新生效；图层级 enabled 无 REST 通道（启停在存储级）；引用中样式删除 403。
/// </summary>
public class BatchOperationServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly BatchOperationService _svc;

    public BatchOperationServiceTests() => _svc = new BatchOperationService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new BatchOperationService(null!));

    [Fact]
    public async Task SetLayersDefaultStyle_PutsPartialLayerBodyPerQualifiedLayer()
    {
        var result = await _svc.SetLayersDefaultStyleAsync(new[] { "m4:roads", "m4:parcels" }, "blue");

        Assert.True(result.AllSucceeded);
        Assert.Equal(2, result.Total);
        Assert.Equal(2, _fake.Requests.Count);

        var first = _fake.Requests[0];
        Assert.Equal("PUT", first.Method);
        Assert.Equal("/rest/layers/m4%3Aroads", first.Path);
        Assert.Contains("\"defaultStyle\"", first.Body);
        Assert.Contains("\"name\":\"blue\"", first.Body);
        Assert.Contains("\"name\":\"m4:roads\"", first.Body);
        Assert.Equal("application/json; charset=utf-8", first.ContentType);

        Assert.Equal("/rest/layers/m4%3Aparcels", _fake.Requests[1].Path);
    }

    [Fact]
    public async Task SetLayersDefaultStyle_EmptyStyle_Throws() =>
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _svc.SetLayersDefaultStyleAsync(new[] { "a:b" }, ""));

    [Fact]
    public async Task SetLayersDefaultStyle_PartialFailure_KeepsGoingAndSummarizes()
    {
        _fake.EnqueueThrow("PUT", new GeoServerRequestException("boom", 403, "referenced"));

        var result = await _svc.SetLayersDefaultStyleAsync(new[] { "a:b" }, "blue");

        Assert.Single(result.Items);
        Assert.False(result.Items[0].Success);
        Assert.Equal("a:b", result.Items[0].Target);
        Assert.Contains("403", result.Items[0].Message);
        Assert.Contains("referenced", result.FailureSummary);
    }

    [Fact]
    public async Task DeleteLayers_AppendsRecurseFlag()
    {
        await _svc.DeleteLayersAsync(new[] { "a:b" }, recurse: true);

        var r = _fake.Last;
        Assert.Equal("DELETE", r.Method);
        Assert.Equal("/rest/layers/a%3Ab?recurse=true", r.Path);
    }

    [Fact]
    public async Task DeleteLayers_DefaultRecurseFalse()
    {
        await _svc.DeleteLayersAsync(new[] { "a:b" });
        Assert.Equal("/rest/layers/a%3Ab?recurse=false", _fake.Last.Path);
    }

    [Fact]
    public async Task DeleteStyles_MixedGlobalAndWorkspacePaths()
    {
        var targets = new[]
        {
            BatchTarget.GlobalStyle("blue"),
            BatchTarget.WorkspaceStyle("m4", "local"),
        };

        var result = await _svc.DeleteStylesAsync(targets, purge: true);

        Assert.True(result.AllSucceeded);
        Assert.Equal("/rest/styles/blue?purge=true", _fake.Requests[0].Path);
        Assert.Equal("/rest/workspaces/m4/styles/local?purge=true", _fake.Requests[1].Path);
    }

    [Fact]
    public async Task DeleteWorkspaces_RecurseByDefault()
    {
        await _svc.DeleteWorkspacesAsync(new[] { "m4", "m5" });

        Assert.Equal(2, _fake.Requests.Count);
        Assert.Equal("/rest/workspaces/m4?recurse=true", _fake.Requests[0].Path);
        Assert.Equal("/rest/workspaces/m5?recurse=true", _fake.Requests[1].Path);
    }

    [Fact]
    public async Task SetStoresEnabled_DataStoreHit_PutsEnabledFlagOnly()
    {
        var result = await _svc.SetStoresEnabledAsync(
            new[] { new BatchTarget { Workspace = "m4", Name = "ds1" } }, enabled: false);

        Assert.True(result.AllSucceeded);
        var r = _fake.Last;
        Assert.Equal("PUT", r.Method);
        Assert.Equal("/rest/workspaces/m4/datastores/ds1", r.Path);
        Assert.Equal("{\"dataStore\":{\"enabled\":false}}", r.Body);
    }

    [Fact]
    public async Task SetStoresEnabled_QualifiedNameWithoutWorkspace_Parses()
    {
        var result = await _svc.SetStoresEnabledAsync(
            new[] { new BatchTarget { Name = "m4:cs1" } }, enabled: true);

        Assert.True(result.AllSucceeded);
        Assert.Equal("/rest/workspaces/m4/datastores/cs1", _fake.Last.Path);
        Assert.Equal("{\"dataStore\":{\"enabled\":true}}", _fake.Last.Body);
    }

    [Fact]
    public async Task SetStoresEnabled_404FallsBackToCoverageStore()
    {
        _fake.EnqueueThrow("PUT", new GeoServerRequestException("nf", 404, null));

        var result = await _svc.SetStoresEnabledAsync(
            new[] { new BatchTarget { Workspace = "m4", Name = "cs1" } }, enabled: true);

        Assert.True(result.AllSucceeded);
        Assert.Equal(2, _fake.Requests.Count);
        Assert.Equal("/rest/workspaces/m4/datastores/cs1", _fake.Requests[0].Path);
        Assert.Equal("/rest/workspaces/m4/coveragestores/cs1", _fake.Requests[1].Path);
        Assert.Equal("{\"coverageStore\":{\"enabled\":true}}", _fake.Requests[1].Body);
    }

    [Fact]
    public async Task SetStoresEnabled_BothFail_ReportsFailure()
    {
        _fake.EnqueueThrow("PUT", new GeoServerRequestException("nf", 404, null));
        _fake.EnqueueThrow("PUT", new GeoServerRequestException("bad", 500, "server error"));

        var result = await _svc.SetStoresEnabledAsync(
            new[] { new BatchTarget { Workspace = "m4", Name = "x" } }, enabled: true);

        Assert.False(result.AllSucceeded);
        Assert.Equal(1, result.Failed);
        Assert.Contains("HTTP 500", result.Items[0].Message);
    }

    [Fact]
    public async Task SetStoresEnabled_MissingWorkspace_FailsItemWithoutRequest()
    {
        var result = await _svc.SetStoresEnabledAsync(
            new[] { new BatchTarget { Name = "lonely" } }, enabled: true);

        Assert.Equal(1, result.Failed);
        Assert.Empty(_fake.Requests);
        Assert.Contains("工作空间", result.Items[0].Message);
    }

    [Fact]
    public async Task Batch_MixedResults_CountsAndOrder()
    {
        // 动词队列按入队顺序消费：ok1 成功响应 → bad 异常 → ok2 成功响应
        _fake.Enqueue("DELETE", "{}");
        _fake.EnqueueThrow("DELETE", new InvalidOperationException("net down"));
        _fake.Enqueue("DELETE", "{}");

        var result = await _svc.DeleteWorkspacesAsync(new[] { "ok1", "bad", "ok2" }, recurse: true);

        Assert.Equal(3, result.Total);
        Assert.Equal(2, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Equal("ok1", result.Items[0].Target);
        Assert.Equal("bad", result.Items[1].Target);
        Assert.Equal("net down", result.Items[1].Message);
        Assert.Equal("ok2", result.Items[2].Target);
    }

    [Fact]
    public async Task Batch_NullAndEmptyInputs_ProduceEmptyResults()
    {
        Assert.Empty((await _svc.DeleteLayersAsync(null!)).Items);
        Assert.Empty((await _svc.DeleteWorkspacesAsync(null!)).Items);
        Assert.Empty((await _svc.SetStoresEnabledAsync(null!, true)).Items);
        Assert.Empty((await _svc.DeleteStylesAsync(null!, true)).Items);
    }

    [Fact]
    public void BatchTarget_WorkspaceStyleHelpers()
    {
        var t = BatchTarget.WorkspaceStyle("m4", "local");
        Assert.True(t.IsWorkspaceStyle);
        Assert.Equal("local", t.StyleName);
        Assert.Equal("m4:workspace/local", t.QualifiedName);

        var g = BatchTarget.GlobalStyle("blue");
        Assert.False(g.IsWorkspaceStyle);
        Assert.Equal("blue", g.StyleName);
    }
}
