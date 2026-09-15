using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>URLCheckService 离线单元测试。FIXED-E7：实测根为 urlChecks 且空态值为 ""（{"urlChecks":""}），
/// 有值按文档为 {"urlChecks":{"entry":[{"string":[name,value]}]}}，服务侧改用容错 Parse。
/// E20（创建包装）维持基线：UrlCheckController @RequestBody 需 XStream 具体实现类根，默认安装无样例可固化。</summary>
public class URLCheckServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly URLCheckService _svc;

    public URLCheckServiceTests() => _svc = new URLCheckService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new URLCheckService(null!));

    [Fact]
    public async Task GetURLChecksAsync_EmptyStringAndEntryArray_Tolerated_FIXED_E7()
    {
        // 空态实测（默认安装无任何检查）：
        _fake.RespondGet("""{"urlChecks":""}""");
        var w = await _svc.GetURLChecksAsync();
        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/urlchecks.json", _fake.Last.Path);
        Assert.Empty(w.Checks);

        // 有值（文档 entry 数组形态）：
        _fake.RespondGet("""{"urlChecks":{"entry":[{"string":["checkLocalHost","false"]},{"string":["checkReferer","false"]}]}}""");
        var w2 = await _svc.GetURLChecksAsync();
        Assert.Equal(new[] { "checkLocalHost", "checkReferer" }, w2.Checks);
    }

    [Fact]
    public async Task CreateURLCheckAsync_PostsUnwrappedCheck_KNOWN_ISSUE_E20()
    {
        // KNOWN-ISSUE E20 维持基线：3.0.1 UrlCheckController（gs-restconfig，POST /rest/urlchecks）
        // 以 @RequestBody AbstractUrlCheck（XStream 具体实现类为根）接收，实测 {"urlCheck":{...}} 报
        // "Cannot construct type"（抽象类不可实例化），默认安装亦无任何已配置检查可反推真实根名——
        // 无法在不依赖私有实现类的前提下固化请求体，保留现状请求形态并注释说明。
        await _svc.CreateURLCheckAsync(new URLCheck { Name = "block", UrlPattern = "file:.*", CheckType = "DENY", Enabled = true });

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/urlchecks", _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("block", (string?)body["name"]);
        Assert.Equal("DENY", (string?)body["checkType"]);
        Assert.Null(body["urlChecks"]); // 无包装根（E20 现状）
    }

    [Fact]
    public async Task DeleteURLCheckAsync_DeletesByName()
    {
        await _svc.DeleteURLCheckAsync("block");

        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/rest/urlchecks/block", _fake.Last.Path);
    }

    [Fact]
    public void NoUpdateMethod_CurrentSurfacePin()
    {
        // 清单：URLCheckService 无 Update 方法（缺功能）
        Assert.Null(typeof(URLCheckService).GetMethod("UpdateURLCheckAsync"));
    }
}
