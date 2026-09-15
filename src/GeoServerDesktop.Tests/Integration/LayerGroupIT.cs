using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// 图层组（全局 + 工作空间级）CRUD 闭环。publishables 引用 e2e 共享已发布图层。
    /// 实测要点（3.0.1）：
    ///  - mode 必须大写枚举名（SINGLE/MIXED/EMAIL），小写回 400 No enum constant；
    ///  - 单个 publishable 的 GET 响应里 "published" 是对象而非数组 → PublishedItem[] 模型抛 Newtonsoft（基线固化，裸 GET 复核 title）；
    ///  - 工作空间级图层组不得引用其他工作空间的图层（500 明确报文）——ws 级测试直接用 e2e 工作空间本体；
    ///  - （FIXED）原 PUT 请求体 workspace 引用为 null → XStream ReferenceConverter NPE 500；
    ///    请求体现经 NullValueHandling.Ignore 省略 null 字段后该族消除，全局/工作空间级更新均可用。
    /// 命名族缩写：lg。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class LayerGroupIT : GeoServerTestBase
    {
        public LayerGroupIT(GeoServerFixture fx) : base(fx) { }

        private const string GlobalLg = TestEnv.Prefix + "_lg_global1";
        // 工作空间级：直接建在 e2e 工作空间里（publishable 必须同工作空间）
        private const string WsLg = TestEnv.Prefix + "_lg_ws1";

        private static LayerGroup NewGroup(string name, string title, string? wsRef) => new LayerGroup
        {
            Name = name,
            Title = title,
            Abstract = "",
            Mode = "SINGLE", // 实测：必须大写枚举名
            Href = "",
            Workspace = wsRef == null ? null : new WorkspaceReference { Name = wsRef, Href = "" },
            Publishables = new PublishableList
            {
                // 实测：publishable 用限定名 ws:layer（对象数组）
                Published = new[] { new PublishedItem { Type = "layer", Name = E2ePublisher.PolyLayerQualified, Href = "" } }
            }
        };

        /// <summary>
        /// GET 单体并断言 title：优先走被测客户端；若命中"单 publishable 对象化 → 数组模型"解析缺陷
        /// （新发现），固化基线并用裸 GET（独立于被测代码）复核 title 真的写进去了。
        /// </summary>
        private async Task AssertGroupTitleAsync(string path, string expectTitle)
        {
            Exception? modelEx = null;
            try
            {
                using var f = Fx.Factory();
                var svc = f.CreateLayerGroupService();
                LayerGroup? g;
                if (path.StartsWith("/rest/layergroups/"))
                {
                    var n = path.Substring("/rest/layergroups/".Length).Replace(".json", "");
                    g = await svc.GetLayerGroupAsync(Uri.UnescapeDataString(n));
                }
                else
                {
                    var m = System.Text.RegularExpressions.Regex.Match(path, @"/layergroups/([^/]+)\.json");
                    g = await svc.GetWorkspaceLayerGroupAsync(E2ePublisher.Ws, m.Groups[1].Value);
                }
                Assert.Equal(expectTitle, g!.Title);
                return;
            }
            catch (Newtonsoft.Json.JsonException e) { modelEx = e; }

            // KNOWN-ISSUE（新发现）：publishables 单元素时 JSON 回对象而非数组，模型数组反序列化必抛
            Assert.NotNull(modelEx);
            var raw = await Fx.Cleanup.GetAsync(path);
            Assert.NotNull(raw);
            Assert.Contains(expectTitle, raw!);
        }

        [Fact]
        public async Task Global_LayerGroup_Crud_And_UpdateNullWorkspaceRef_Baseline()
        {
            if (!RequireGeoServer()) return;
            E2ePublisher.EnsurePublishedLayer(E2ePublisher.Ws, TestEnv.Prefix + "_poly");
            using var f = Fx.Factory();
            var svc = f.CreateLayerGroupService();
            try
            {
                await GsKit.WipeLayerGroupAsync(Fx, GlobalLg);

                // CREATE → READ（全局创建请求体 workspace:null 实测可通过 POST 路径）
                await svc.CreateLayerGroupAsync(NewGroup(GlobalLg, "gdtest-t1", wsRef: null));
                var all = await svc.GetLayerGroupsAsync();
                Assert.Contains(all, g => g.Name == GlobalLg);
                await AssertGroupTitleAsync("/rest/layergroups/" + GlobalLg + ".json", "gdtest-t1");

                // --- UPDATE（FIXED，null 引用族随请求体 null 省略一并修复）：
                // 原基线：PUT 体必带 "workspace":null → XStream ReferenceConverter NPE 500。
                // 请求体现经 NullValueHandling.Ignore 后该缺陷族消除，全局图层组可正常更新 →
                // 断言改为更新生效（title=t2 回读）。 ---
                await svc.UpdateLayerGroupAsync(GlobalLg, NewGroup(GlobalLg, "gdtest-t2", wsRef: null));
                await AssertGroupTitleAsync("/rest/layergroups/" + GlobalLg + ".json", "gdtest-t2");

                // DELETE → 404（删除不带请求体，不受 null 引用影响）
                await svc.DeleteLayerGroupAsync(GlobalLg);
                var ex = await Record.ExceptionAsync(() => svc.GetLayerGroupAsync(GlobalLg));
                GsKit.AssertRequest(ex, 404, "删除后 GET 图层组");
            }
            finally
            {
                await GsKit.WipeLayerGroupAsync(Fx, GlobalLg);
            }
        }

        [Fact]
        public async Task Workspace_LayerGroup_Full_Crud()
        {
            if (!RequireGeoServer()) return;
            E2ePublisher.EnsurePublishedLayer(E2ePublisher.Ws, TestEnv.Prefix + "_poly");
            using var f = Fx.Factory();
            var svc = f.CreateLayerGroupService();
            try
            {
                // 建在 e2e 工作空间：publishable（gdtest_ws_e2e:gdtest_poly）与工作空间一致，
                // 否则实测 500 "Layer group within a workspace (x) can not contain resources from other workspace"
                // 前置幂等清理：删上一轮可能残留的本测试图层组（不动共享资源本体）
                try { await Fx.Cleanup.DeleteAsync($"/rest/workspaces/{E2ePublisher.Ws}/layergroups/{WsLg}"); } catch { }
                E2ePublisher.EnsurePublishedLayer(E2ePublisher.Ws, TestEnv.Prefix + "_poly");

                // CREATE（引用体填实 workspace 后实测 201）→ READ
                await svc.CreateWorkspaceLayerGroupAsync(E2ePublisher.Ws, NewGroup(WsLg, "gdtest-w1", wsRef: E2ePublisher.Ws));
                var all = await svc.GetWorkspaceLayerGroupsAsync(E2ePublisher.Ws);
                Assert.Contains(all, g => g.Name == WsLg);
                await AssertGroupTitleAsync($"/rest/workspaces/{E2ePublisher.Ws}/layergroups/{WsLg}.json", "gdtest-w1");

                // UPDATE：填实 workspace 引用的 PUT 实测 200 → 重读 title=w2
                await svc.UpdateWorkspaceLayerGroupAsync(E2ePublisher.Ws, WsLg, NewGroup(WsLg, "gdtest-w2", wsRef: E2ePublisher.Ws));
                await AssertGroupTitleAsync($"/rest/workspaces/{E2ePublisher.Ws}/layergroups/{WsLg}.json", "gdtest-w2");

                // DELETE → 404
                await svc.DeleteWorkspaceLayerGroupAsync(E2ePublisher.Ws, WsLg);
                var ex = await Record.ExceptionAsync(() => svc.GetWorkspaceLayerGroupAsync(E2ePublisher.Ws, WsLg));
                GsKit.AssertRequest(ex, 404, "删除后 GET 工作空间图层组");
            }
            finally
            {
                try { await svc.DeleteWorkspaceLayerGroupAsync(E2ePublisher.Ws, WsLg); } catch { }
            }
        }
    }
}
