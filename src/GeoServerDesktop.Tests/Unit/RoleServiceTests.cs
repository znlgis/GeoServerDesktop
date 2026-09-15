using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>RoleService 离线单元测试。FIXED-E7（roles 属误报翻正）：实测 roles.json 就是 {"roles":["ADMIN",...]} 字符串数组，模型恰合真实形态。</summary>
public class RoleServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly RoleService _svc;

    public RoleServiceTests() => _svc = new RoleService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new RoleService(null!));

    [Fact]
    public async Task GetRolesAsync_RealStringArrayShape_Parses_FIXED_E7()
    {
        _fake.RespondGet("""{"roles":["ADMIN","AUTHENTICATED"]}""");
        var w = await _svc.GetRolesAsync();
        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/security/roles.json", _fake.Last.Path);
        Assert.Equal(2, w.Roles.Count);
    }

    [Fact]
    public async Task GetUserRolesAsync_UsesUserScopedPath()
    {
        // 实测 GET /rest/security/roles/user/admin.json → {"roles":["ADMIN"]}
        _fake.RespondGet("""{"roles":["ADMIN"]}""");
        var w = await _svc.GetUserRolesAsync("admin");
        Assert.Equal("/rest/security/roles/user/admin.json", _fake.Last!.Path);
        Assert.Single(w.Roles);
    }

    [Fact]
    public async Task AssociateRoleAsync_PostsWithNullContent()
    {
        await _svc.AssociateRoleAsync("ADMIN", "bob");
        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/security/roles/role/ADMIN/user/bob", _fake.Last.Path);
        Assert.Null(_fake.Last.Body);
        Assert.Null(_fake.Last.ContentType);
    }

    [Fact]
    public async Task DissociateRoleAsync_DeletesSamePath()
    {
        await _svc.DissociateRoleAsync("ADMIN", "bob");
        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/rest/security/roles/role/ADMIN/user/bob", _fake.Last.Path);
    }
}
