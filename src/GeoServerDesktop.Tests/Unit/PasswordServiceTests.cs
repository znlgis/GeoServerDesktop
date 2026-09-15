using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>PasswordService 离线单元测试。FIXED-E16（原基线误报翻正）：3.0.1 源码
/// UserPasswordController#passwordPut 以 @RequestBody Map 直接取扁平 newPassword 键——
/// 扁平 {"newPassword":"..."} 即真实契约，外层 {"password":{...}} 包装反而 400。</summary>
public class PasswordServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly PasswordService _svc;

    public PasswordServiceTests() => _svc = new PasswordService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new PasswordService(null!));

    [Fact]
    public async Task ChangePasswordAsync_PutsFlatNewPasswordBody_FIXED_E16()
    {
        await _svc.ChangePasswordAsync("s3cret");

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/security/self/password", _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("s3cret", (string?)body["newPassword"]); // 扁平键即契约（UserPasswordController Map @RequestBody）
        Assert.Null(body["password"]); // 不应有包装根
    }
}
