using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>ResourceService（数据目录文件）离线单元测试。含 headers.Add("Content-Type") 写法。</summary>
public class ResourceServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly ResourceService _svc;

    public ResourceServiceTests() => _svc = new ResourceService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new ResourceService(null!));

    [Fact]
    public async Task ListResourcesAsync_EmptyPathRequestsBareResourceEndpoint()
    {
        _fake.RespondGet("<html>listing</html>");

        var raw = await _svc.ListResourcesAsync("");

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/resource", _fake.Last.Path); // 空路径 → 无尾斜杠裸端点
        Assert.Equal("<html>listing</html>", raw);
    }

    [Fact]
    public async Task ListResourcesAsync_NonEmptyPathAppendsUnderResource()
    {
        _fake.RespondGet("file-content");
        var raw = await _svc.ListResourcesAsync("styles/poi.sld");

        Assert.Equal("/rest/resource/styles/poi.sld", _fake.Last!.Path);
        Assert.Equal("file-content", raw);
    }

    [Fact]
    public async Task GetResourceContentAsync_ReadsFileUnderResource()
    {
        _fake.RespondGet("log-line-1\nlog-line-2");

        var raw = await _svc.GetResourceContentAsync("logs/geoserver.log");

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/resource/logs/geoserver.log", _fake.Last.Path);
        Assert.Equal("log-line-1\nlog-line-2", raw);
    }

    [Fact]
    public async Task GetResourceContentAsync_EmptyPath_ThrowsWithoutRequest()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _svc.GetResourceContentAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _svc.GetResourceContentAsync("   "));
        Assert.Empty(_fake.Requests); // 空路径不得发出请求（避免裸目录列表端点）
    }

    [Fact]
    public async Task UploadResourceAsync_PutsRawBytesWithHeaderAddedContentType()
    {
        var data = new byte[] { 10, 20, 30 };
        await _svc.UploadResourceAsync("styles/x.sld", data, "application/vnd.ogc.sld+xml");

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/resource/styles/x.sld", _fake.Last.Path);
        Assert.Equal("application/vnd.ogc.sld+xml", _fake.Last.ContentType);
        Assert.Equal(data, _fake.Last.RawBody);
    }

    [Fact]
    public async Task UploadResourceAsync_DefaultContentTypeOctetStream()
    {
        await _svc.UploadResourceAsync("d/f.bin", new byte[] { 1 });
        Assert.Equal("application/octet-stream", _fake.Last!.ContentType);
    }

    [Fact]
    public async Task DeleteResourceAsync_DeletesUnderResource()
    {
        await _svc.DeleteResourceAsync("styles/x.sld");

        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/rest/resource/styles/x.sld", _fake.Last.Path);
    }
}
