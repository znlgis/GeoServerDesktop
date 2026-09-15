using System;
using System.IO;
using GeoServerDesktop.App.Services;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// App 层 SettingsService 离线测试。
    /// 使用 TESTABILITY 注入点 SettingsService.SettingsDirectoryOverride 指向随机临时目录，
    /// 绝不触碰用户真实 %APPDATA%/GeoServerDesktop（E33 现状的规避）。
    /// </summary>
    public sealed class AppSettingsServiceTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "gsd_settings_" + Guid.NewGuid().ToString("N"));

        public AppSettingsServiceTests()
        {
            SettingsService.SettingsDirectoryOverride = _dir;
        }

        public void Dispose()
        {
            SettingsService.SettingsDirectoryOverride = null;
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); } catch { }
        }

        private string SettingsFile => Path.Combine(_dir, "settings.json");

        [Fact]
        public async Task Load_NoFile_ReturnsDefaults()
        {
            var svc = new SettingsService();
            var s = await svc.LoadSettingsAsync();

            Assert.Equal(1200, s.WindowWidth);
            Assert.Equal(800, s.WindowHeight);
            Assert.False(s.RememberConnection);
            Assert.Null(s.LastBaseUrl);
            Assert.Null(s.LastUsername);
        }

        [Fact]
        public async Task SaveThenLoad_RoundTrips_ValuesAndSpecialChars()
        {
            var svc = new SettingsService();
            var original = new AppSettings
            {
                LastBaseUrl = "http://localhost:8765/geoserver",
                LastUsername = "用户_测试 <>\"&",   // 中文 + 特殊字符
                RememberConnection = true,
                WindowWidth = 1366.5,
                WindowHeight = 768
            };

            await svc.SaveSettingsAsync(original);
            Assert.True(File.Exists(SettingsFile)); // 落在临时目录，非 %APPDATA%

            var reloaded = await new SettingsService().LoadSettingsAsync();
            Assert.Equal(original.LastBaseUrl, reloaded.LastBaseUrl);
            Assert.Equal(original.LastUsername, reloaded.LastUsername);   // 中文/转义字符往返无损
            Assert.True(reloaded.RememberConnection);
            Assert.Equal(1366.5, reloaded.WindowWidth);
            Assert.Equal(768, reloaded.WindowHeight);
        }

        [Fact]
        public async Task Load_CorruptedJson_SilentlyReturnsDefaults()
        {
            Directory.CreateDirectory(_dir);
            await File.WriteAllTextAsync(SettingsFile, "{ this is not valid json ");

            var svc = new SettingsService();
            var s = await svc.LoadSettingsAsync(); // 异常被吞，静默回落默认

            Assert.Equal(1200, s.WindowWidth);
            Assert.Equal(800, s.WindowHeight);
            Assert.False(s.RememberConnection);
        }

        [Fact]
        public void OverrideNull_KeepsDefaultAppDataPath()
        {
            // 现状固化：未注入时目录为 %APPDATA%/GeoServerDesktop（无其它注入点）。
            SettingsService.SettingsDirectoryOverride = null;
            var expectedDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GeoServerDesktop");
            var expectedFile = Path.Combine(expectedDir, "settings.json");

            var svc = new SettingsService();
            // 通过私有字段确认路径解析（不写文件、只读反射）——避免实际落盘。
            var field = typeof(SettingsService).GetField("_settingsPath",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var actual = field!.GetValue(svc) as string;

            Assert.Equal(expectedFile, actual);
        }
    }
}
