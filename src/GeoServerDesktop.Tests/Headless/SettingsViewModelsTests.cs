using System;
using System.Linq;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// 设置类 ViewModel 无头测试集合（WMS/WFS/WCS/Global/Logging/DiskQuota/Gridsets/
    /// CachingDefaults/Security）。离线用例不触网；集成用例连真 GeoServer 并与裸 REST 交叉校验；
    /// E36/E37 原 KNOWN-ISSUE 基线已随产品端修复翻转为 FIXED 断言（其余现状固化仍保留）。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class SettingsViewModelsTests : GeoServerTestBase
    {
        public SettingsViewModelsTests(GeoServerFixture fx) : base(fx) { }

        // ---------- 离线（不依赖服务器） ----------

        [Fact]
        public async Task CachingDefaults_ExplicitlyUnavailable_Fixed_E37()
        {
            // FIXED-E37 翻转：不再 Task.Delay 假装“加载/保存成功”。库层无 GWC defaults 端点
            // （实测 /gwc/rest/settings 404、/rest/settings.json 无 gwc 字段），Load/Save 均应
            // 显式提示不可配置；本用例连“死”服务且不触网（离线证明显式化实现同样不发包）。
            var conn = new GeoServerConnectionService();
            conn.Connect(new GeoServerClientOptions { BaseUrl = "http://127.0.0.1:1/", Username = "a", Password = "b" });
            var vm = new CachingDefaultsViewModel(conn);
            try
            {
                Assert.True(vm.IsEnabled); // 默认 true

                await vm.LoadSettingsCommand.ExecuteAsync(null);
                Assert.Equal(vm.L.StatusGwcNotConfigurable, vm.StatusMessage);
                Assert.NotEqual(vm.L.StatusCachingDefaultsLoaded, vm.StatusMessage);
                Assert.False(vm.IsLoading);

                await vm.SaveSettingsCommand.ExecuteAsync(null);
                Assert.Equal(vm.L.StatusGwcNotConfigurable, vm.StatusMessage);
                Assert.NotEqual(vm.L.StatusCachingDefaultsSaved, vm.StatusMessage);
                Assert.False(vm.IsLoading);
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public async Task Gridsets_DeleteNotConnected_IsCaught_NotFaulted_ActualVsE35()
        {
            // 现状修正：源码中 GetGridsetService() 位于 try 内，未连接时 InvalidOperationException
            // 被捕获为“删除失败”状态而非命令 fault（E35 描述与实际代码不符——见汇报“意外发现”）。
            var conn = new GeoServerConnectionService();
            var vm = new GridsetsViewModel(conn);

            vm.SelectedGridset = null;
            await vm.DeleteGridsetCommand.ExecuteAsync(null);
            Assert.Equal(vm.L.StatusNoGridsetSelected, vm.StatusMessage);

            vm.SelectedGridset = "some_gridset";
            await vm.DeleteGridsetCommand.ExecuteAsync(null);
            var failPrefix = string.Format(vm.L.StatusGridsetDeleteFailed, "");
            Assert.StartsWith(failPrefix, vm.StatusMessage);
            Assert.False(vm.IsLoading);
            Assert.False(vm.DeleteGridsetCommand.IsRunning); // 未抛到命令外层
        }

        [Fact]
        public async Task Security_NotConnected_UsesLocalizedStatus()
        {
            var conn = new GeoServerConnectionService();
            var vm = new SecuritySettingsViewModel(conn);
            await vm.LoadSettingsCommand.ExecuteAsync(null);
            Assert.Equal(vm.L.StatusNotConnected, vm.StatusMessage);
        }

        // ---------- 集成（连真 GeoServer） ----------

        [Fact]
        public async Task Wms_LoadBackfill_MatchesBareRest()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new WMSSettingsViewModel(conn);
            try
            {
                await vm.LoadSettingsCommand.ExecuteAsync(null);
                Assert.Equal(vm.L.StatusWmsSettingsLoaded, vm.StatusMessage);
                Assert.False(vm.IsLoading);

                var (name, enabled) = VmRest.WmsNameEnabled(VmRest.Get("/rest/services/wms/settings.json"));
                Assert.Equal(name, vm.ServiceName);
                Assert.Equal(enabled ?? false, vm.IsEnabled);
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public async Task Wfs_SaveRoundTrip_ChangesThenRestores()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new WFSSettingsViewModel(conn);
            long original = -1;
            try
            {
                await vm.LoadSettingsCommand.ExecuteAsync(null);
                Assert.Equal(vm.L.StatusWfsSettingsLoaded, vm.StatusMessage);
                original = vm.MaxFeatures;
                Assert.True(original > 0);

                long probe = original + 7;
                vm.MaxFeatures = (int)probe;
                await vm.SaveSettingsCommand.ExecuteAsync(null);

                if (vm.StatusMessage == vm.L.StatusWfsSettingsSaved)
                {
                    // 保存成功：裸 GET 校验变更，再复位
                    Assert.Equal(probe, VmRest.WfsMaxFeatures(VmRest.Get("/rest/services/wfs/settings.json")));
                    vm.MaxFeatures = (int)original;
                    await vm.SaveSettingsCommand.ExecuteAsync(null);
                }
                else
                {
                    // 现状固化：WFS settings 的 PUT 被 GeoServer 拒绝 → VM 报保存失败，服务器值不变
                    var failPrefix = string.Format(vm.L.StatusWfsSettingsSaveFailed, "");
                    Assert.StartsWith(failPrefix, vm.StatusMessage);
                    Assert.Equal(original, VmRest.WfsMaxFeatures(VmRest.Get("/rest/services/wfs/settings.json")));
                    Check.Warn("WFS.save", "WFSSettingsViewModel.SaveSettings 在 GeoServer 3.0.1 上 PUT 失败（现状固化）");
                }
                Assert.False(vm.IsLoading);
            }
            finally
            {
                // 双保险：无论中途结果如何，尽量把 maxFeatures 复位到原值（用同一连接）
                if (original > 0)
                {
                    vm.MaxFeatures = (int)original;
                    try { await vm.SaveSettingsCommand.ExecuteAsync(null); } catch { }
                }
                conn.Disconnect();
            }
        }

        [Fact]
        public async Task GlobalSettings_Load_BackfillsRealValues_FIXED()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new GlobalSettingsViewModel(conn);
            try
            {
                await vm.LoadSettingsCommand.ExecuteAsync(null);
                // FIXED：GlobalSettings 模型根已对齐实测形态 {"global":{"settings":{...}}}
                // （原根 "settings" 导致 Settings=null、Contact* 静默空回填的现状基线随之翻转），
                // 现应回填真实联系人与组织。
                Assert.Equal(vm.L.StatusGlobalSettingsLoaded, vm.StatusMessage);
                Assert.False(vm.IsLoading);
                var json = VmRest.Get("/rest/settings.json") ?? "";
                Assert.False(string.IsNullOrWhiteSpace(vm.ContactPerson));
                Assert.Contains("\"" + vm.ContactPerson + "\"", json);
                Assert.False(string.IsNullOrWhiteSpace(vm.ContactOrganization));
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public async Task Logging_LoadBackfill_MatchesBareRest()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new LoggingViewModel(conn);
            try
            {
                await vm.LoadSettingsCommand.ExecuteAsync(null);
                Assert.Equal(vm.L.StatusLoggingSettingsLoaded, vm.StatusMessage);
                var json = VmRest.Get("/rest/logging.json") ?? "";
                Assert.False(string.IsNullOrWhiteSpace(vm.LogLevel));
                // 独立校验：裸 JSON 文本里应能找到 VM 回填的 level
                Assert.Contains("\"" + vm.LogLevel + "\"", json);
                Assert.Contains(vm.LogLevel, vm.LogLevelOptions); // 回填值应为合法级别之一
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public async Task Wcs_LoadBackfill_Completes()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new WCSSettingsViewModel(conn);
            try
            {
                await vm.LoadSettingsCommand.ExecuteAsync(null);
                Assert.Equal(vm.L.StatusWcsSettingsLoaded, vm.StatusMessage);
                Assert.False(vm.IsLoading);
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public async Task DiskQuota_Load_CompletesGracefully_GwcXmlMismatch()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new DiskQuotaViewModel(conn);
            try
            {
                await vm.LoadSettingsCommand.ExecuteAsync(null);
                // GWC diskquota 以 XML 返回且客户端未包壳 → 反序列化异常被捕获，不应抛到命令外层。
                Assert.False(vm.IsLoading);
                var failPrefix = string.Format(vm.L.StatusDiskQuotaLoadFailed, "");
                Assert.True(vm.StatusMessage == vm.L.StatusDiskQuotaLoaded || vm.StatusMessage.StartsWith(failPrefix),
                    "意外状态: " + vm.StatusMessage);
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public async Task Gridsets_Load_Completes_Fixed_E8()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new GridsetsViewModel(conn);
            try
            {
                await vm.LoadGridsetsCommand.ExecuteAsync(null);
                Assert.False(vm.IsLoading);
                // FIXED-E8 翻转：裸 JSON 数组由 RawStringArrayConverter 承接后，网格集列表可正常加载
                var tpl = vm.L.StatusGridsetsLoaded; // "Loaded {0} gridsets" / 中文等价
                var okPrefix = tpl.Substring(0, tpl.IndexOf('{', StringComparison.Ordinal)).TrimEnd();
                Assert.StartsWith(okPrefix, vm.StatusMessage);
                Assert.NotEmpty(vm.Gridsets);
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public async Task Security_Load_LocalizedEnglish_Fixed_E36()
        {
            if (!RequireGeoServer()) return;
            bool originalChinese = LocalizationService.Instance.AppHeaderSubtitle != "Administration";
            try
            {
                if (originalChinese) LocalizationService.Instance.ToggleLanguage(); // 切英文
                var conn = VmTestKit.Connected();
                var vm = new SecuritySettingsViewModel(conn);
                try
                {
                    await vm.LoadSettingsCommand.ExecuteAsync(null);
                    Assert.False(vm.IsLoading);
                    // FIXED-E36 翻转：英文态下状态串已走本地化键，不再含硬编码中文；仍应提及 ACL
                    Assert.Contains("ACL", vm.StatusMessage);
                    Assert.False(vm.StatusMessage.Any(c => c >= 0x4E00 && c <= 0x9FFF),
                        "英文态不应再出现中文硬编码（E36 已修复），实际: " + vm.StatusMessage);
                }
                finally { conn.Disconnect(); }
            }
            finally
            {
                if ((LocalizationService.Instance.AppHeaderSubtitle != "Administration") != originalChinese)
                    LocalizationService.Instance.ToggleLanguage();
            }
        }
    }
}
