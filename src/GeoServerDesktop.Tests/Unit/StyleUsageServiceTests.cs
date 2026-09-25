using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>
/// StyleUsageService 离线单元测试：样式列表 + 图层列表 + 逐图层详情聚合；
/// 工作空间样式引用排除、单图层失败跳过、Ordinal 精确匹配、未引用筛选。
/// </summary>
public class StyleUsageServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly StyleUsageService _svc;

    public StyleUsageServiceTests() => _svc = new StyleUsageService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new StyleUsageService(null!));

    [Fact]
    public async Task GetStyleUsageAsync_AggregatesGlobalStyleReferences()
    {
        _fake.RespondGet("""{"styles":{"style":[{"name":"poi"},{"name":"roads"}]}}""");
        _fake.RespondGet("""{"layers":{"layer":[{"name":"l1"},{"name":"l2"}]}}""");
        _fake.RespondGet("""{"layer":{"name":"l1","defaultStyle":{"name":"poi","href":"http://localhost:8765/geoserver/rest/styles/poi.json"}}}""");
        _fake.RespondGet("""{"layer":{"name":"l2","defaultStyle":{"name":"ws_style","href":"http://localhost:8765/geoserver/rest/workspaces/ws/styles/ws_style.json"}}}""");

        var usages = await _svc.GetStyleUsageAsync();

        // 请求序列：样式列表 → 图层列表 → 逐图层详情
        Assert.Equal(4, _fake.Requests.Count);
        Assert.Equal("/rest/styles.json", _fake.Requests[0].Path);
        Assert.Equal("/rest/layers.json", _fake.Requests[1].Path);
        Assert.Equal("/rest/layers/l1.json", _fake.Requests[2].Path);
        Assert.Equal("/rest/layers/l2.json", _fake.Requests[3].Path);

        Assert.Equal(2, usages.Length);
        Assert.Equal("poi", usages[0].StyleName);
        Assert.True(usages[0].IsUsed);
        Assert.Single(usages[0].Layers);
        Assert.Equal("l1", usages[0].Layers[0]);

        Assert.Equal("roads", usages[1].StyleName);
        Assert.False(usages[1].IsUsed);
        Assert.Empty(usages[1].Layers);
    }

    [Fact]
    public async Task GetStyleUsageAsync_WorkspaceStyleReference_NotCounted()
    {
        // 工作空间样式 href 含 /workspaces/：即使与全局样式同名也不计入全局总览
        _fake.RespondGet("""{"styles":{"style":[{"name":"poi"}]}}""");
        _fake.RespondGet("""{"layers":{"layer":[{"name":"l1"}]}}""");
        _fake.RespondGet("""{"layer":{"name":"l1","defaultStyle":{"name":"poi","href":"http://h/rest/workspaces/ws/styles/poi.json"}}}""");

        var usages = await _svc.GetStyleUsageAsync();

        Assert.Single(usages);
        Assert.False(usages[0].IsUsed);
    }

    [Fact]
    public async Task GetStyleUsageAsync_NullHref_StillCounted()
    {
        _fake.RespondGet("""{"styles":{"style":[{"name":"poi"}]}}""");
        _fake.RespondGet("""{"layers":{"layer":[{"name":"l1"}]}}""");
        _fake.RespondGet("""{"layer":{"name":"l1","defaultStyle":{"name":"poi"}}}""");

        var usages = await _svc.GetStyleUsageAsync();

        Assert.True(usages[0].IsUsed);
        Assert.Equal("l1", usages[0].Layers[0]);
    }

    [Fact]
    public async Task GetStyleUsageAsync_LayerDetailFailure_Skipped()
    {
        _fake.RespondGet("""{"styles":{"style":[{"name":"poi"}]}}""");
        _fake.RespondGet("""{"layers":{"layer":[{"name":"bad"},{"name":"good"}]}}""");
        _fake.RespondGetThrows(new InvalidOperationException("boom"));
        _fake.RespondGet("""{"layer":{"name":"good","defaultStyle":{"name":"poi","href":"http://h/rest/styles/poi.json"}}}""");

        var usages = await _svc.GetStyleUsageAsync();

        Assert.Single(usages);
        Assert.True(usages[0].IsUsed);
        Assert.Single(usages[0].Layers);
        Assert.Equal("good", usages[0].Layers[0]);
    }

    [Fact]
    public async Task GetStyleUsageAsync_NullDetail_Skipped()
    {
        _fake.RespondGet("""{"styles":{"style":[{"name":"poi"}]}}""");
        _fake.RespondGet("""{"layers":{"layer":[{"name":"l1"}]}}""");
        _fake.RespondGet("{}");

        var usages = await _svc.GetStyleUsageAsync();

        Assert.Single(usages);
        Assert.False(usages[0].IsUsed);
    }

    [Fact]
    public async Task GetStyleUsageAsync_NullDefaultStyle_Skipped()
    {
        _fake.RespondGet("""{"styles":{"style":[{"name":"poi"}]}}""");
        _fake.RespondGet("""{"layers":{"layer":[{"name":"l1"}]}}""");
        _fake.RespondGet("""{"layer":{"name":"l1"}}""");

        var usages = await _svc.GetStyleUsageAsync();

        Assert.Single(usages);
        Assert.False(usages[0].IsUsed);
    }

    [Fact]
    public async Task GetStyleUsageAsync_CaseSensitiveNameMatch()
    {
        // 服务端资源名大小写敏感（Ordinal）：POI 不匹配全局样式 poi
        _fake.RespondGet("""{"styles":{"style":[{"name":"poi"}]}}""");
        _fake.RespondGet("""{"layers":{"layer":[{"name":"l1"}]}}""");
        _fake.RespondGet("""{"layer":{"name":"l1","defaultStyle":{"name":"POI","href":"http://h/rest/styles/POI.json"}}}""");

        var usages = await _svc.GetStyleUsageAsync();

        Assert.False(usages[0].IsUsed);
    }

    [Fact]
    public async Task GetStyleUsageAsync_DuplicateStyleNames_Deduplicated()
    {
        _fake.RespondGet("""{"styles":{"style":[{"name":"poi"},{"name":"poi"}]}}""");
        _fake.RespondGet("""{"layers":{"layer":[{"name":"l1"}]}}""");
        _fake.RespondGet("""{"layer":{"name":"l1","defaultStyle":{"name":"poi","href":"http://h/rest/styles/poi.json"}}}""");

        var usages = await _svc.GetStyleUsageAsync();

        Assert.Single(usages);
        Assert.Equal("poi", usages[0].StyleName);
        Assert.Equal("l1", usages[0].Layers[0]);
    }

    [Fact]
    public async Task GetStyleUsageAsync_EmptyNames_Skipped()
    {
        _fake.RespondGet("""{"styles":{"style":[{"name":""},{"name":"poi"}]}}""");
        _fake.RespondGet("""{"layers":{"layer":[{"name":""},{"name":"l1"}]}}""");
        _fake.RespondGet("""{"layer":{"name":"l1"}}""");

        var usages = await _svc.GetStyleUsageAsync();

        // 空图层名不发详情请求：样式列表 + 图层列表 + l1 详情 = 3 次
        Assert.Equal(3, _fake.Requests.Count);
        Assert.Single(usages);
        Assert.Equal("poi", usages[0].StyleName);
    }

    [Fact]
    public async Task GetUnusedStyleNamesAsync_ReturnsOnlyUnreferenced()
    {
        _fake.RespondGet("""{"styles":{"style":[{"name":"poi"},{"name":"roads"}]}}""");
        _fake.RespondGet("""{"layers":{"layer":[{"name":"l1"}]}}""");
        _fake.RespondGet("""{"layer":{"name":"l1","defaultStyle":{"name":"poi","href":"http://h/rest/styles/poi.json"}}}""");

        var unused = await _svc.GetUnusedStyleNamesAsync();

        Assert.Single(unused);
        Assert.Equal("roads", unused[0]);
    }
}
