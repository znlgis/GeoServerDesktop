using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>AuthenticationFilterService 离线单元测试。FIXED-E1：路由全小写 authfilters，列表/单体均为 3.0.1 双层/动态类名形态。</summary>
public class AuthenticationFilterServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly AuthenticationFilterService _svc;

    public AuthenticationFilterServiceTests() => _svc = new AuthenticationFilterService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new AuthenticationFilterService(null!));

    [Fact]
    public async Task GetFiltersAsync_UsesLowercasePath_ParsesDoubleWrapped_FIXED_E1()
    {
        // FIXED-E1：官方路由为全小写 authfilters；实测列表 {"authfilters":{"authfilter":[{name,href}]}}
        _fake.RespondGet("""{"authfilters":{"authfilter":[{"name":"anonymous","href":"http://x/anonymous.json"},{"name":"basic","href":"http://x/basic.json"}]}}""");
        var w = await _svc.GetFiltersAsync();

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/security/authfilters.json", _fake.Last.Path);
        Assert.Equal(2, w.Filters.Count);
        Assert.Equal("basic", w.Filters[1].Name);

        // 空态容错（字符串值 → 空集合，不抛）
        _fake.RespondGet("""{"authfilters":""}""");
        var empty = await _svc.GetFiltersAsync();
        Assert.Empty(empty.Filters);
    }

    [Fact]
    public async Task GetFilterAsync_ParsesDynamicClassRoot_FIXED_E1()
    {
        // 3.0.1 单体以配置类全名为动态根键（实测）
        _fake.RespondGet("""{"org.geoserver.security.config.BasicAuthenticationFilterConfig":{"id":"x1","name":"basic","className":"org.geoserver.security.filter.GeoServerBasicAuthenticationFilter","useRememberMe":true}}""");

        var w = await _svc.GetFilterAsync("basic");

        Assert.Equal("/rest/security/authfilters/basic.json", _fake.Last!.Path);
        Assert.Equal("basic", w.Filter.Name);
        Assert.Equal("org.geoserver.security.filter.GeoServerBasicAuthenticationFilter", w.Filter.ClassName);
        Assert.True((bool)w.Filter.Config["useRememberMe"]);
    }

    [Fact]
    public async Task CreateFilterAsync_PostsWrappedFilterLowercasePath()
    {
        await _svc.CreateFilterAsync(new AuthenticationFilter { Name = "f" });

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/security/authfilters", _fake.Last.Path);
        Assert.Equal("f", (string?)Json.P(_fake.Last.Body)["filter"]?["name"]);
    }

    [Fact]
    public async Task UpdateFilterAsync_PutsWrappedFilterLowercasePath()
    {
        await _svc.UpdateFilterAsync("f", new AuthenticationFilter { Name = "f", ClassName = "c" });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/security/authfilters/f", _fake.Last.Path);
        Assert.Equal("c", (string?)Json.P(_fake.Last.Body)["filter"]?["className"]);
    }

    [Fact]
    public async Task DeleteFilterAsync_DeletesLowercasePath()
    {
        await _svc.DeleteFilterAsync("f");

        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/rest/security/authfilters/f", _fake.Last.Path);
    }
}
