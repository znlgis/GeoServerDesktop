using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>AuthenticationProviderService 离线单元测试。FIXED-E2：路由全小写 authproviders；列表为动态 Java 类名键 map（JToken 手工解析）。</summary>
public class AuthenticationProviderServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly AuthenticationProviderService _svc;

    public AuthenticationProviderServiceTests() => _svc = new AuthenticationProviderService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new AuthenticationProviderService(null!));

    [Fact]
    public async Task GetProvidersAsync_UsesLowercasePath_ParsesDynamicFqnMap_FIXED_E2()
    {
        _fake.RespondGet("""{"authproviders":{"org.geoserver.security.config.UsernamePasswordAuthenticationProviderConfig":{"id":"i1","name":"default","className":"org.geoserver.security.auth.UsernamePasswordAuthenticationProvider","serverUrl":"u"}}}""");
        var w = await _svc.GetProvidersAsync();

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/security/authproviders.json", _fake.Last.Path);
        Assert.Single(w.Providers);
        Assert.Equal("default", w.Providers[0].Name);
        Assert.Equal("org.geoserver.security.config.UsernamePasswordAuthenticationProviderConfig", w.Providers[0].ConfigClassName);
        Assert.Equal("u", (string?)w.Providers[0].Config["serverUrl"]);

        // 空串容错（无 provider 时值可为 ""）
        _fake.RespondGet("""{"authproviders":""}""");
        var empty = await _svc.GetProvidersAsync();
        Assert.Empty(empty.Providers);
    }

    [Fact]
    public async Task GetProviderAsync_ParsesDynamicClassRoot_FIXED_E2()
    {
        _fake.RespondGet("""{"org.geoserver.security.config.MemoryAuthenticationProviderConfig":{"name":"memory","className":"org.f.C","foo":"bar"}}""");

        var w = await _svc.GetProviderAsync("memory");

        Assert.Equal("/rest/security/authproviders/memory.json", _fake.Last!.Path);
        Assert.Equal("memory", w.Provider.Name);
        Assert.Equal("org.geoserver.security.config.MemoryAuthenticationProviderConfig", w.Provider.ConfigClassName);
        Assert.Equal("bar", (string?)w.Provider.Config["foo"]);
    }

    [Fact]
    public async Task CreateProviderAsync_PostsWrappedProviderLowercasePath()
    {
        await _svc.CreateProviderAsync(new AuthenticationProvider { Name = "p" });

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/security/authproviders", _fake.Last.Path);
        Assert.Equal("p", (string?)Json.P(_fake.Last.Body)["provider"]?["name"]);
    }

    [Fact]
    public async Task UpdateProviderAsync_PutsWrappedProviderLowercasePath()
    {
        await _svc.UpdateProviderAsync("p", new AuthenticationProvider { Name = "p" });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/security/authproviders/p", _fake.Last.Path);
    }

    [Fact]
    public async Task DeleteProviderAsync_DeletesProviderLowercasePath()
    {
        await _svc.DeleteProviderAsync("p");

        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/rest/security/authproviders/p", _fake.Last.Path);
    }
}
