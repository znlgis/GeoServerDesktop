using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>DiskQuotaService (GWC) 离线单元测试。FIXED-E13：diskquota 端点（DiskQuotaController）与 Accept 无关，
/// 恒返回/接收 XML（根 org.geowebcache.diskquota.DiskQuotaConfig），服务侧以 XDocument 做模型读写往返。</summary>
public class DiskQuotaServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly DiskQuotaService _svc;

    public DiskQuotaServiceTests() => _svc = new DiskQuotaService(_fake);

    private const string RealXml =
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <org.geowebcache.diskquota.DiskQuotaConfig>
          <enabled>true</enabled>
          <cacheCleanUpFrequency>5</cacheCleanUpFrequency>
          <cacheCleanUpUnits>SECONDS</cacheCleanUpUnits>
          <maxConcurrentCleanUps>2</maxConcurrentCleanUps>
          <globalExpirationPolicyName>LFU</globalExpirationPolicyName>
          <globalQuota>
            <id>0</id>
            <bytes>21474836480</bytes>
          </globalQuota>
          <quotaStore>HSQL</quotaStore>
        </org.geowebcache.diskquota.DiskQuotaConfig>
        """;

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new DiskQuotaService(null!));

    [Fact]
    public async Task GetDiskQuotaAsync_MapsRealXml_FIXED_E13()
    {
        _fake.RespondGet(RealXml);

        var c = await _svc.GetDiskQuotaAsync();

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/gwc/rest/diskquota", _fake.Last.Path); // 无 .json 后缀
        Assert.True(c.Enabled);
        Assert.Equal(5, c.CacheCleanUpFrequency);
        Assert.Equal("SECONDS", c.CacheCleanUpUnits);
        Assert.Equal(2, c.MaxConcurrentCleanUps);
        Assert.Equal("LFU", c.GlobalExpirationPolicyName);
        Assert.Equal(21474836480, c.GlobalQuota.Bytes);
        Assert.Equal("HSQL", c.QuotaStore);
    }

    [Fact]
    public async Task UpdateDiskQuotaAsync_PutsXmlBody_FIXED_E13()
    {
        _fake.RespondGet(RealXml);
        var cfg = await _svc.GetDiskQuotaAsync(); // 读→改→写往返
        cfg.Enabled = false;
        await _svc.UpdateDiskQuotaAsync(cfg);

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/gwc/rest/diskquota", _fake.Last.Path);
        Assert.Contains("application/xml", _fake.Last.ContentType);
        var body = _fake.Last.Body;
        Assert.Contains("<org.geowebcache.diskquota.DiskQuotaConfig>", body);
        Assert.Contains("<enabled>false</enabled>", body);
        Assert.Contains("<cacheCleanUpUnits>SECONDS</cacheCleanUpUnits>", body);
        Assert.Contains("<bytes>21474836480</bytes>", body);
        Assert.Contains("<quotaStore>HSQL</quotaStore>", body);
    }
}
