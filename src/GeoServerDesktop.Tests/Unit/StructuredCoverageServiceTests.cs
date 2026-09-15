using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>StructuredCoverageService 离线单元测试。含 E5（index 未包装）、E21（harvest files 为字符串数组而非对象数组）、E24（filter 未转义）。</summary>
public class StructuredCoverageServiceTests
{
    private const string Base = "/rest/workspaces/ws/coveragestores/store/coverages/cov";
    private readonly RecordingFakeClient _fake = new();
    private readonly StructuredCoverageService _svc;

    public StructuredCoverageServiceTests() => _svc = new StructuredCoverageService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new StructuredCoverageService(null!));

    [Fact]
    public async Task GetIndexAsync_FlatDeserialization_KNOWN_ISSUE_E5()
    {
        // GeoServer SC index 实际根为 "index"；模型直接反序列化扁平对象。
        // 固化两种形态的当前行为：扁平可解析、包装后字段为 null。
        _fake.RespondGet("""{"name":"idx","schema":{"time":"datetime"}}""");
        var idx = await _svc.GetIndexAsync("ws", "store", "cov");

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal($"{Base}/index.json", _fake.Last.Path);
        Assert.Equal("idx", idx.Name);
        Assert.Equal("datetime", (string?)idx.Schema["time"]);

        _fake.RespondGet("""{"index":{"name":"idx"}}""");
        var wrapped = await _svc.GetIndexAsync("ws", "store", "cov");
        Assert.Null(wrapped.Name); // E5：包装形态下静默丢数据
    }

    [Fact]
    public async Task UpdateIndexAsync_PutsUnwrappedBody_KNOWN_ISSUE_E5()
    {
        await _svc.UpdateIndexAsync("ws", "store", "cov", new StructuredCoverageIndex { Name = "idx" });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal($"{Base}/index", _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("idx", (string?)body["name"]); // 无 "index" 包装根（E5）
    }

    [Fact]
    public async Task GetGranulesAsync_NoOptionalParamsProduceNoQueryString()
    {
        _fake.RespondGet("""{"type":"FeatureCollection","features":[{"id":"g1","fid":"granule.1","properties":{"name":"a.tif"}}],"totalFeatures":1}""");

        var w = await _svc.GetGranulesAsync("ws", "store", "cov");

        Assert.Equal($"{Base}/index/granules.json", _fake.Last!.Path);
        Assert.Single(w.Granules);
        Assert.Equal("g1", w.Granules[0].Id);
        Assert.Equal("granule.1", w.Granules[0].Fid);
        Assert.Equal("a.tif", (string?)w.Granules[0].Properties["name"]);
        Assert.Equal(1, w.TotalFeatures);
    }

    [Fact]
    public async Task GetGranulesAsync_QueriesAppendedInFixedOrder()
    {
        _fake.RespondGet("""{"features":[]}""");
        await _svc.GetGranulesAsync("ws", "store", "cov", filter: "x=1", offset: 10, limit: 20);

        // FIXED-E24：filter 值经 Uri.EscapeDataString（x=1 -> x%3D1）
        Assert.Equal($"{Base}/index/granules.json?filter=x%3D1&offset=10&limit=20", _fake.Last!.Path);
    }

    [Fact]
    public async Task GetGranulesAsync_FilterUrlEscaped_FIXED_E24()
    {
        // FIXED-E24：filter 为 CQL（含空格/&/>/中文），现经 Uri.EscapeDataString 转义后拼接
        _fake.RespondGet("""{"features":[]}""");
        await _svc.GetGranulesAsync("ws", "store", "cov", filter: "name = '中国' & t>0");

        Assert.Equal($"{Base}/index/granules.json?filter=name%20%3D%20%27%E4%B8%AD%E5%9B%BD%27%20%26%20t%3E0", _fake.Last!.Path);
    }

    [Fact]
    public async Task GetGranuleAsync_FlatDeserialization()
    {
        // GeoServer 实际返回 Feature 对象（type/id/geometry/properties），模型为扁平 Granule：
        // id/fid/properties 可解析（E5 同族的形态差异，此处按源码现状固化）
        _fake.RespondGet("""{"id":"g1","type":"Feature","properties":{"location":"file:/x.nc"}}""");

        var g = await _svc.GetGranuleAsync("ws", "store", "cov", "g1");

        Assert.Equal($"{Base}/index/granules/g1.json", _fake.Last!.Path);
        Assert.Equal("g1", g.Id);
        Assert.Equal("file:/x.nc", (string?)g.Properties["location"]);
    }

    [Fact]
    public async Task DeleteGranuleAsync_DeletesNoJsonSuffix()
    {
        await _svc.DeleteGranuleAsync("ws", "store", "cov", "g1");

        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal($"{Base}/index/granules/g1", _fake.Last.Path);
    }

    [Fact]
    public async Task HarvestGranulesAsync_PostsStringArrayFiles_KNOWN_ISSUE_E21()
    {
        // KNOWN-ISSUE E21：GeoServer SC harvest 期望 {"files":[{"file":"..."}]} 对象数组，
        // 当前发送 {"files":["a","b"]} 字符串数组。固化当前请求体。
        await _svc.HarvestGranulesAsync("ws", "store", "cov", new[] { "a.nc", "b.nc" });

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal($"{Base}/index/granules", _fake.Last.Path);
        var files = (Newtonsoft.Json.Linq.JArray)Json.P(_fake.Last.Body)["files"]!;
        Assert.Equal(2, files.Count);
        Assert.Equal("a.nc", (string?)files[0]); // 字符串数组（期望为对象数组）
    }
}
