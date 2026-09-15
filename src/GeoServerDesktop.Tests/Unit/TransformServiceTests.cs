using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>TransformService 离线单元测试。创建用 application/xslt+xml 原文；含 E7。</summary>
public class TransformServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly TransformService _svc;

    public TransformServiceTests() => _svc = new TransformService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new TransformService(null!));

    [Fact]
    public async Task GetTransformsAsync_FlatParses_RealDoubleWrappedThrows_KNOWN_ISSUE_E7()
    {
        _fake.RespondGet("""{"transforms":["gml32-xsd"]}""");
        var w = await _svc.GetTransformsAsync();
        Assert.Equal("/rest/transforms.json", _fake.Last!.Path);
        Assert.Single(w.Transforms);

        // KNOWN-ISSUE E7：实际 {"transforms":{"transform":[...]}} → 抛
        _fake.RespondGet("""{"transforms":{"transform":[{"name":"t"}]}}""");
        await Assert.ThrowsAnyAsync<Newtonsoft.Json.JsonException>(() => _svc.GetTransformsAsync());
    }

    [Fact]
    public async Task GetTransformAsync_NoExtensionRawString()
    {
        _fake.RespondGet("<xslt/>");
        var raw = await _svc.GetTransformAsync("t1");

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/transforms/t1", _fake.Last.Path);
        Assert.Equal("<xslt/>", raw);
    }

    [Fact]
    public async Task CreateTransformAsync_PostsRawXsltWithXsltContentType()
    {
        await _svc.CreateTransformAsync("t1", "<xslt:stylesheet/>");

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/transforms/t1", _fake.Last.Path);
        Assert.Equal("application/xslt+xml; charset=utf-8", _fake.Last.ContentType);
        Assert.Equal("<xslt:stylesheet/>", _fake.Last.Body);
    }

    [Fact]
    public async Task DeleteTransformAsync_DeletesByName()
    {
        await _svc.DeleteTransformAsync("t1");

        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/rest/transforms/t1", _fake.Last.Path);
    }
}
