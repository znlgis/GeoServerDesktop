using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>BlobstoreService (GWC) 离线单元测试。FIXED-E8：列表为顶层裸数组；FIXED-E11：单体根为实现类名
/// {"FileBlobStore":{...}}，创建/更新均为 PUT /gwc/rest/blobstores/{name} + XStream XML（实测 201/200）。</summary>
public class BlobstoreServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly BlobstoreService _svc;

    public BlobstoreServiceTests() => _svc = new BlobstoreService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new BlobstoreService(null!));

    [Fact]
    public async Task GetBlobstoresAsync_RawJsonArray_FIXED_E8()
    {
        _fake.RespondGet("""["diskcache","s3"]""");
        var w = await _svc.GetBlobstoresAsync();
        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/gwc/rest/blobstores.json", _fake.Last.Path); // 走 /gwc/rest 前缀而非 /rest
        Assert.Equal(2, w.Blobstores.Count);
    }

    [Fact]
    public async Task GetBlobstoreAsync_ParsesImplementationClassRoot_FIXED_E11()
    {
        _fake.RespondGet("""{"FileBlobStore":{"fileSystemBlockSize":0,"id":"diskcache","baseDirectory":"/tmp/gwc","@default":"false","enabled":true}}""");

        var w = await _svc.GetBlobstoreAsync("diskcache");

        Assert.Equal("/gwc/rest/blobstores/diskcache.json", _fake.Last!.Path);
        Assert.Equal("diskcache", w.Blobstore.Id);
        Assert.Equal("FileBlobStore", w.Blobstore.Type);
        Assert.Equal("/tmp/gwc", w.Blobstore.BaseDirectory);
        Assert.True(w.Blobstore.Enabled);
    }

    [Fact]
    public async Task CreateBlobstoreAsync_PutsXmlToNamedRoute_FIXED_E11()
    {
        await _svc.CreateBlobstoreAsync(new Blobstore { Id = "new", BaseDirectory = "/data/new" });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/gwc/rest/blobstores/new", _fake.Last.Path);
        Assert.Contains("application/xml", _fake.Last.ContentType);
        Assert.Contains("<FileBlobStore>", _fake.Last.Body);
        Assert.Contains("<name>new</name>", _fake.Last.Body);
        Assert.Contains("<baseDirectory>/data/new</baseDirectory>", _fake.Last.Body);
    }

    [Fact]
    public async Task UpdateBlobstoreAsync_PutsXmlById_FIXED_E11()
    {
        await _svc.UpdateBlobstoreAsync("diskcache", new Blobstore { Id = "diskcache", Enabled = false });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/gwc/rest/blobstores/diskcache", _fake.Last.Path);
        Assert.Contains("<name>diskcache</name>", _fake.Last.Body);
        Assert.Contains("<enabled>false</enabled>", _fake.Last.Body);
    }

    [Fact]
    public async Task DeleteBlobstoreAsync_DeletesById()
    {
        await _svc.DeleteBlobstoreAsync("diskcache");

        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/gwc/rest/blobstores/diskcache", _fake.Last.Path);
    }
}
