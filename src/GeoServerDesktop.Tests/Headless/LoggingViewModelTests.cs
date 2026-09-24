using System;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// 日志查看器（LoggingViewModel 的 tail/文件查看功能）无头测试：
    /// 离线守卫 + TakeLastLines/CountLines 边界 + 连真 GeoServer 的读取/截取/裸 REST 交叉校验。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class LoggingViewModelTests : GeoServerTestBase
    {
        public LoggingViewModelTests(GeoServerFixture fx) : base(fx) { }

        // ---------- 离线（不依赖服务器） ----------

        [Fact]
        public async Task RefreshLog_NotConnected_UsesLocalizedStatus()
        {
            var conn = new GeoServerConnectionService();
            var vm = new LoggingViewModel(conn);

            await vm.RefreshLogCommand.ExecuteAsync(null);

            Assert.Equal(vm.L.StatusNotConnected, vm.StatusMessage);
            Assert.False(vm.IsLogLoading);
            Assert.Equal(string.Empty, vm.LogContent);
        }

        [Fact]
        public async Task RefreshLog_DeadServer_IsCaught_NotFaulted()
        {
            var conn = new GeoServerConnectionService();
            conn.Connect(new GeoServerClientOptions { BaseUrl = "http://127.0.0.1:1/", Username = "a", Password = "b" });
            var vm = new LoggingViewModel(conn);
            try
            {
                await vm.RefreshLogCommand.ExecuteAsync(null);

                Assert.False(vm.IsLogLoading);
                var failPrefix = string.Format(vm.L.StatusLogFileLoadFailed, "");
                Assert.StartsWith(failPrefix, vm.StatusMessage);
                Assert.False(vm.RefreshLogCommand.IsRunning); // 未抛到命令外层
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public void TakeLastLines_EdgeCases()
        {
            Assert.Equal(string.Empty, LoggingViewModel.TakeLastLines("", 10));
            Assert.Equal(string.Empty, LoggingViewModel.TakeLastLines("a\nb", 0));

            // 行数不足或恰好 → 原文
            Assert.Equal("a\nb", LoggingViewModel.TakeLastLines("a\nb", 10));
            Assert.Equal("a\nb\nc", LoggingViewModel.TakeLastLines("a\nb\nc", 3));

            // 超出 → 尾部（保留原换行结构）
            Assert.Equal("b\nc\n", LoggingViewModel.TakeLastLines("a\nb\nc\n", 2));
            Assert.Equal("c\n", LoggingViewModel.TakeLastLines("a\nb\nc\n", 1));
            Assert.Equal("b\nc", LoggingViewModel.TakeLastLines("a\nb\nc", 2));

            // 单行
            Assert.Equal("abc", LoggingViewModel.TakeLastLines("abc", 1));
        }

        [Fact]
        public void CountLines_EdgeCases()
        {
            Assert.Equal(0, LoggingViewModel.CountLines(""));
            Assert.Equal(1, LoggingViewModel.CountLines("a"));
            Assert.Equal(1, LoggingViewModel.CountLines("a\n"));
            Assert.Equal(3, LoggingViewModel.CountLines("a\nb\nc"));
            Assert.Equal(3, LoggingViewModel.CountLines("a\nb\nc\n"));
            Assert.Equal(3, LoggingViewModel.CountLines("a\n\nb\n")); // 中间空行计入
        }

        // ---------- 集成（连真 GeoServer） ----------

        [Fact]
        public async Task RefreshLog_LoadsTail_AndMatchesBareRest()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new LoggingViewModel(conn);
            try
            {
                await vm.RefreshLogCommand.ExecuteAsync(null);

                // 状态与资源路径（REST 未返回 location 时为默认 logs/geoserver.log）
                var lineCount = LoggingViewModel.CountLines(vm.LogContent);
                Assert.Equal(string.Format(vm.L.StatusLogFileLoaded, lineCount), vm.StatusMessage);
                Assert.False(vm.IsLogLoading);
                Assert.False(string.IsNullOrEmpty(vm.LogResourcePath));

                // 内容非空，且尾部行数不超过上限（截取生效）
                Assert.False(string.IsNullOrEmpty(vm.LogContent));
                Assert.True(lineCount <= LoggingViewModel.MaxLogLines,
                    $"尾部行数 {lineCount} 超过上限 {LoggingViewModel.MaxLogLines}");

                // 交叉校验：裸 REST 读原始内容，VM 尾部应与其存在重叠行（日志为追加语义；
                // 容忍两次读取间的日志增长/轮转，只要存在一行交集即证明读的是同一日志文件）
                var raw = VmRest.Get("/rest/resource/logs/geoserver.log");
                Assert.NotNull(raw);
                var rawLines = raw!.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 10).ToHashSet();
                var overlap = vm.LogContent.Split('\n').Select(l => l.Trim())
                    .Count(l => l.Length > 10 && rawLines.Contains(l));
                Assert.True(overlap > 0, "VM 日志尾部与裸 REST 内容无任何重叠行");
            }
            finally { conn.Disconnect(); }
        }
    }
}
