using System;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// GeoServerConnectionService 离线测试（不启动 UI、不触网）。
    /// 覆盖：Connect 参数校验、连接/断开事件与幂等、18 个 Get*Service 未连接守卫、E32 无探测固化。
    /// </summary>
    public sealed class ConnectionServiceTests
    {
        private static GeoServerClientOptions Opts(string url = "http://localhost:8765/geoserver")
            => new GeoServerClientOptions { BaseUrl = url, Username = "admin", Password = "geoserver" };

        [Fact]
        public void Connect_NullOptions_ThrowsArgumentNullException()
        {
            var svc = new GeoServerConnectionService();
            Assert.Throws<ArgumentNullException>(() => svc.Connect(null!));
            Assert.False(svc.IsConnected);
        }

        [Fact]
        public void Connect_SetsIsConnectedAndRaisesTrueEvent()
        {
            var svc = new GeoServerConnectionService();
            bool? raised = null;
            svc.ConnectionStatusChanged += (s, connected) => raised = connected;

            svc.Connect(Opts());

            Assert.True(svc.IsConnected);
            Assert.NotNull(svc.CurrentOptions);
            Assert.True(raised);
        }

        [Fact]
        public void Disconnect_IsIdempotent_AndRaisesFalseEvent()
        {
            var svc = new GeoServerConnectionService();
            int falseCount = 0;
            svc.ConnectionStatusChanged += (s, connected) => { if (!connected) falseCount++; };

            // 未连接也可安全断开（幂等，且首次断开也触发 false 事件）
            svc.Disconnect();
            Assert.False(svc.IsConnected);
            Assert.Equal(1, falseCount);

            svc.Connect(Opts());
            svc.Disconnect();
            svc.Disconnect(); // 再次断开不报错，仍触发 false 事件（无条件）
            Assert.False(svc.IsConnected);
            Assert.Null(svc.CurrentOptions);
            // Connect 内部先 Disconnect()（多发一次 false）：initial(1)+Connect内部(2)+Disconnect(3)+Disconnect(4)
            Assert.Equal(4, falseCount);
        }

        [Fact]
        public void WhenNotConnected_AllGetServiceFactories_ThrowInvalidOperationException()
        {
            var svc = new GeoServerConnectionService();
            const string msg = "Not connected to a GeoServer instance";

            // 18 个服务工厂，逐个断言异常类型与消息
            Action[] factories =
            {
                () => svc.GetWorkspaceService(),
                () => svc.GetDataStoreService(),
                () => svc.GetLayerService(),
                () => svc.GetStyleService(),
                () => svc.GetLayerGroupService(),
                () => svc.GetFeatureTypeService(),
                () => svc.GetPreviewService(),
                () => svc.GetAboutService(),
                () => svc.GetGlobalSettingsService(),
                () => svc.GetLoggingService(),
                () => svc.GetWMSSettingsService(),
                () => svc.GetWFSSettingsService(),
                () => svc.GetWCSSettingsService(),
                () => svc.GetDiskQuotaService(),
                () => svc.GetGridsetService(),
                () => svc.GetSecurityService(),
                () => svc.GetUserGroupService(),
                () => svc.GetRoleService(),
            };

            Assert.Equal(18, factories.Length);
            foreach (var f in factories)
            {
                var ex = Assert.Throws<InvalidOperationException>(() => f());
                Assert.Equal(msg, ex.Message);
            }
        }

        [Fact]
        public void WhenConnected_GetServiceFactories_DoNotThrow()
        {
            var svc = new GeoServerConnectionService();
            svc.Connect(Opts());
            try
            {
                // 工厂仅 EnsureConnected，不发 HTTP；应能正常返回实例
                Assert.NotNull(svc.GetWorkspaceService());
                Assert.NotNull(svc.GetRoleService());
            }
            finally { svc.Disconnect(); }
        }

        [Fact]
        public void Connect_BadUrl_DoesNotProbe_ReportsConnected_KNOWNISSUE_E32()
        {
            // 现状固化：Connect 仅 new factory，不发任何连通性探测，错误 URL 也判定“连接成功”。
            var svc = new GeoServerConnectionService();
            svc.Connect(Opts("http://127.0.0.1:9/geoserver")); // 无监听端口

            Assert.True(svc.IsConnected);          // E32：错误 URL 仍“连接成功”
            Assert.NotNull(svc.CurrentOptions);
            // 失败要到后续命令（真正发 HTTP）才显现——此处不触网，故不探测即无异常。
            Assert.NotNull(svc.GetAboutService()); // 工厂本身不抛（EnsureConnected 通过）
            svc.Disconnect();
        }
    }
}
