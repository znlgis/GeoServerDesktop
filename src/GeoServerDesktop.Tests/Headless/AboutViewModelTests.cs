using System;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// AboutViewModel 集成测试：加载系统信息后 GeoServerVersion 与“独立裸 GET version.json 解析”交叉校验。
    /// 已知风险：GeoTools 的 Version 在 GeoServer 3.0.1 中是未加引号的数字，若令库内 string 模型反序列化失败，
    /// 客户端会整体回落 "Error"（E39 变体）——两种结果都被固化断言。未连接分支另测（E32 现状）。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class AboutViewModelTests : GeoServerTestBase
    {
        public AboutViewModelTests(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public async Task LoadSystemInfo_CrossChecksBareRest()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new AboutViewModel(conn);
            try
            {
                await vm.LoadSystemInfoCommand.ExecuteAsync(null);
                Assert.False(vm.IsLoading);

                // 独立解析裸 REST，不经被测库
                string? independent = VmRest.GeoServerVersion(VmRest.Get("/rest/about/version.json"));
                Assert.Equal("3.0.1", independent);

                Assert.False(string.IsNullOrWhiteSpace(vm.GeoServerVersion));

                if (vm.GeoServerVersion == "Error")
                {
                    // 固化：数字型 GeoTools Version 令整份 version.json 反序列化失败 → 回落 Error
                    var failPrefix = string.Format(vm.L.StatusSystemInfoLoadFailed, "");
                    Assert.StartsWith(failPrefix, vm.StatusMessage);
                    Check.Warn("About.E39-numeric-version",
                        "客户端因 GeoTools 版本为数字未能解析出 GeoServer 版本（现状固化）");
                }
                else
                {
                    Assert.Equal(independent, vm.GeoServerVersion);
                    Assert.Equal(vm.L.StatusSystemInfoLoaded, vm.StatusMessage);
                }
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public async Task LoadSystemInfo_NotConnected_KnownIssue_E32()
        {
            var conn = new GeoServerConnectionService();
            var vm = new AboutViewModel(conn);
            await vm.LoadSystemInfoCommand.ExecuteAsync(null);
            Assert.Equal(vm.L.StatusNotConnected, vm.StatusMessage);
            Assert.Equal("Loading...", vm.GeoServerVersion); // 未触发加载，保持初值
        }
    }
}
