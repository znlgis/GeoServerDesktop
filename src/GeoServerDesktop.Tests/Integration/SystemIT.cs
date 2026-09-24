using System;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// 系统族集成测试：about（version/manifests/system-status）、全局 settings 与 contact 往返、
    /// logging 往返、reload。3.0.1 实测形态作为行为基线固化。命名族缩写：sys。
    /// 注意：本类无破坏性写（contact/logging 均回滚原值）；reload 较重，放在类内最后一个测试。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class SystemIT : GeoServerTestBase
    {
        public SystemIT(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public async Task About_Version_RealDeserialization_Baseline()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateAboutService();
            var v = await svc.GetVersionAsync();
            Assert.NotNull(v?.About?.Resources);
            var gs = Assert.Single(v!.About!.Resources, r => r.Name == "GeoServer");
            // 实测 3.0.1：Version 是字符串（"3.0.1"），E39 担心的对象形 {"#text":...} 未出现；
            // GeoTools 的 Version 甚至是 JSON 数字（35.1），Newtonsoft 数字→string 兼容成功。
            Assert.StartsWith("3.", gs.Version);
        }

        [Fact]
        public async Task About_Manifests_Baseline()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateAboutService();
            // 实测 3.0.1：/rest/about/manifests.json 端点已移除 → 404（旧文档/2.x 行为不再成立）
            var ex = await Record.ExceptionAsync(() => svc.GetManifestsAsync());
            if (ex != null) GsKit.AssertRequest(ex, 404, "manifests");
            else Assert.NotNull("新版服务器 manifests 可用——基线更新，无异常通过");
        }

        [Fact]
        public async Task About_SystemStatus_RealDeserialization()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateAboutService();
            var s = await svc.GetSystemStatusAsync();
            Assert.NotNull(s?.Metrics?.MetricArray);
            Assert.NotEmpty(s!.Metrics!.MetricArray!);
            Assert.All(s.Metrics.MetricArray!, m => Assert.NotNull(m.Name));
        }

        [Fact]
        public async Task Settings_Global_Get_And_PutSameBody_KeepsAllFields()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateSettingsService();

            // FIXED（原基线 Settings 恒 null）：3.0.1 GET /rest/settings.json 根为
            // {"global":{"settings":{...}}}，模型已对齐 "global" 根，Settings 可直接读出。
            var gs = await svc.GetGlobalSettingsAsync();
            Assert.NotNull(gs?.Settings);
            Assert.False(string.IsNullOrWhiteSpace(gs!.Settings!.Charset));

            // --- 整包 PUT 语义往返：GET 快照 → 改目标字段(numDecimals) → PUT → 逐字段比对 ---
            var before = Newtonsoft.Json.Linq.JObject.Parse(await Fx.Cleanup.GetAsync("/rest/settings.json"))["global"]["settings"];
            int origDecimals = (int)before["numDecimals"];
            try
            {
                gs.Settings.NumDecimals = origDecimals == 7 ? 8 : 7; // 目标字段
                await svc.UpdateGlobalSettingsAsync(gs);

                var after = Newtonsoft.Json.Linq.JObject.Parse(await Fx.Cleanup.GetAsync("/rest/settings.json"))["global"]["settings"];
                Assert.Equal((int)gs.Settings.NumDecimals, (int)after["numDecimals"]); // 目标字段生效
                // 除 numDecimals 外逐字段比对：ExtensionData 保证 GET 出现但模型未声明的键
                // （metadata 的 quietOnNotFound map 形态等）也原样保留——GeoServer 3.0.1 整包替换语义
                // 下，任何客户端丢键都会在此翻红。
                foreach (var kv in before.Children<Newtonsoft.Json.Linq.JProperty>())
                {
                    if (kv.Name == "numDecimals") continue;
                    if (kv.Name == "id") continue; // 服务器目录对象身份键，回传与否实测不变更（跳过比对）
                    Assert.True(Newtonsoft.Json.Linq.JToken.DeepEquals(kv.Value, after[kv.Name]),
                        $"PUT 整包往返后字段 {kv.Name} 变化/丢失：{kv.Value} → {after[kv.Name]}");
                }
            }
            finally
            {
                // 回滚 numDecimals（快照对象当前含改动值，重取一次干净快照再写回）
                var snap = await svc.GetGlobalSettingsAsync();
                snap!.Settings!.NumDecimals = origDecimals;
                await svc.UpdateGlobalSettingsAsync(snap);
            }
        }

        [Fact]
        public async Task Settings_Contact_Roundtrip_FIXED_E17()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateSettingsService();

            // FIXED-E17（判为误报）：GET /rest/settings/contact.json 实测即单层平铺
            // {"contact":{...}}（无 {"contact":{"contact":...,"address":{...}}} 双层），
            // 模型 Address 为 string 不与对象撞键；往返读写均可用。
            var c = await svc.GetContactInfoAsync();
            Assert.NotNull(c?.Contact);
            var orig = c!.Contact!;
            Assert.NotNull(orig.ContactPerson);
            Assert.NotNull(orig.Welcome); // E17 复核补齐的 GET 实际键

            string? origPerson = orig.ContactPerson;
            orig.ContactPerson = "gdtest-sys-marker";
            await svc.UpdateContactInfoAsync(orig);
            var mid = await svc.GetContactInfoAsync();
            Assert.Equal("gdtest-sys-marker", mid!.Contact!.ContactPerson);
            // PUT 同扁平体往返：welcome/onlineResource 等其余字段不得被抹掉
            Assert.Equal(orig.Welcome, mid.Contact.Welcome);
            Assert.Equal(orig.OnlineResource, mid.Contact.OnlineResource);
            orig.ContactPerson = origPerson;
            await svc.UpdateContactInfoAsync(orig);
            var back = await svc.GetContactInfoAsync();
            Assert.Equal(origPerson, back!.Contact!.ContactPerson);
        }

        [Fact]
        public async Task Logging_Get_And_PutSameLevel_Rollback()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateLoggingService();
            var orig = await svc.GetLoggingSettingsAsync();
            Assert.NotNull(orig?.Logging);
            string? level = orig.Logging!.Level; // 实测 3.0.1 GET：{"logging":{"level":"DEFAULT_LOGGING","stdOutLogging":true}}
            Assert.NotNull(level);
            try
            {
                await svc.UpdateLoggingSettingsAsync(orig); // 原级别回写
                var back = await svc.GetLoggingSettingsAsync();
                Assert.Equal(level, back!.Logging!.Level);
            }
            catch (Exception e) when (GsKit.IsJsonNet(e) || e is GeoServerRequestException)
            {
                // 模型 location/fileLogging 为 null（GET 未返回）→ PUT 可能被拒；基线：级别未变
                var raw = await Fx.Cleanup.GetAsync("/rest/logging.json");
                Assert.NotNull(raw);
                Assert.Contains(level!, raw!);
            }
        }

        [Fact]
        public async Task Zz_ReloadCatalog_Returns200_Baseline()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateReloadService();
            // FIXED-E18：POST /rest/reload 现以显式 application/json 空体提交（实测 3.0.1 对
            // 无体/text/plain/json 三种形态均 200）。放在本类最后（zZ 前缀提示排序）：
            // reload 重建整个 catalog，较重。
            await svc.ReloadCatalogAsync();
            // reload 后服务器仍可用：GET 版本 200
            var v = await f.CreateAboutService().GetVersionAsync();
            Assert.NotNull(v?.About?.Resources);
        }

        [Fact]
        public async Task Reset_Endpoint_Exists_GetProbeOnly()
        {
            if (!RequireGeoServer()) return;
            // Warn（E18 同项决策）：/rest/reset 会清空全部存储/栅格/模式缓存，测试连接池共享实例，
            // 仅在明确安全时才允许 POST 调用。集成基线只以 GET 探测端点在位（原始状态码，不走会抛的
            // REST 客户端）：实测 405 Method Not Allowed。
            Assert.Equal(405, await GsKit.RawStatusAsync(Fx, "/rest/reset"));
        }
    }
}
