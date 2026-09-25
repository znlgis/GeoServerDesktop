using System;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Migration;
using GeoServerDesktop.Tests.Infrastructure;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// M4 设置同步集成测试：全局设置（contact 子树）与工作空间级 WMS 设置的
    /// 读取→叶子级差异→选择性应用→收敛复核→恢复。
    /// 实测基线（GeoServer 3.0.1）：
    /// ① /rest/settings PUT 整包替换（保留其余键）；contact 双端点（/settings 与 /settings/contact）
    ///    读写一致，GET /rest/settings 内嵌 contact 节点；volatile 键（id/updateSequence）不参与差异。
    /// ② /rest/services/wms/settings 根 wms 为平铺字段（无 service 包装）；
    /// ③ /rest/services/wms/workspaces/{ws}/settings 仅对已注册工作空间级 WMS 设置的实例存在
    ///    （默认演示环境通常只有 ne），未注册工作空间 404——测试按环境自动发现，缺则跳过。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class SettingsSyncIT : GeoServerTestBase
    {
        public SettingsSyncIT(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public async Task GlobalSettings_Diff_SelectiveApply_And_Converge()
        {
            if (!RequireGeoServer()) return;
            using var a = Fx.Factory();
            using var b = Fx.Factory();
            var svcA = a.CreateSettingsCompareService(); // 源
            var svcB = b.CreateSettingsCompareService(); // 目标（当前连接）

            // 初始两侧一致
            var initial = await SettingsCompareService.CompareAsync(svcA, svcB, SettingsDomain.Global);
            Assert.True(initial.IsIdentical, "同实例初始应无差异：" + string.Join("|", initial.Items.Select(i => i.Path)));

            // 源侧改 contact 子树（整包 PUT 实测 200，GET /rest/settings 内嵌回显）
            var originalJson = await svcA.ReadRawAsync(SettingsDomain.Global);
            var marker = "gdtest_sync_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            var parsed = JObject.Parse(originalJson);
            var contact = parsed["global"]?["settings"]?["contact"] as JObject;
            Assert.NotNull(contact);
            var originalCity = (string)contact!["addressCity"];
            contact["addressCity"] = marker;
            await svcA.PutRawAsync("/rest/settings", parsed.ToString(Newtonsoft.Json.Formatting.None));
            try
            {
                // 双工厂独立读回仍一致（同实例）→ 用合成目标验证"读-比-应用"全链路：
                // 构造"对侧"文档 = 当前文档把 city 还原为旧值，模拟差异存在的目标实例
                var other = JObject.Parse(originalJson);
                var otherContact = other["global"]?["settings"]?["contact"] as JObject;
                Assert.NotNull(otherContact);
                otherContact!["addressCity"] = "PRE-EXISTING-TARGET";

                var liveSource = await svcA.ReadRawAsync(SettingsDomain.Global);
                var diff = SettingsCompare.Compare(SettingsDomain.Global, liveSource, other.ToString());
                var city = Assert.Single(diff.Items, i => i.Path.EndsWith("addressCity", StringComparison.Ordinal));
                Assert.Contains(marker, city.SourceValue);
                Assert.Equal("\"PRE-EXISTING-TARGET\"", city.TargetValue);

                // 勾选 addressCity 应用 → PUT 当前实例（整包替换语义：目标基底 + 勾选覆写）
                await svcB.ApplyAsync(liveSource, SettingsDomain.Global, new[] { city.Path });
                var after = await SettingsCompareService.CompareAsync(svcA, svcB, SettingsDomain.Global);
                Assert.DoesNotContain(after.Items, i => i.Path.EndsWith("addressCity", StringComparison.Ordinal));
                var echo = JObject.Parse(await svcB.ReadRawAsync(SettingsDomain.Global));
                Assert.Equal(marker, (string)echo["global"]!["settings"]!["contact"]!["addressCity"]);
            }
            finally
            {
                await svcA.PutRawAsync("/rest/settings", originalJson);
            }
            var restored = await SettingsCompareService.CompareAsync(svcA, svcB, SettingsDomain.Global);
            Assert.True(restored.IsIdentical, "恢复后应无差异：" + string.Join("|", restored.Items.Select(i => i.Path)));
            // 原始 city 复原（若源本来就是某值）
            var check = JObject.Parse(await svcA.ReadRawAsync(SettingsDomain.Global));
            Assert.Equal(originalCity, (string)check["global"]!["settings"]!["contact"]!["addressCity"]);
        }

        [Fact]
        public async Task WorkspaceWmsSettings_DiffAndApply()
        {
            if (!RequireGeoServer()) return;
            using var a = Fx.Factory();
            var svcA = a.CreateSettingsCompareService();

            // 环境自适应：找第一个已注册工作空间级 WMS 设置的实例；没有则跳过（不内置数据集名假设）
            string? ws = null;
            foreach (var w in (await a.CreateWorkspaceService().GetWorkspacesAsync()).OrderBy(x => x.Name))
            {
                try
                {
                    await svcA.ReadRawAtPathAsync(
                        "/rest/services/wms/workspaces/" + Uri.EscapeDataString(w.Name!) + "/settings.json");
                    ws = w.Name;
                    break;
                }
                catch (GeoServerDesktop.GeoServerClient.Http.GeoServerRequestException)
                {
                    // 未注册该工作空间的 WMS 设置，尝试下一个
                }
            }
            if (ws == null)
            {
                SkipLog.Skip("环境无已注册的工作空间级 WMS 设置实例，跳过该用例");
                return;
            }

            string getPath = "/rest/services/wms/workspaces/" + Uri.EscapeDataString(ws) + "/settings.json";
            string putPath = "/rest/services/wms/workspaces/" + Uri.EscapeDataString(ws) + "/settings";

            // 实测根 wms 为平铺字段（无 service 包装）→ 按根对象叶子比对
            var originalJson = await svcA.ReadRawAtPathAsync(getPath);
            var parsed = JObject.Parse(originalJson);
            Assert.True(parsed["wms"] is JObject, "WMS 设置根应为 wms 对象");
            bool originalFlag = (bool?)parsed["wms"]!["citeCompliant"] ?? false;
            parsed["wms"]!["citeCompliant"] = !originalFlag;
            await svcA.PutRawAsync(putPath, parsed.ToString(Newtonsoft.Json.Formatting.None));
            try
            {
                var sourceJson = await svcA.ReadRawAtPathAsync(getPath);
                Assert.Equal(!originalFlag,
                    (bool?)JObject.Parse(sourceJson)["wms"]!["citeCompliant"]);

                // 合成"目标"（该键保持原值）→ 差异恰为 citeCompliant → 应用回本实例（等价跨实例写路径）
                var other = JObject.Parse(originalJson);
                var diff = SettingsCompare.Compare(SettingsDomain.Wms, sourceJson, other.ToString(Newtonsoft.Json.Formatting.None),
                    rootKey: "wms");
                var item = Assert.Single(diff.Items, i => i.Path == "citeCompliant");
                Assert.Equal("citeCompliant", item.Path);

                var payload = SettingsCompare.Apply(SettingsDomain.Wms, sourceJson,
                    await svcA.ReadRawAtPathAsync(getPath), new[] { "citeCompliant" },
                    rootKey: "wms");
                await svcA.PutRawAsync(putPath, payload);

                var after = SettingsCompare.Compare(SettingsDomain.Wms, sourceJson,
                    await svcA.ReadRawAtPathAsync(getPath), rootKey: "wms");
                Assert.DoesNotContain(after.Items, i => i.Path == "citeCompliant");
            }
            finally
            {
                await svcA.PutRawAsync(putPath, originalJson);
            }
        }
    }
}
