using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>FilterChainService 离线单元测试。FIXED-E3：3.0.1 真实路由为单数全小写 /rest/security/filterchain（复数实测 404）。</summary>
public class FilterChainServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly FilterChainService _svc;

    public FilterChainServiceTests() => _svc = new FilterChainService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new FilterChainService(null!));

    [Fact]
    public async Task GetFilterChainsAsync_SingularLowercasePath_ParsesDoubleWrapped_FIXED_E3()
    {
        await _svc.GetFilterChainsAsync();
        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/security/filterchain.json", _fake.Last.Path); // 复数 filterChains/filterchains 实测 404

        // 实测响应：{"filterchain":{"filters":[{"@name":...}]}}
        _fake.RespondGet("""{"filterchain":{"filters":[{"@name":"web","@class":"org.geoserver.security.HtmlLoginFilterChain","@path":"/web/**,/","@disabled":false,"@allowSessionCreation":true,"filter":["httpBasic"]},{"@name":"rest","@path":"/rest/**","filter":["anonymous"]}]}}""");
        var w = await _svc.GetFilterChainsAsync();
        Assert.Equal(2, w.Chains.Count);
        Assert.Equal("web", w.Chains[0].Name);
        Assert.Equal("/web/**,/", w.Chains[0].Pattern);
        Assert.True(w.Chains[0].AllowSessionCreation);
        Assert.Equal(new[] { "httpBasic" }, w.Chains[0].Filters);
    }

    [Fact]
    public async Task GetFilterChainAsync_ParsesSingleUnderSingularPath_FIXED_E3()
    {
        // 单体响应根为 "filters"（对象）
        _fake.RespondGet("""{"filters":{"@name":"anonymous","@path":"/","@disabled":false,"@allowSessionCreation":false,"filter":["anonymous","httpBasic"]}}""");

        var w = await _svc.GetFilterChainAsync("anonymous");

        Assert.Equal("/rest/security/filterchain/anonymous.json", _fake.Last!.Path);
        Assert.Equal("anonymous", w.Chain.Name);
        Assert.Equal(new[] { "anonymous", "httpBasic" }, w.Chain.Filters);
    }

    [Fact]
    public async Task UpdateFilterChainAsync_PutsUnderSingularPath_FIXED_E3()
    {
        await _svc.UpdateFilterChainAsync("c", new FilterChain { Name = "c", Pattern = "/*" });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/security/filterchain/c", _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("/*", (string?)body["filterChain"]?["@path"]);
    }

    [Fact]
    public void NoCreateOrDeleteMethods_CurrentSurfacePin()
    {
        // 清单：FilterChainService 无 Create/Delete（GeoServer 过滤链不可增删，仅可改），固化方法面
        Assert.Null(typeof(FilterChainService).GetMethod("CreateFilterChainAsync"));
        Assert.Null(typeof(FilterChainService).GetMethod("DeleteFilterChainAsync"));
    }
}
