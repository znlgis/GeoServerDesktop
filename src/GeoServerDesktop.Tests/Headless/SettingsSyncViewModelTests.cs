using System;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.Tests.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// SettingsSyncViewModel 无头测试（M4）：源连接表单驱动的读-比-应用命令流状态机——
    /// 初始同实例无差异 → 源侧扰动（裸 REST 整包 PUT 改 contact.addressCity）→ 重读比对
    /// （同实例两侧同步变化，仍一致，证明两侧读取通道独立）→ 恢复基线 → 无差异。
    /// 差异计算与选择性应用的精确语义由 L1 SettingsCompareTests 与 L2 SettingsSyncIT 覆盖。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class SettingsSyncViewModelTests : GeoServerTestBase
    {
        public SettingsSyncViewModelTests(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public async Task Compare_Diverge_And_Restore_CommandFlow()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new SettingsSyncViewModel(conn);
            string marker = "gdtest_vmsync_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            string? originalJson = null;
            try
            {
                vm.SourceUrl = TestEnv.BaseUrl;
                vm.SourceUser = TestEnv.User;
                vm.SourcePassword = TestEnv.Pass;

                await vm.CompareCommand.ExecuteAsync(null);
                Assert.Empty(vm.Diffs);
                Assert.Equal(vm.L.SyncStatusIdentical, vm.StatusMessage);

                originalJson = VmRest.Get("/rest/settings.json");
                Assert.NotNull(originalJson);
                var originalCity = (string)JObject.Parse(originalJson!)["global"]!["settings"]!["contact"]!["addressCity"];

                // 源侧扰动（裸 REST，不经 VM）
                var parsed = JObject.Parse(originalJson!);
                parsed["global"]!["settings"]!["contact"]!["addressCity"] = marker;
                Assert.Equal(200, VmRest.PutJson("/rest/settings", parsed.ToString(Formatting.None)));
                try
                {
                    // 两侧同实例：比对仍应一致（读取通道独立且都看到扰动值）
                    await vm.CompareCommand.ExecuteAsync(null);
                    Assert.Empty(vm.Diffs);
                    var echo = JObject.Parse(VmRest.Get("/rest/settings.json")!);
                    Assert.Equal(marker, (string)echo["global"]!["settings"]!["contact"]!["addressCity"]);
                }
                finally
                {
                    Assert.Equal(200, VmRest.PutJson("/rest/settings", originalJson!));
                }

                await vm.CompareCommand.ExecuteAsync(null);
                Assert.Empty(vm.Diffs);
                var restored = JObject.Parse(VmRest.Get("/rest/settings.json")!);
                Assert.Equal(originalCity, (string)restored["global"]!["settings"]!["contact"]!["addressCity"]);
            }
            finally
            {
                try
                {
                    if (originalJson != null) VmRest.PutJson("/rest/settings", originalJson);
                }
                catch { }
                conn.Disconnect();
            }
        }

        [Fact]
        public async Task Compare_WithoutUrl_ReportsGuidance()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new SettingsSyncViewModel(conn);
            try
            {
                vm.SourceUrl = "";
                await vm.CompareCommand.ExecuteAsync(null);
                Assert.Equal(vm.L.SyncStatusNeedUrl, vm.StatusMessage);
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public async Task Apply_WithoutCompare_ReportsGuidance()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new SettingsSyncViewModel(conn);
            try
            {
                await vm.ApplySelectedCommand.ExecuteAsync(null);
                Assert.Equal(vm.L.SyncStatusCompareFirst, vm.StatusMessage);
            }
            finally { conn.Disconnect(); }
        }
    }
}
