using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>TemplateService 离线单元测试。FIXED-E7：3.0.1 /rest/templates.json 根为类全名
/// {"org.geoserver.rest.catalog.TemplateInfos":""|{...}}（空态空串），服务侧改用容错 Parse。</summary>
public class TemplateServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly TemplateService _svc;

    public TemplateServiceTests() => _svc = new TemplateService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new TemplateService(null!));

    [Fact]
    public async Task GetTemplatesAsync_ParsesDynamicClassRoot_EmptyTolerated_FIXED_E7()
    {
        // 空态实测（3.0.1 全新安装）：
        _fake.RespondGet("""{"org.geoserver.rest.catalog.TemplateInfos":""}""");
        var empty = await _svc.GetTemplatesAsync();
        Assert.Equal("/rest/templates.json", _fake.Last!.Path);
        Assert.Empty(empty.Templates);

        // 有数据：内层为 TemplateInfo 数组
        _fake.RespondGet("""{"org.geoserver.rest.catalog.TemplateInfos":{"org.geoserver.rest.catalog.TemplateInfo":[{"name":"header","href":"h1"}]}}""");
        var w = await _svc.GetTemplatesAsync();
        Assert.Equal(new[] { "header" }, w.Templates);
    }

    [Fact]
    public async Task GetTemplateAsync_NoExtensionReturnsRawString()
    {
        _fake.RespondGet("<h1>tpl</h1>");
        var raw = await _svc.GetTemplateAsync("header");
        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/templates/header", _fake.Last.Path);
        Assert.Equal("<h1>tpl</h1>", raw);
    }

    [Fact]
    public async Task CreateTemplateAsync_PostsRawTextPlainUnderName()
    {
        await _svc.CreateTemplateAsync("header", "<h1>tpl</h1>");
        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/templates/header", _fake.Last.Path);
        Assert.Equal("text/plain; charset=utf-8", _fake.Last.ContentType);
        Assert.Equal("<h1>tpl</h1>", _fake.Last.Body);
    }

    [Fact]
    public async Task DeleteTemplateAsync_DeletesUnderName()
    {
        await _svc.DeleteTemplateAsync("header");
        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/rest/templates/header", _fake.Last.Path);
    }
}
