using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>CoverageStoreService 离线单元测试。recurse 用三元式 "true"/"false"（清单 E22 风格差异，结果与 ToLowerInvariant 相同）。</summary>
public class CoverageStoreServiceTests
{
    private const string Base = "/rest/workspaces/ws/coveragestores";
    private readonly RecordingFakeClient _fake = new();
    private readonly CoverageStoreService _svc;

    public CoverageStoreServiceTests() => _svc = new CoverageStoreService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new CoverageStoreService(null!));

    [Fact]
    public async Task GetCoverageStoresAsync_ParsesCoverageStoresWrapper()
    {
        _fake.RespondGet("""{"coverageStores":{"coverageStore":[{"name":"srtm","type":"GeoTIFF","enabled":true,"url":"file:data/srtm.tif","workspace":{"name":"ws"}}]}}""");

        var result = await _svc.GetCoverageStoresAsync("ws");

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal($"{Base}.json", _fake.Last.Path);
        Assert.Single(result);
        Assert.Equal("srtm", result[0].Name);
        Assert.Equal("GeoTIFF", result[0].Type);
        Assert.Equal("file:data/srtm.tif", result[0].Url);
    }

    [Fact]
    public async Task GetCoverageStoreAsync_SingleUnwrap()
    {
        _fake.RespondGet("""{"coverageStore":{"name":"srtm","description":"d","href":"h"}}""");

        var cs = await _svc.GetCoverageStoreAsync("ws", "srtm");

        Assert.Equal($"{Base}/srtm.json", _fake.Last!.Path);
        Assert.Equal("d", cs.Description);
    }

    [Fact]
    public async Task CreateCoverageStoreAsync_PostsWrappedCamelRoot()
    {
        await _svc.CreateCoverageStoreAsync("ws", new CoverageStore { Name = "srtm", Type = "GeoTIFF", Url = "file:u" });

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal(Base, _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("srtm", (string?)body["coverageStore"]?["name"]);
        Assert.Equal("GeoTIFF", (string?)body["coverageStore"]?["type"]);
    }

    [Fact]
    public async Task UpdateCoverageStoreAsync_PutsWrapped()
    {
        await _svc.UpdateCoverageStoreAsync("ws", "srtm", new CoverageStore { Name = "srtm" });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal($"{Base}/srtm", _fake.Last.Path);
        Assert.Equal("srtm", (string?)Json.P(_fake.Last.Body)["coverageStore"]?["name"]);
    }

    [Fact]
    public async Task DeleteCoverageStoreAsync_RecurseTernary_KNOWN_ISSUE_E22()
    {
        // KNOWN-ISSUE E22（风格项）：此处 recurse 用三元式而非 ToLowerInvariant，序列化结果一致，固化现状
        await _svc.DeleteCoverageStoreAsync("ws", "srtm");
        Assert.Equal($"{Base}/srtm?recurse=false", _fake.Last!.Path);

        await _svc.DeleteCoverageStoreAsync("ws", "srtm", true);
        Assert.Equal("DELETE", _fake.Last.Method);
        Assert.Equal($"{Base}/srtm?recurse=true", _fake.Last.Path);
    }

    [Fact]
    public async Task UploadCoverageFileAsync_PutsRawBytesFileExtensionOctetStream()
    {
        var bytes = new byte[] { 5, 6, 7 };
        await _svc.UploadCoverageFileAsync("ws", "srtm", bytes, "geotiff");

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal($"{Base}/srtm/file.geotiff", _fake.Last.Path);
        Assert.Equal("application/octet-stream", _fake.Last.ContentType);
        Assert.Equal(bytes, _fake.Last.RawBody);
    }
}
