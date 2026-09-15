using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>UserGroupService 离线单元测试。FIXED-E7：3.0.1（UsersRestController 源码+实测）
/// users.json 为对象数组、groups.json 为字符串数组；无单体 GET 详情路由（由列表+成员端点合成）；
/// 创建 POST /users、更新 POST /user/{user}、删除 DELETE /user/{user}；组创建 POST /group/{name}（集合 POST /groups 实测 405）。</summary>
public class UserGroupServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly UserGroupService _svc;

    public UserGroupServiceTests() => _svc = new UserGroupService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new UserGroupService(null!));

    [Fact]
    public async Task GetServicesAsync_UsesUsergroupservicesRoute_FIXED_E7()
    {
        // 3.0.1 路由为 /rest/security/usergroupservices（旧 /usergroup/services 实测 404）
        _fake.RespondGet("""{"userGroupServices":{"userGroupService":[{"name":"default","href":"http://x/default.json"}]}}""");
        var ok = await _svc.GetServicesAsync();

        Assert.Equal("/rest/security/usergroupservices.json", _fake.Last!.Path);
        Assert.Equal(new[] { "default" }, ok.Services);
        Assert.Equal("http://x/default.json", ok.References[0].Href);
    }

    [Fact]
    public async Task GetUsersAsync_ParsesObjectArray_FIXED_E7()
    {
        // 3.0.1 实测直接对象数组（无内层 user 键）
        _fake.RespondGet("""{"users":[{"enabled":true,"password":null,"userName":"admin"},{"enabled":false,"password":null,"userName":"root"}]}""");
        var w = await _svc.GetUsersAsync();

        Assert.Equal("/rest/security/usergroup/users.json", _fake.Last!.Path);
        Assert.Equal(2, w.Users.Count);
        Assert.Equal(new[] { "admin", "root" }, w.UserNames);
        Assert.True(w.Users[0].Enabled);
        Assert.False(w.Users[1].Enabled);
    }

    [Fact]
    public async Task GetUserAsync_SynthesizesFromListAndGroupsEndpoint_FIXED_E7()
    {
        // 单体 GET users/{name}.json 在 3.0.1 实测 404 —— 改为 users 列表 + GET /user/{name}/groups.json 合成
        _fake.RespondGet("""{"users":[{"enabled":true,"password":null,"userName":"admin"},{"enabled":true,"password":null,"userName":"other"}]}""");
        _fake.RespondGet("""{"groups":["admin"]}""");

        var w = await _svc.GetUserAsync("admin");

        Assert.Equal("/rest/security/usergroup/users.json", _fake.Requests[0].Path);
        Assert.Equal("/rest/security/usergroup/user/admin/groups.json", _fake.Requests[1].Path);
        Assert.Equal("admin", w.User.UserName);
        Assert.True(w.User.Enabled);
        Assert.Equal(new[] { "admin" }, w.User.Groups);
    }

    [Fact]
    public async Task GetUserAsync_UnknownUser_ReturnsNullUserWithoutGroupsProbe_FIXED_E7()
    {
        _fake.RespondGet("""{"users":[{"enabled":true,"userName":"admin"}]}""");

        var w = await _svc.GetUserAsync("ghost");

        Assert.Null(w.User);
        Assert.Single(_fake.Requests); // 未命中则不再请求 groups
    }

    [Fact]
    public async Task CreateUserAsync_PostsWrappedUser_DefaultsEnabled_FIXED_E7()
    {
        // enabled 必须显式（否则 3.0.1 NPE 500）；password null 经 GeoServerJson.Request 省略
        await _svc.CreateUserAsync(new User { UserName = "bob", Enabled = null });

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/security/usergroup/users", _fake.Last.Path);
        var u = Json.P(_fake.Last.Body)["user"];
        Assert.Equal("bob", (string?)u?["userName"]);
        Assert.True((bool?)u?["enabled"]);
        Assert.Null(u["password"]); // null 字段不再序列化
    }

    [Fact]
    public async Task CreateUserAsync_KeepsExplicitFields()
    {
        await _svc.CreateUserAsync(new User { UserName = "bob", Password = "pw", Enabled = false });

        var u = Json.P(_fake.Last!.Body)["user"];
        Assert.Equal("pw", (string?)u?["password"]);
        Assert.False((bool?)u?["enabled"]);
    }

    [Fact]
    public async Task UpdateUserAsync_PostsSingularUserRoute_FIXED_E7()
    {
        // 实测：PUT users/{name} 404、PUT user/{name} 405；更新为 POST /user/{user}（200）
        await _svc.UpdateUserAsync("bob", new User { UserName = "bob", Enabled = false });

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/security/usergroup/user/bob", _fake.Last.Path);
        Assert.False((bool?)Json.P(_fake.Last.Body)["user"]?["enabled"]);
    }

    [Fact]
    public async Task DeleteUserAsync_DeletesSingularUserRoute_FIXED_E7()
    {
        // 实测 DELETE /user/{user} 200；复数 users/{user} 404
        await _svc.DeleteUserAsync("bob");

        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/rest/security/usergroup/user/bob", _fake.Last.Path);
    }

    [Fact]
    public async Task GetGroupsAsync_ParsesStringArray_FIXED_E7()
    {
        // 3.0.1 实测 groups.json={"groups":["str",...]} 直接字符串数组（E7 对 groups 属误报，模型即真实形态）
        _fake.RespondGet("""{"groups":["admin","everyone"]}""");
        var w = await _svc.GetGroupsAsync();
        Assert.Equal("/rest/security/usergroup/groups.json", _fake.Last!.Path);
        Assert.Equal(2, w.Groups.Count);
    }

    [Fact]
    public async Task GetGroupAsync_NoUsableRoute_ThrowsNotSupported_FIXED_E7()
    {
        // 3.0.1 组详情通道不可用：/group/{g}/users 实测对任意组名恒 400（Tomcat），且无组属性 GET 路由
        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => _svc.GetGroupAsync("dev"));
        Assert.Contains("3.0.1", ex.Message);
        Assert.Null(_fake.Last); // 不发出请求
    }

    [Fact]
    public async Task CreateGroupAsync_PostsGroupNamedRoute_FIXED_E7()
    {
        // 集合 POST /groups 实测 405 → 创建路由 POST /group/{groupName}（实测 201）
        await _svc.CreateGroupAsync(new UserGroup { GroupName = "dev" });

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/security/usergroup/group/dev", _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("dev", (string?)body["group"]?["groupName"]);
        Assert.True((bool?)body["group"]?["enabled"]); // enabled 显式（null → true 兜底）
    }

    [Fact]
    public async Task DeleteGroupAsync_DeletesGroupSingularRoute_FIXED_E7()
    {
        await _svc.DeleteGroupAsync("dev");

        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/rest/security/usergroup/group/dev", _fake.Last.Path);
    }

    [Fact]
    public void NoUpdateGroupMethodExists_MissingFeatureNoted()
    {
        // 清单：无 UpdateGroup —— 缺功能而非错误；用反射固化"不存在该方法"的现状
        Assert.Null(typeof(UserGroupService).GetMethod("UpdateGroupAsync"));
    }
}
