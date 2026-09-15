using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>SecurityService（ACL）离线单元测试。FIXED-E19：3.0.1 ACL 路由无 .json 后缀（带后缀实测 404），
/// catalog 回 {"mode":...}，其余资源回扁平 {"资源模式":"逗号分隔角色"} map，由 SecurityACL.Parse 双形态承接。</summary>
public class SecurityServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly SecurityService _svc;

    public SecurityServiceTests() => _svc = new SecurityService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new SecurityService(null!));

    [Fact]
    public async Task GetACLAsync_CatalogModeRoot_NoJsonSuffix_FIXED_E19()
    {
        _fake.RespondGet("""{"mode":"HIDE"}""");

        var acl = await _svc.GetACLAsync("catalog");

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/security/acl/catalog", _fake.Last.Path); // E19 修复：不再拼 .json（真实服务器 404）
        Assert.Equal("HIDE", acl.Mode);
        Assert.Null(acl.Rules);
    }

    [Fact]
    public async Task GetACLAsync_FlatMapRoot_TranslatesToRules_FIXED_E19()
    {
        _fake.RespondGet("""{"*.*.r":"*","*.*.w":"GROUP_ADMIN,ADMIN"}""");

        var acl = await _svc.GetACLAsync("layers");

        Assert.Equal(2, acl.Rules.Count);
        Assert.Equal("*.*.r", acl.Rules[0].Role);
        Assert.Equal("*", acl.Rules[0].Access);
        Assert.Equal("GROUP_ADMIN,ADMIN", acl.Rules[1].Access);
        Assert.Equal("GROUP_ADMIN,ADMIN", acl.RawRules["*.*.w"]);
    }

    [Fact]
    public async Task SetACLAsync_PostsModeBodyForCatalog_FIXED_E19()
    {
        var acl = new SecurityACL { Mode = "MIXED" };
        await _svc.SetACLAsync("catalog", acl);

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/security/acl/catalog", _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("MIXED", (string?)body["mode"]);
    }

    [Fact]
    public async Task SetACLAsync_PostsFlatMapForRuleResources_FIXED_E19()
    {
        // 旧 Rules 列表 → 转译回扁平 map（3.0.1 ACL 文件即 map 形态）
        var acl = new SecurityACL
        {
            Rules = new List<AccessRule> { new() { Role = "*.*.r", Access = "*" } }
        };
        await _svc.SetACLAsync("layers", acl);

        var body = Json.P(_fake.Last!.Body);
        Assert.Equal("*", (string?)body["*.*.r"]);
        Assert.Null(body["mode"]);
        Assert.Null(body["rules"]);
    }

    [Fact]
    public async Task DeleteACLAsync_DeletesNoJsonSuffix()
    {
        await _svc.DeleteACLAsync("res");
        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/rest/security/acl/res", _fake.Last.Path);
    }
}
