using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>AboutService 离线单元测试。ResourceInfo 键为 "@name"/"Version"(大写 V)/"Build-Timestamp"/"Git-Revision"。</summary>
public class AboutServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly AboutService _svc;

    public AboutServiceTests() => _svc = new AboutService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new AboutService(null!));

    [Fact]
    public async Task GetVersionAsync_ParsesAboutResourceArrayWithStringVersion_KNOWN_ISSUE_E39_PIN()
    {
        // KNOWN-ISSUE E39（部分固化）：模型 ResourceInfo.Version 为 string，且键为大写 "Version"。
        // GeoServer 2.2x 实测 version.json 中 Version 为字符串时正常解析；
        // 若某些版本返回 {"#text":"3.0.1"} 对象形态则会反序列化失败（需集成层按实测验证）。
        _fake.RespondGet("""{"about":{"resource":[{"@name":"GeoServer","Version":"2.24.0","Build-Timestamp":"2024-01-01T00:00:00","Git-Revision":"abc123","Git-Branch":"origin/2.24.x"},{"@name":"GeoTools","Version":"30.0"},{"@name":"GeoWebCache","Version":"1.24.0"}]}}""");

        var w = await _svc.GetVersionAsync();

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/about/version.json", _fake.Last.Path);
        Assert.Equal(3, w.About.Resources.Length);
        Assert.Equal("GeoServer", w.About.Resources[0].Name); // @name
        Assert.Equal("2.24.0", w.About.Resources[0].Version); // 大写 Version 键
        Assert.Equal("abc123", w.About.Resources[0].GitRevision); // Git-Revision 键
        Assert.Equal("30.0", w.About.Resources[1].Version);
    }

    [Fact]
    public async Task GetManifestsAsync_ParsesManifestArray()
    {
        // 清单形态与版本形态同为 about.resource 双层，但内层键为小写 name/version
        _fake.RespondGet("""{"about":{"resource":[{"name":"AppSchema Data Store","version":"2.24.0","Build-Timestamp":"x","Git-Revision":"y"},{"name":"WMS","version":"2.24.0"}]}}""");

        var w = await _svc.GetManifestsAsync();

        Assert.Equal("/rest/about/manifests.json", _fake.Last!.Path);
        Assert.Equal("AppSchema Data Store", w.About.Resources[0].Name);
        Assert.Equal("2.24.0", w.About.Resources[0].Version);
    }

    [Fact]
    public async Task GetSystemStatusAsync_ParsesMetricsMetricArray()
    {
        _fake.RespondGet("""{"metrics":{"metric":[{"available":true,"description":"Total memory","name":"memory.total","unit":"bytes","category":"memory","identifier":"memory.total","priority":3,"value":2076164096},{"available":true,"description":"free","name":"memory.free","unit":"bytes","category":"memory","identifier":"memory.free","priority":3,"value":1000}]}}""");

        var w = await _svc.GetSystemStatusAsync();

        Assert.Equal("/rest/about/system-status.json", _fake.Last!.Path);
        Assert.Equal(2, w.Metrics.MetricArray.Length);
        var m = w.Metrics.MetricArray[0];
        Assert.True(m.Available);
        Assert.Equal("memory.total", m.Name);
        Assert.Equal("bytes", m.Unit);
        Assert.Equal("memory", m.Category);
        Assert.Equal(3, m.Priority);
        Assert.NotNull(m.Value); // object 类型承接任意 JSON 值
    }
}
