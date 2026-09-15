using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>FeatureTypeService 离线单元测试。清单确认：全仓库无 nativebbox 查询参数。</summary>
public class FeatureTypeServiceTests
{
    private const string Base = "/rest/workspaces/ws/datastores/store";
    private readonly RecordingFakeClient _fake = new();
    private readonly FeatureTypeService _svc;

    public FeatureTypeServiceTests() => _svc = new FeatureTypeService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new FeatureTypeService(null!));

    [Fact]
    public async Task GetFeatureTypesAsync_ParsesFeatureTypesWrapper()
    {
        _fake.RespondGet("""{"featureTypes":{"featureType":[{"name":"states","nativeName":"states","srs":"EPSG:4326","enabled":true}]}}""");

        var result = await _svc.GetFeatureTypesAsync("ws", "store");

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal($"{Base}/featuretypes.json", _fake.Last.Path);
        Assert.Single(result);
        Assert.Equal("states", result[0].Name);
        Assert.Equal("EPSG:4326", result[0].Srs);
    }

    [Fact]
    public async Task GetFeatureTypeAsync_ParsesRichRealWorldResponse()
    {
        // 真实 GeoServer 形态：keywords.string、store.@class、小写 minx/maxx/miny/maxy
        _fake.RespondGet("""
        {"featureType":{"name":"states","nativeName":"states",
         "namespace":{"name":"topp","href":"nh"},
         "title":"States","abstract":"the states","keywords":{"key":["a","b"],"string":["a","b"]},
         "nativeCRS":"EPSG:4326","srs":"EPSG:4326",
         "nativeBoundingBox":{"minx":-124.7,"maxx":-66.9,"miny":24.9,"maxy":49.4,"crs":"EPSG:4326"},
         "latLonBoundingBox":{"minx":-180,"maxx":180,"miny":-90,"maxy":90,"crs":"EPSG:4326"},
         "enabled":true,
         "store":{"@class":"dataStore","name":"states","href":"sh"},
         "href":"fh"}}
        """);

        var ft = await _svc.GetFeatureTypeAsync("ws", "store", "states");

        Assert.Equal($"{Base}/featuretypes/states.json", _fake.Last!.Path);
        Assert.Equal("the states", ft.Abstract); // JSON 键 "abstract"
        Assert.Equal(new[] { "a", "b" }, ft.Keywords.Keywords);
        Assert.Equal(-124.7, ft.NativeBoundingBox.MinX);
        Assert.Equal(49.4, ft.NativeBoundingBox.MaxY);
        Assert.Equal("dataStore", ft.Store.Class); // @class
        Assert.Equal("states", ft.Store.Name);
        Assert.True(ft.Enabled);
        Assert.Equal("nh", ft.Namespace.Href);
    }

    [Fact]
    public async Task CreateFeatureTypeAsync_PostsWrappedFeatureType_NoNativeBboxQuery()
    {
        var ft = new FeatureType { Name = "roads", NativeName = "roads" };
        await _svc.CreateFeatureTypeAsync("ws", "store", ft);

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal($"{Base}/featuretypes", _fake.Last.Path); // 不带 .json 也不带 nativebbox
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("roads", (string?)body["featureType"]?["name"]);
        Assert.Null(body["nativebbox"]);
    }

    [Fact]
    public async Task CreateFeatureTypeAsync_DefaultEnabledSerializedAsTrue_FIXED_E27()
    {
        // FIXED-E27：FeatureType.Enabled 现为 bool? 且构造函数默认 true（其余资源多为 bool?）。
        // 通过本客户端直接创建要素类型时，即使不显式赋值，请求体也带 "enabled":true，
        // 新建图层默认可用——原"直调创建默认禁用"缺陷已修复。
        var ft = new FeatureType { Name = "roads" };
        Assert.True(ft.Enabled); // 构造函数默认 true

        await _svc.CreateFeatureTypeAsync("ws", "store", ft);

        var body = Json.P(_fake.Last!.Body);
        var enabled = body["featureType"]!["enabled"];
        Assert.NotNull(enabled);
        Assert.True((bool)enabled);

        // 显式 false 仍可发布禁用要素类型
        await _svc.CreateFeatureTypeAsync("ws", "store", new FeatureType { Name = "roads2", Enabled = false });
        Assert.False((bool)Json.P(_fake.Last.Body)["featureType"]!["enabled"]!);
    }

    [Fact]
    public async Task UpdateFeatureTypeAsync_PutsWrappedUnderName()
    {
        await _svc.UpdateFeatureTypeAsync("ws", "store", "roads", new FeatureType { Name = "roads", Enabled = true });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal($"{Base}/featuretypes/roads", _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.True((bool)body["featureType"]!["enabled"]!);
    }

    [Fact]
    public async Task DeleteFeatureTypeAsync_RecurseToLowerInvariant()
    {
        await _svc.DeleteFeatureTypeAsync("ws", "store", "roads");
        Assert.Equal($"{Base}/featuretypes/roads?recurse=false", _fake.Last!.Path);

        await _svc.DeleteFeatureTypeAsync("ws", "store", "roads", true);
        Assert.Equal("DELETE", _fake.Last.Method);
        Assert.Equal($"{Base}/featuretypes/roads?recurse=true", _fake.Last.Path);
    }
}
