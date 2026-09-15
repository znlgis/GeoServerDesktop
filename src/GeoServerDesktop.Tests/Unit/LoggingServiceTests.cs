using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>LoggingService 离线单元测试。LoggingSettings 模型自身即根包装 {"logging":{...}}。</summary>
public class LoggingServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly LoggingService _svc;

    public LoggingServiceTests() => _svc = new LoggingService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new LoggingService(null!));

    [Fact]
    public async Task GetLoggingSettingsAsync_ParsesLoggingRoot()
    {
        _fake.RespondGet("""{"logging":{"level":"DEFAULT_LOGGING","location":"logs/geoserver.log","stdOutLogging":false,"fileLogging":true}}""");

        var s = await _svc.GetLoggingSettingsAsync();

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/logging.json", _fake.Last.Path);
        Assert.Equal("DEFAULT_LOGGING", s.Logging.Level);
        Assert.Equal("logs/geoserver.log", s.Logging.Location);
        Assert.False(s.Logging.StdOutLogging);
        Assert.True(s.Logging.FileLogging);
    }

    [Fact]
    public async Task UpdateLoggingSettingsAsync_PutsSelfRootedLoggingBody()
    {
        // PUT body 由 LoggingSettings 模型自带 "logging" 根
        var settings = new LoggingSettings
        {
            Logging = new LoggingConfig { Level = "VERBOSE_LOGGING", Location = "logs/geoserver.log", StdOutLogging = true, FileLogging = false }
        };
        await _svc.UpdateLoggingSettingsAsync(settings);

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/logging", _fake.Last.Path);
        var logging = Json.P(_fake.Last.Body)["logging"];
        Assert.Equal("VERBOSE_LOGGING", (string?)logging?["level"]);
        Assert.True((bool?)logging?["stdOutLogging"]);
        Assert.False((bool?)logging?["fileLogging"]);
    }
}
