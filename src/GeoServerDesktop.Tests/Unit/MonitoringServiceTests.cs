using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>MonitoringService 离线单元测试。FIXED-E7：requests 实测根为 org.geoserver.monitor.RequestDatas
/// （内层 org.geoserver.monitor.RequestData 数组、项仅 name/href 摘要），服务侧改用手工 Parse；
/// E6（statistics 形态完全不符）维持基线。</summary>
public class MonitoringServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly MonitoringService _svc;

    public MonitoringServiceTests() => _svc = new MonitoringService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new MonitoringService(null!));

    [Fact]
    public async Task GetRequestsAsync_ParsesDynamicClassRoot_FIXED_E7()
    {
        // 3.0.1 实测形态：动态类名键 + {name(数字 id),href} 摘要
        _fake.RespondGet("""{"org.geoserver.monitor.RequestDatas":{"org.geoserver.monitor.RequestData":[{"name":10784,"href":"http://x/rest/monitor/requests/10784.json"},{"name":10785,"href":"http://x/rest/monitor/requests/10785.json"}]}}""");

        var w = await _svc.GetRequestsAsync();

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/monitor/requests.json", _fake.Last.Path);
        Assert.Equal(2, w.Requests.Count);
        Assert.Equal(10784, w.Requests[0].Id);
        Assert.Equal("http://x/rest/monitor/requests/10784.json", w.Requests[0].Href);

        // 空态容错（无数据时值可为 ""）
        _fake.RespondGet("""{"org.geoserver.monitor.RequestDatas":""}""");
        var empty = await _svc.GetRequestsAsync();
        Assert.Empty(empty.Requests);
    }

    [Fact]
    public async Task GetStatisticsAsync_RealGeoServerShapeYieldsAllDefaults_KNOWN_ISSUE_E6()
    {
        // KNOWN-ISSUE E6（维持）：statistics.json 实为 {"statistics":{"stat":[...]}}，
        // 与模型 {totalRequests,avgResponseTime,byPath} 完全不符 → 静默解析为全零/null。
        _fake.RespondGet("""{"statistics":{"stat":[{"name":"memory.total","value":"2076164096"}]}}""");

        var s = await _svc.GetStatisticsAsync();

        Assert.Equal("/rest/monitor/statistics.json", _fake.Last!.Path);
        Assert.Equal(0, s.TotalRequests);
        Assert.Equal(0.0, s.AvgResponseTime);
        Assert.Null(s.ByPath);
    }

    [Fact]
    public async Task GetStatisticsAsync_ModelShapedJsonParses()
    {
        _fake.RespondGet("""{"totalRequests":100,"avgResponseTime":12.5,"byPath":{"/wms":{"count":90,"avgTime":11,"totalBytes":9999}}}""");

        var s = await _svc.GetStatisticsAsync();

        Assert.Equal(100, s.TotalRequests);
        Assert.Equal(12.5, s.AvgResponseTime);
        Assert.Equal(90, s.ByPath["/wms"].Count);
        Assert.Equal(9999, s.ByPath["/wms"].TotalBytes);
    }

    [Fact]
    public void NoDataCrumbOrToggleMethods_CurrentSurfacePin()
    {
        // 清单：无 data/crumb 资源方法、无 PUT 启停监控（缺功能）
        Assert.Null(typeof(MonitoringService).GetMethod("GetDataAsync"));
        Assert.Null(typeof(MonitoringService).GetMethod("EnableAsync"));
    }
}
