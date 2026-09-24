using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// 欢迎页仪表盘（DashboardViewModel）无头测试：
    /// 离线守卫/死服务容错/断开复位 + 连真 GeoServer 的自动刷新与版本、计数交叉校验（VmRest 独立解析）。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class DashboardViewModelTests : GeoServerTestBase
    {
        public DashboardViewModelTests(GeoServerFixture fx) : base(fx) { }

        // ---------- 离线（不依赖服务器） ----------

        [Fact]
        public async Task Defaults_NotConnected_UsesLocalizedStatus()
        {
            var conn = new GeoServerConnectionService();
            var vm = new DashboardViewModel(conn);

            Assert.False(vm.IsConnected);
            Assert.Equal(string.Empty, vm.ServerUrl);
            Assert.Equal(DashboardViewModel.ValuePlaceholder, vm.GeoServerVersion);
            Assert.Null(vm.WorkspaceCount);
            Assert.Null(vm.LayerCount);
            Assert.Equal(DashboardViewModel.ValuePlaceholder, vm.WorkspaceCountText);
            Assert.Equal(DashboardViewModel.ValuePlaceholder, vm.LayerCountText);
            Assert.Equal(vm.L.StatusNotConnected, vm.StatusMessage);

            await vm.RefreshCommand.ExecuteAsync(null); // 未连接守卫：不加载、状态回显

            Assert.Equal(vm.L.StatusNotConnected, vm.StatusMessage);
            Assert.False(vm.IsLoading);
        }

        [Fact]
        public async Task Refresh_DeadServer_IsCaught_NotFaulted()
        {
            var conn = new GeoServerConnectionService();
            conn.Connect(new GeoServerClientOptions { BaseUrl = "http://127.0.0.1:1/", Username = "a", Password = "b" });
            var vm = new DashboardViewModel(conn); // 连接后创建：不触发自动刷新，只测显式命令
            try
            {
                await vm.RefreshCommand.ExecuteAsync(null);

                Assert.False(vm.IsLoading);
                var failPrefix = string.Format(vm.L.StatusDashboardLoadFailed, "");
                Assert.StartsWith(failPrefix, vm.StatusMessage);
                Assert.False(vm.RefreshCommand.IsRunning); // 未抛到命令外层
                Assert.Equal(DashboardViewModel.ValuePlaceholder, vm.GeoServerVersion); // 版本失败仅回落占位
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public async Task Connect_DeadServer_SyncsState_AndAutoRefreshFailsGracefully()
        {
            var conn = new GeoServerConnectionService();
            var vm = new DashboardViewModel(conn);
            try
            {
                conn.Connect(new GeoServerClientOptions { BaseUrl = "http://127.0.0.1:1/", Username = "a", Password = "b" });

                // 连接事件同步置位
                Assert.True(vm.IsConnected);
                Assert.Equal("http://127.0.0.1:1/", vm.ServerUrl);

                // 自动刷新（即发即忘）对死服务容错：等待其结束，状态为加载失败
                var failPrefix = string.Format(vm.L.StatusDashboardLoadFailed, "");
                Assert.True(await WaitUntilAsync(() => !vm.IsLoading && vm.StatusMessage.StartsWith(failPrefix)),
                    $"等待自动刷新完成超时；StatusMessage={vm.StatusMessage}");
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public async Task Disconnect_ResetsOverview()
        {
            var conn = new GeoServerConnectionService();
            var vm = new DashboardViewModel(conn);
            conn.Connect(new GeoServerClientOptions { BaseUrl = "http://127.0.0.1:1/", Username = "a", Password = "b" });
            Assert.True(vm.IsConnected);
            var failPrefix = string.Format(vm.L.StatusDashboardLoadFailed, "");
            Assert.True(await WaitUntilAsync(() => !vm.IsLoading && vm.StatusMessage.StartsWith(failPrefix)),
                $"等待自动刷新完成超时；StatusMessage={vm.StatusMessage}");

            conn.Disconnect();

            Assert.False(vm.IsConnected);
            Assert.Equal(string.Empty, vm.ServerUrl);
            Assert.Equal(DashboardViewModel.ValuePlaceholder, vm.GeoServerVersion);
            Assert.Null(vm.WorkspaceCount);
            Assert.Null(vm.LayerCount);
            Assert.Equal(DashboardViewModel.ValuePlaceholder, vm.WorkspaceCountText);
            Assert.Equal(vm.L.StatusNotConnected, vm.StatusMessage);
        }

        // ---------- 集成（连真 GeoServer） ----------

        [Fact]
        public async Task Connect_LiveServer_AutoRefreshesOverview_MatchesBareRest()
        {
            if (!RequireGeoServer()) return;
            var conn = new GeoServerConnectionService();
            var vm = new DashboardViewModel(conn);
            try
            {
                conn.Connect(GeoServerAvailability.Options());
                Assert.True(vm.IsConnected);
                Assert.Equal(TestEnv.BaseUrl, vm.ServerUrl);

                Assert.True(await WaitUntilAsync(() => !vm.IsLoading && vm.StatusMessage == vm.L.StatusDashboardLoaded),
                    $"等待仪表盘自动刷新完成超时；StatusMessage={vm.StatusMessage}");

                // 版本交叉校验（独立解析裸 REST）
                var independent = VmRest.GeoServerVersion(VmRest.Get("/rest/about/version.json"));
                Assert.NotNull(independent);
                Assert.Equal(independent, vm.GeoServerVersion);

                // 计数交叉校验（独立统计裸 REST）
                Assert.NotNull(vm.WorkspaceCount);
                Assert.True(vm.WorkspaceCount > 0);
                Assert.Equal(VmRest.WorkspaceCount(), vm.WorkspaceCount);
                Assert.NotNull(vm.LayerCount);
                Assert.True(vm.LayerCount > 0);
                Assert.Equal(VmRest.LayerCount(), vm.LayerCount);

                // 速览文本与数值一致
                Assert.Equal(vm.WorkspaceCount.ToString(), vm.WorkspaceCountText);
                Assert.Equal(vm.LayerCount.ToString(), vm.LayerCountText);
            }
            finally { conn.Disconnect(); }
        }

        private static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs = 15000)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (condition()) return true;
                await Task.Delay(100);
            }
            return condition();
        }
    }
}
