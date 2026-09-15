using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>WorkspaceService 离线单元测试：动词/路径/请求体包装/响应解析。</summary>
public class WorkspaceServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly WorkspaceService _svc;

    public WorkspaceServiceTests() => _svc = new WorkspaceService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new WorkspaceService(null!));

    [Fact]
    public async Task GetWorkspacesAsync_UsesJsonSuffixAndParsesDoubleWrappedResponse()
    {
        // GET /rest/workspaces.json；喂入 GeoServer 真实双层包装并断言强类型属性
        _fake.RespondGet("""{"workspaces":{"workspace":[{"name":"topp","isolated":false,"href":"http://localhost:8080/geoserver/rest/workspaces/topp.json"},{"name":"ne","isolated":true,"href":"h2"}]}}""");

        var result = await _svc.GetWorkspacesAsync();

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/workspaces.json", _fake.Last.Path);
        Assert.Equal(2, result.Length);
        Assert.Equal("topp", result[0].Name);
        Assert.False(result[0].Isolated);
        Assert.Equal("http://localhost:8080/geoserver/rest/workspaces/topp.json", result[0].Href);
        Assert.True(result[1].Isolated);
    }

    [Fact]
    public async Task GetWorkspacesAsync_EmptyPayload_ReturnsEmptyArray()
    {
        // 服务层对 wrapper/list/数组任一为 null 时返回空数组（非 null）
        _fake.RespondGet("""{"workspaces":{}}""");
        Assert.Empty(await _svc.GetWorkspacesAsync());

        _fake.RespondGet("{}");
        Assert.Empty(await _svc.GetWorkspacesAsync());
    }

    [Fact]
    public async Task GetWorkspaceAsync_AppendsJsonSuffixAndUnwrapsWorkspaceRoot()
    {
        _fake.RespondGet("""{"workspace":{"name":"topp","isolated":false,"href":"h"}}""");

        var ws = await _svc.GetWorkspaceAsync("topp");

        Assert.Equal("/rest/workspaces/topp.json", _fake.Last!.Path);
        Assert.Equal("topp", ws.Name);
        Assert.Equal("h", ws.Href);
    }

    [Fact]
    public async Task CreateWorkspaceAsync_PostsWrappedNameNoJsonSuffix()
    {
        // 注意：POST 创建路径不带 .json 后缀（依赖 Accept 头）
        await _svc.CreateWorkspaceAsync("topp");

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/workspaces", _fake.Last.Path);
        Assert.Equal("application/json; charset=utf-8", _fake.Last.ContentType);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("topp", (string?)body["workspace"]?["name"]);
    }

    [Fact]
    public async Task UpdateWorkspaceAsync_PutsNewNameUnderOldNamePath()
    {
        await _svc.UpdateWorkspaceAsync("old", "new");

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/workspaces/old", _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("new", (string?)body["workspace"]?["name"]);
    }

    [Fact]
    public async Task Paths_EscapeResourceNameSegments_FIXED_E31Family()
    {
        // FIXED（资源名安全）：所有内插 name 段统一 Uri.EscapeDataString（含空格/中文/保留字符不再破坏 URL）
        await _svc.GetWorkspaceAsync("my ws");
        Assert.Equal("/rest/workspaces/my%20ws.json", _fake.Last!.Path);

        await _svc.UpdateWorkspaceAsync("中文名", "new");
        Assert.Contains("/rest/workspaces/" + Uri.EscapeDataString("中文名"), _fake.Last!.Path);

        await _svc.DeleteWorkspaceAsync("a/b", recurse: true);
        Assert.Contains("/rest/workspaces/" + Uri.EscapeDataString("a/b"), _fake.Last!.Path);
        Assert.EndsWith("?recurse=true", _fake.Last.Path); // 转义不影响查询串
    }

    [Fact]
    public async Task DeleteWorkspaceAsync_RecurseSerializedAsLowerInvariant()
    {
        await _svc.DeleteWorkspaceAsync("topp");
        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/rest/workspaces/topp?recurse=false", _fake.Last.Path);

        await _svc.DeleteWorkspaceAsync("topp", recurse: true);
        Assert.Equal("/rest/workspaces/topp?recurse=true", _fake.Last.Path);
    }
}
