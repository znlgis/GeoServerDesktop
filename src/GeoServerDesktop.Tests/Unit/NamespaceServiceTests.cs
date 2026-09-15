using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>NamespaceService 离线单元测试。删除无 recurse 参数（与 Workspace 不同，属源码现状）。</summary>
public class NamespaceServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly NamespaceService _svc;

    public NamespaceServiceTests() => _svc = new NamespaceService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new NamespaceService(null!));

    [Fact]
    public async Task GetNamespacesAsync_ParsesDoubleWrappedResponse()
    {
        _fake.RespondGet("""{"namespaces":{"namespace":[{"prefix":"topp","uri":"http://topp","isolated":false,"href":"h"}]}}""");

        var result = await _svc.GetNamespacesAsync();

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/namespaces.json", _fake.Last.Path);
        Assert.Single(result);
        Assert.Equal("topp", result[0].Prefix);
        Assert.Equal("http://topp", result[0].Uri);
    }

    [Fact]
    public async Task GetNamespacesAsync_Empty_ReturnsEmptyArray()
    {
        _fake.RespondGet("""{"namespaces":{}}""");
        Assert.Empty(await _svc.GetNamespacesAsync());
    }

    [Fact]
    public async Task GetNamespaceAsync_SingleResourceUnwrap()
    {
        _fake.RespondGet("""{"namespace":{"prefix":"p","uri":"http://p","href":"h"}}""");

        var ns = await _svc.GetNamespaceAsync("p");

        Assert.Equal("/rest/namespaces/p.json", _fake.Last!.Path);
        Assert.Equal("p", ns.Prefix);
        Assert.Equal("http://p", ns.Uri);
    }

    [Fact]
    public async Task CreateNamespaceAsync_PostsPrefixAndUri()
    {
        await _svc.CreateNamespaceAsync("topp", "http://topp");

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/namespaces", _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("topp", (string?)body["namespace"]?["prefix"]);
        Assert.Equal("http://topp", (string?)body["namespace"]?["uri"]);
    }

    [Fact]
    public async Task UpdateNamespaceAsync_PutsBodyIncludingPrefix()
    {
        // 源码现状：PUT body 仍包含 prefix（与创建同构），路径用旧 prefix
        await _svc.UpdateNamespaceAsync("old", "http://new");

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/namespaces/old", _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("old", (string?)body["namespace"]?["prefix"]);
        Assert.Equal("http://new", (string?)body["namespace"]?["uri"]);
    }

    [Fact]
    public async Task DeleteNamespaceAsync_NoRecurseParameter()
    {
        await _svc.DeleteNamespaceAsync("topp");

        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/rest/namespaces/topp", _fake.Last.Path);
    }
}
