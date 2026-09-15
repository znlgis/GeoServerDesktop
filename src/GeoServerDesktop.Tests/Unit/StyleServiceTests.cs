using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>StyleService 离线单元测试：POST+PUT 两步创建、purge 参数（非 recurse，与 GeoServer 一致）。</summary>
public class StyleServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly StyleService _svc;

    public StyleServiceTests() => _svc = new StyleService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new StyleService(null!));

    [Fact]
    public async Task GetStylesAsync_ParsesStylesWrapper()
    {
        // languageVersion.version 在真实 GeoServer JSON 中为数值，模型为 string（Newtonsoft 数值→字符串转换）
        _fake.RespondGet("""{"styles":{"style":[{"name":"poi","format":"sld","filename":"poi.sld","languageVersion":{"class":"languageVersion","version":"1.0"},"href":"h"}]}}""");

        var result = await _svc.GetStylesAsync();

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/styles.json", _fake.Last.Path);
        Assert.Single(result);
        Assert.Equal("poi", result[0].Name);
        Assert.Equal("1.0", result[0].LanguageVersion.Version);
    }

    [Fact]
    public async Task GetStyleAsync_SingleUnwrap()
    {
        _fake.RespondGet("""{"style":{"name":"poi","format":"sld","href":"h"}}""");

        var s = await _svc.GetStyleAsync("poi");

        Assert.Equal("/rest/styles/poi.json", _fake.Last!.Path);
        Assert.Equal("sld", s.Format);
    }

    [Fact]
    public async Task GetStyleSldAsync_RequestsDotSldAndReturnsRawXml()
    {
        const string sld = "<StyledLayerDescriptor/>";
        _fake.RespondGet(sld);

        var result = await _svc.GetStyleSldAsync("poi");

        Assert.Equal("/rest/styles/poi.sld", _fake.Last!.Path);
        Assert.Equal(sld, result); // 原始字符串直返
    }

    [Fact]
    public async Task CreateStyleAsync_TwoStepPostMetadataThenPutSld()
    {
        const string sld = "<sld>body</sld>";
        await _svc.CreateStyleAsync("mystyle", sld);

        Assert.Equal(2, _fake.Requests.Count);

        // 第一步：POST 元数据 {"style":{"name":...,"filename":"{name}.sld"}}
        var step1 = _fake.Requests[0];
        Assert.Equal("POST", step1.Method);
        Assert.Equal("/rest/styles", step1.Path);
        Assert.Equal("application/json; charset=utf-8", step1.ContentType);
        var body1 = Json.P(step1.Body);
        Assert.Equal("mystyle", (string?)body1["style"]?["name"]);
        Assert.Equal("mystyle.sld", (string?)body1["style"]?["filename"]);

        // 第二步：PUT 原始 SLD，Content-Type 为 SLD XML
        var step2 = _fake.Requests[1];
        Assert.Equal("PUT", step2.Method);
        Assert.Equal("/rest/styles/mystyle", step2.Path);
        Assert.Equal("application/vnd.ogc.sld+xml; charset=utf-8", step2.ContentType);
        Assert.Equal(sld, step2.Body);
    }

    [Fact]
    public async Task UpdateStyleAsync_PutsRawSld()
    {
        const string sld = "<sld>x</sld>";
        await _svc.UpdateStyleAsync("poi", sld);

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/styles/poi", _fake.Last.Path);
        Assert.Equal(sld, _fake.Last.Body);
        Assert.Equal("application/vnd.ogc.sld+xml; charset=utf-8", _fake.Last.ContentType);
    }

    [Fact]
    public async Task DeleteStyleAsync_UsesPurgeNotRecurse_KNOWN_ISSUE_E22()
    {
        // KNOWN-ISSUE E22 括注：Style 删除用 purge（与 GeoServer 一致，官方即如此，非缺陷）；固化参数名与大小写
        await _svc.DeleteStyleAsync("poi");
        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/rest/styles/poi?purge=false", _fake.Last.Path);

        await _svc.DeleteStyleAsync("poi", purge: true);
        Assert.Equal("/rest/styles/poi?purge=true", _fake.Last.Path);
    }

    [Fact]
    public async Task GetWorkspaceStylesAsync_ScopedPath()
    {
        _fake.RespondGet("""{"styles":{"style":[{"name":"poi"}]}}""");
        var result = await _svc.GetWorkspaceStylesAsync("ws");

        Assert.Equal("/rest/workspaces/ws/styles.json", _fake.Last!.Path);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetWorkspaceStyleAsync_ScopedPath()
    {
        _fake.RespondGet("""{"style":{"name":"poi","href":"h"}}""");
        var s = await _svc.GetWorkspaceStyleAsync("ws", "poi");

        Assert.Equal("/rest/workspaces/ws/styles/poi.json", _fake.Last!.Path);
        Assert.Equal("h", s.Href);
    }

    [Fact]
    public async Task GetWorkspaceStyleSldAsync_ScopedDotSld()
    {
        _fake.RespondGet("<x/>");
        var raw = await _svc.GetWorkspaceStyleSldAsync("ws", "poi");

        Assert.Equal("/rest/workspaces/ws/styles/poi.sld", _fake.Last!.Path);
        Assert.Equal("<x/>", raw);
    }

    [Fact]
    public async Task CreateWorkspaceStyleAsync_TwoStepScoped()
    {
        const string sld = "<sld/>";
        await _svc.CreateWorkspaceStyleAsync("ws", "poi", sld);

        Assert.Equal(2, _fake.Requests.Count);
        Assert.Equal("POST", _fake.Requests[0].Method);
        Assert.Equal("/rest/workspaces/ws/styles", _fake.Requests[0].Path);
        var body1 = Json.P(_fake.Requests[0].Body);
        Assert.Equal("poi", (string?)body1["style"]?["name"]);
        Assert.Equal("poi.sld", (string?)body1["style"]?["filename"]);

        Assert.Equal("PUT", _fake.Requests[1].Method);
        Assert.Equal("/rest/workspaces/ws/styles/poi", _fake.Requests[1].Path);
        Assert.Equal(sld, _fake.Requests[1].Body);
    }

    [Fact]
    public async Task UpdateWorkspaceStyleAsync_PutsRawSldScoped()
    {
        await _svc.UpdateWorkspaceStyleAsync("ws", "poi", "<s/>");

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/workspaces/ws/styles/poi", _fake.Last.Path);
        Assert.Equal("<s/>", _fake.Last.Body);
    }

    [Fact]
    public async Task DeleteWorkspaceStyleAsync_PurgeScoped()
    {
        await _svc.DeleteWorkspaceStyleAsync("ws", "poi", purge: true);

        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/rest/workspaces/ws/styles/poi?purge=true", _fake.Last.Path);
    }
}
