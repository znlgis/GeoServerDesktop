using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>ReloadService 离线单元测试。FIXED-E18：POST 空体显式声明 application/json（原默认 text/plain）。</summary>
public class ReloadServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly ReloadService _svc;

    public ReloadServiceTests() => _svc = new ReloadService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new ReloadService(null!));

    [Fact]
    public async Task ReloadCatalogAsync_PostsEmptyJsonBody_FIXED_E18()
    {
        // FIXED-E18：原 new StringContent("") 无显式 media type（text/plain; charset=utf-8）。
        // 实测 3.0.1 /rest/reload 对无体/text/plain/application/json 三种均 200；
        // 修复后统一显式 application/json 空体，路径维持无 .json 后缀现状。
        _fake.RespondPost("""{"msg":"GeoServer reloaded its resources."}""");
        await _svc.ReloadCatalogAsync();

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/reload", _fake.Last.Path);
        Assert.Equal(string.Empty, _fake.Last.Body);
        Assert.Equal("application/json; charset=utf-8", _fake.Last.ContentType);
    }

    [Fact]
    public async Task ResetAsync_PostsEmptyJsonBody_FIXED_E18()
    {
        // 同 ReloadCatalogAsync；Warn：/rest/reset 会清全部缓存，集成侧仅 GET 探测存在性（实测 405）不实调。
        await _svc.ResetAsync();

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/reset", _fake.Last.Path);
        Assert.Equal("application/json; charset=utf-8", _fake.Last.ContentType);
    }
}
