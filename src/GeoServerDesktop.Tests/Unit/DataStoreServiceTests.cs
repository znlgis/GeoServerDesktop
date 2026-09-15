using System;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>DataStoreService 离线单元测试：含上传原始字节与 reset 空 body（清单 E26 注意点）。</summary>
public class DataStoreServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly DataStoreService _svc;

    public DataStoreServiceTests() => _svc = new DataStoreService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new DataStoreService(null!));

    [Fact]
    public async Task GetDataStoresAsync_ParsesDataStoresWrapper()
    {
        _fake.RespondGet("""{"dataStores":{"dataStore":[{"name":"shapes","type":"Shapefile","enabled":true,"workspace":{"name":"ne","href":"h"},"href":"h2"}]}}""");

        var result = await _svc.GetDataStoresAsync("ne");

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/workspaces/ne/datastores.json", _fake.Last.Path);
        Assert.Single(result);
        Assert.Equal("shapes", result[0].Name);
        Assert.Equal("Shapefile", result[0].Type);
        Assert.True(result[0].Enabled);
        Assert.Equal("ne", result[0].Workspace.Name);
    }

    [Fact]
    public async Task GetDataStoreAsync_ParsesConnectionParametersEntryKeyDollarShape()
    {
        // 连接参数为 XML 风格 JSON：connectionParameters.entry[] 每项 {"@key":..,"$":..}
        _fake.RespondGet("""{"dataStore":{"name":"pg","type":"PostGIS","connectionParameters":{"entry":[{"@key":"host","$":"localhost"},{"@key":"port","$":"5432"}]},"href":"h"}}""");

        var ds = await _svc.GetDataStoreAsync("ws", "pg");

        Assert.Equal("/rest/workspaces/ws/datastores/pg.json", _fake.Last!.Path);
        Assert.Equal(2, ds.ConnectionParameters.Entries.Length);
        Assert.Equal("host", ds.ConnectionParameters.Entries[0].Key);
        Assert.Equal("localhost", ds.ConnectionParameters.Entries[0].Value);
        Assert.Equal("5432", ds.ConnectionParameters.Entries[1].Value);
    }

    [Fact]
    public async Task CreateDataStoreAsync_PostsWrappedDataStoreCamelKey()
    {
        // 包装根为驼峰 "dataStore"（与 GeoServer 一致）
        var ds = new DataStore { Name = "shapes", Type = "Shapefile" };
        await _svc.CreateDataStoreAsync("ws", ds);

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/workspaces/ws/datastores", _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("shapes", (string?)body["dataStore"]?["name"]);
        Assert.Equal("Shapefile", (string?)body["dataStore"]?["type"]);
    }

    [Fact]
    public async Task UpdateDataStoreAsync_PutsWrappedDataStore()
    {
        await _svc.UpdateDataStoreAsync("ws", "old", new DataStore { Name = "old" });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/workspaces/ws/datastores/old", _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("old", (string?)body["dataStore"]?["name"]);
    }

    [Fact]
    public async Task DeleteDataStoreAsync_RecurseToLowerInvariant()
    {
        await _svc.DeleteDataStoreAsync("ws", "shapes");
        Assert.Equal("/rest/workspaces/ws/datastores/shapes?recurse=false", _fake.Last!.Path);

        await _svc.DeleteDataStoreAsync("ws", "shapes", true);
        Assert.Equal("DELETE", _fake.Last.Method);
        Assert.Equal("/rest/workspaces/ws/datastores/shapes?recurse=true", _fake.Last.Path);
    }

    [Fact]
    public async Task ResetDataStoreAsync_PutsWithNullContent_FIXED_E26()
    {
        // FIXED-E26（判为误报）：实测 3.0.1 PUT .../datastores/{ds}/reset 对空体（无 Content-Type）、
        // 空 application/json、空 text/plain 三种形态一律 200（curl 全形态复测 + DataStoreShapefileIT
        // ResetDataStore_Returns200 固化）。保持 content=null 现状，无 415/400，不需要补 JSON 头。
        await _svc.ResetDataStoreAsync("ws", "shapes");

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/workspaces/ws/datastores/shapes/reset", _fake.Last.Path);
        Assert.Null(_fake.Last.Body);
        Assert.Null(_fake.Last.ContentType);
    }

    [Fact]
    public async Task UploadFileAsync_PutsRawBytesWithFileFormatInPathAndCustomContentType()
    {
        var bytes = new byte[] { 1, 2, 3, 250, 251 };
        await _svc.UploadFileAsync("ws", "shp", "zip", bytes, "application/zip");

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/workspaces/ws/datastores/shp/file.zip", _fake.Last.Path);
        Assert.Equal("application/zip", _fake.Last.ContentType);
        Assert.Equal(bytes, _fake.Last.RawBody);
    }

    [Fact]
    public async Task Paths_EscapeResourceNameSegments()
    {
        // FIXED（资源名安全）：workspace/dataStore 等 name 段统一转义（GET/PUT/DELETE 全路径形态不变于普通名）
        await _svc.GetDataStoreAsync("ws 1", "my store");
        Assert.Equal("/rest/workspaces/ws%201/datastores/my%20store.json", _fake.Last!.Path);

        await _svc.CreateDataStoreAsync("ws", new DataStore { Name = "s" });
        Assert.Equal("/rest/workspaces/ws/datastores", _fake.Last!.Path); // 普通名不产生多余转义
    }

    [Fact]
    public async Task UploadFileAsync_DefaultContentTypeIsOctetStream()
    {
        await _svc.UploadFileAsync("ws", "shp", "shp", new byte[] { 9 });
        Assert.Equal("application/octet-stream", _fake.Last!.ContentType);
        Assert.Equal("/rest/workspaces/ws/datastores/shp/file.shp", _fake.Last.Path);
    }
}
