using GeoServerDesktop.GeoServerClient.Services;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>LogResourcePathResolver 离线单元测试：各类日志位置形态 → data_dir 相对资源路径。</summary>
public class LogResourcePathResolverTests
{
    [Theory]
    [InlineData("/data/geoserver/data_dir/logs/geoserver.log", "logs/geoserver.log")]
    [InlineData("logs/geoserver.log", "logs/geoserver.log")]
    [InlineData("C:/geo/data_dir/logs/geoserver.log", "logs/geoserver.log")]
    [InlineData(@"C:\geo\data_dir\logs\geoserver.log", "logs/geoserver.log")]
    [InlineData("/opt/gs/logs/gs.log", "logs/gs.log")]
    [InlineData("data_dir/logs/x.log", "logs/x.log")]
    [InlineData("  /data/gs/logs/x.log  ", "logs/x.log")]
    public void Resolve_LogFilePaths_ReturnsDataDirRelativePath(string input, string expected) =>
        Assert.Equal(expected, LogResourcePathResolver.Resolve(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/var/log/geoserver.log")]         // 单数 log 段，不在数据目录 logs 内
    [InlineData("/data/geoserver/data_dir/logs/")] // 仅目录（无文件名）
    [InlineData("logs/")]                          // 仅目录（无文件名）
    [InlineData("other/geoserver.log")]            // 无 logs 段
    [InlineData("geoserver.log")]                  // 裸文件名
    public void Resolve_UnresolvableLocations_ReturnsNull(string? input) =>
        Assert.Null(LogResourcePathResolver.Resolve(input));

    [Fact]
    public void DefaultLogResourcePath_IsDataDirRelative() =>
        Assert.Equal("logs/geoserver.log", LogResourcePathResolver.DefaultLogResourcePath);
}
