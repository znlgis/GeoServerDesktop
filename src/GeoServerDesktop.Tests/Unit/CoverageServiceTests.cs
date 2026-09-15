using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>CoverageService 离线单元测试。</summary>
public class CoverageServiceTests
{
    private const string Base = "/rest/workspaces/ws/coveragestores/store/coverages";
    private readonly RecordingFakeClient _fake = new();
    private readonly CoverageService _svc;

    public CoverageServiceTests() => _svc = new CoverageService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new CoverageService(null!));

    [Fact]
    public async Task GetCoveragesAsync_ParsesCoveragesWrapper()
    {
        _fake.RespondGet("""{"coverages":{"coverage":[{"name":"world","nativeName":"world","srs":"EPSG:4326"}]}}""");

        var result = await _svc.GetCoveragesAsync("ws", "store");

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal($"{Base}.json", _fake.Last.Path);
        Assert.Single(result);
        Assert.Equal("world", result[0].Name);
        Assert.Equal("EPSG:4326", result[0].Srs);
    }

    [Fact]
    public async Task GetCoverageAsync_ParsesSingleWithAbstractAndStoreClass()
    {
        _fake.RespondGet("""{"coverage":{"name":"world","abstract":"abs","enabled":true,"store":{"@class":"coverageStore","name":"store"},"nativeBoundingBox":{"minx":0,"maxx":10,"miny":0,"maxy":10,"crs":"EPSG:4326"}}}""");

        var cov = await _svc.GetCoverageAsync("ws", "store", "world");

        Assert.Equal($"{Base}/world.json", _fake.Last!.Path);
        Assert.Equal("abs", cov.Abstract);
        Assert.True(cov.Enabled); // Coverage.Enabled 为 bool?（与 FeatureType 的非空 bool 不同）
        Assert.Equal("coverageStore", cov.Store.Class);
        Assert.Equal(10, cov.NativeBoundingBox.MaxX);
    }

    [Fact]
    public async Task CreateCoverageAsync_PostsWrappedCoverage()
    {
        await _svc.CreateCoverageAsync("ws", "store", new Coverage { Name = "world" });

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal(Base, _fake.Last.Path);
        Assert.Equal("world", (string?)Json.P(_fake.Last.Body)["coverage"]?["name"]);
    }

    [Fact]
    public async Task UpdateCoverageAsync_PutsWrapped()
    {
        await _svc.UpdateCoverageAsync("ws", "store", "world", new Coverage { Name = "world", Title = "t" });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal($"{Base}/world", _fake.Last.Path);
        Assert.Equal("t", (string?)Json.P(_fake.Last.Body)["coverage"]?["title"]);
    }

    [Fact]
    public async Task DeleteCoverageAsync_RecurseTernary_KNOWN_ISSUE_E22()
    {
        // KNOWN-ISSUE E22（风格项）：三元式 recurse
        await _svc.DeleteCoverageAsync("ws", "store", "world");
        Assert.Equal($"{Base}/world?recurse=false", _fake.Last!.Path);

        await _svc.DeleteCoverageAsync("ws", "store", "world", true);
        Assert.Equal("DELETE", _fake.Last.Method);
        Assert.Equal($"{Base}/world?recurse=true", _fake.Last.Path);
    }
}
