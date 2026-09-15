using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>GridsetService (GWC) 离线单元测试。FIXED-E8：列表为顶层裸数组；FIXED-E9：单体根 {"gridSet":{...}}
/// （大写 S）；FIXED-E10：创建为 PUT /gwc/rest/gridsets/{name} + XStream XML（JSON 体实测 500 "Duplicate field coords"，
/// XML 实测 201）。无 Update 方法。</summary>
public class GridsetServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly GridsetService _svc;

    public GridsetServiceTests() => _svc = new GridsetService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new GridsetService(null!));

    [Fact]
    public async Task GetGridsetsAsync_RawJsonArray_FIXED_E8()
    {
        _fake.RespondGet("""["EPSG:4326","EPSG:900913","GlobalCRS84Pixel"]""");
        var w = await _svc.GetGridsetsAsync();
        Assert.Equal("/gwc/rest/gridsets.json", _fake.Last!.Path);
        Assert.Equal(3, w.GridSets.Count);
    }

    [Fact]
    public async Task GetGridsetAsync_ParsesCapitalSGridSetRoot_FIXED_E9()
    {
        _fake.RespondGet("""{"gridSet":{"name":"Custom","srs":{"number":4326},"extent":{"coords":[-180.0,-90.0,180.0,90.0]},"alignTopLeft":false,"resolutions":[0.703125,0.3515625],"tileHeight":256,"tileWidth":256,"yCoordinateFirst":false}}""");

        var g = await _svc.GetGridsetAsync("Custom");

        Assert.Equal("/gwc/rest/gridsets/Custom.json", _fake.Last!.Path);
        Assert.Equal("Custom", g.Name);
        Assert.Equal(4326, g.SRS.Number);
        Assert.Equal(new[] { -180.0, -90.0, 180.0, 90.0 }, g.Extent.Coords);
        Assert.Equal(2, g.Resolutions.Count);
        Assert.Equal(256, g.TileWidth);
    }

    [Fact]
    public async Task CreateGridsetAsync_PutsXmlToNamedRoute_FIXED_E10()
    {
        await _svc.CreateGridsetAsync(new Gridset
        {
            Name = "Custom",
            SRS = new SRS { Number = 4326 },
            Extent = new Extent { Coords = new[] { -180.0, -90.0, 180.0, 90.0 } },
            Resolutions = new System.Collections.Generic.List<double> { 0.703125 },
            TileWidth = 256,
            TileHeight = 256
        });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/gwc/rest/gridsets/Custom", _fake.Last.Path);
        Assert.Contains("application/xml", _fake.Last.ContentType);
        var body = _fake.Last.Body;
        Assert.Contains("<gridSet>", body);
        Assert.Contains("<name>Custom</name>", body);
        Assert.Contains("<srs><number>4326</number></srs>", body);
        Assert.Contains("<double>-180</double>", body);
        Assert.Contains("<resolutions>", body);
    }

    [Fact]
    public async Task DeleteGridsetAsync_DeletesByName()
    {
        await _svc.DeleteGridsetAsync("Custom");

        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/gwc/rest/gridsets/Custom", _fake.Last.Path);
    }

    [Fact]
    public void NoUpdateMethod_CurrentSurfacePin()
    {
        // 清单：GridsetService 无 Update 方法（缺功能）——GWC 的 PUT /{name} 本身即 upsert，Create 已承接
        Assert.Null(typeof(GridsetService).GetMethod("UpdateGridsetAsync"));
    }
}
