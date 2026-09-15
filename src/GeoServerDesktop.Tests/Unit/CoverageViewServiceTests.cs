using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>CoverageViewService 离线单元测试。删除无 recurse（清单 E25：GeoServer 支持但客户端缺参数，缺功能而非错误）。</summary>
public class CoverageViewServiceTests
{
    private const string Base = "/rest/workspaces/ws/coverageviews";
    private readonly RecordingFakeClient _fake = new();
    private readonly CoverageViewService _svc;

    public CoverageViewServiceTests() => _svc = new CoverageViewService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new CoverageViewService(null!));

    [Fact]
    public async Task GetCoverageViewsAsync_FlatStringListShape_CurrentBehavior()
    {
        // 当前模型期望 {"coverageViews": ["a","b"]}（List<string> 扁平形态）
        _fake.RespondGet("""{"coverageViews":["ndvi","evi"]}""");

        var w = await _svc.GetCoverageViewsAsync("ws");

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal($"{Base}.json", _fake.Last.Path);
        Assert.Equal(new[] { "ndvi", "evi" }, w.CoverageViews);
    }

    [Fact]
    public async Task GetCoverageViewsAsync_RealGeoServerDoubleWrapped_Throws_KNOWN_ISSUE_E7()
    {
        // KNOWN-ISSUE E7：GeoServer 实际返回 {"coverageViews":{"coverageView":[...]}} 双层结构，
        // 与模型 List<string> 不符，反序列化抛 JsonException。固化当前抛错行为作为回归基线。
        _fake.RespondGet("""{"coverageViews":{"coverageView":[{"name":"ndvi"}]}}""");

        await Assert.ThrowsAnyAsync<Newtonsoft.Json.JsonException>(() => _svc.GetCoverageViewsAsync("ws"));
    }

    [Fact]
    public async Task GetCoverageViewAsync_ParsesBands()
    {
        _fake.RespondGet("""{"coverageView":{"name":"ndvi","coverageBands":[{"definition":"ndvi","index":1,"compositionType":"COLOR_INDEX","inputCoverageBands":[{"coverageName":"world","band":0}]}]}}""");

        var w = await _svc.GetCoverageViewAsync("ws", "ndvi");

        Assert.Equal($"{Base}/ndvi.json", _fake.Last!.Path);
        Assert.Equal("ndvi", w.CoverageView.Name);
        Assert.Equal("ndvi", w.CoverageView.CoverageBands[0].Definition);
        Assert.Equal(1, w.CoverageView.CoverageBands[0].Index);
        Assert.Equal("world", w.CoverageView.CoverageBands[0].InputCoverageBands[0].CoverageName);
    }

    [Fact]
    public async Task CreateCoverageViewAsync_PostsWrappedCoverageView()
    {
        await _svc.CreateCoverageViewAsync("ws", new CoverageView { Name = "ndvi" });

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal(Base, _fake.Last.Path);
        Assert.Equal("ndvi", (string?)Json.P(_fake.Last.Body)["coverageView"]?["name"]);
    }

    [Fact]
    public async Task UpdateCoverageViewAsync_PutsWrapped()
    {
        await _svc.UpdateCoverageViewAsync("ws", "ndvi", new CoverageView { Name = "ndvi" });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal($"{Base}/ndvi", _fake.Last.Path);
        Assert.Equal("ndvi", (string?)Json.P(_fake.Last.Body)["coverageView"]?["name"]);
    }

    [Fact]
    public async Task DeleteCoverageViewAsync_NoRecurse_KNOWN_ISSUE_E25()
    {
        // KNOWN-ISSUE E25：GeoServer 支持 recurse 但本客户端删除无该参数（缺功能），固化现状路径
        await _svc.DeleteCoverageViewAsync("ws", "ndvi");

        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal($"{Base}/ndvi", _fake.Last.Path);
    }
}
