using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// 样式（全局 + 工作空间级）CRUD 与图层绑定闭环。
    /// 实测要点（3.0.1）：非规范 SLD 会被 PUT .sld 拒 400（见 SldTemplates 注释）；
    /// 删除"正在被图层引用"的全局样式回 403 而非普通 4xx——按行为基线断言 4xx。
    /// 命名族缩写：sty；共享层 e2e 图层 gdtest_poly 经 E2ePublisher 幂等就绪。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class StyleLayerIT : GeoServerTestBase
    {
        public StyleLayerIT(GeoServerFixture fx) : base(fx) { }

        private const string GlobalStyle = TestEnv.Prefix + "_sty_style1";
        private const string Ws = TestEnv.Prefix + "_ws_sty";
        private const string WsStyle = TestEnv.Prefix + "_sty_wsstyle1";
        private const string LayerName = TestEnv.Prefix + "_poly"; // gdtest_ws_e2e 的发布图层（全局同名）

        [Fact]
        public async Task Global_Style_Crud_LayerBinding_And_InUseDeleteFails()
        {
            if (!RequireGeoServer()) return;
            E2ePublisher.EnsurePublishedLayer(E2ePublisher.Ws, LayerName);
            using var f = Fx.Factory();
            var stSvc = f.CreateStyleService();
            var lySvc = f.CreateLayerService();
            string originalStyle = "polygon";
            try
            {
                await GsKit.WipeStyleAsync(Fx, GlobalStyle);

                // --- CREATE（服务内部：POST 元数据 + PUT SLD，两步） ---
                await stSvc.CreateStyleAsync(GlobalStyle, SldTemplates.Red);

                // --- READ ---
                var st = await stSvc.GetStyleAsync(GlobalStyle);
                Assert.NotNull(st);
                Assert.Equal(GlobalStyle, st!.Name);
                var sld = await stSvc.GetStyleSldAsync(GlobalStyle);
                Assert.Contains("#FF0000", sld); // 自定义红色填充

                // --- UPDATE：换蓝色版 SLD ---
                await stSvc.UpdateStyleAsync(GlobalStyle, SldTemplates.Blue);
                var sld2 = await stSvc.GetStyleSldAsync(GlobalStyle);
                Assert.Contains("#0000FF", sld2);

                // --- 图层绑定：PUT layer.defaultStyle（实测 200；请求体须回传填实的 resource 引用，
                //     "resource":null 与 LG/FT 同族 → XStream ReferenceConverter NPE） ---
                var layer = await lySvc.GetLayerAsync(LayerName);
                Assert.NotNull(layer);
                originalStyle = layer!.DefaultStyle?.Name ?? "polygon";
                var bind = BuildLayerUpdate(layer, GlobalStyle);
                await lySvc.UpdateLayerAsync(LayerName, bind);
                var bound = await lySvc.GetLayerAsync(LayerName);
                Assert.Equal(GlobalStyle, bound!.DefaultStyle!.Name);

                // --- 在用样式删除：实测 GeoServer 3.0.1 回 403（行为基线：4xx 即可） ---
                var exInUse = await Record.ExceptionAsync(() => stSvc.DeleteStyleAsync(GlobalStyle, purge: false));
                var gre = GsKit.AssertRequest(exInUse, 403, "删除在用样式");
                Assert.NotNull(gre);

                // --- 解绑后删除成功 → GET 404 ---
                await lySvc.UpdateLayerAsync(LayerName, BuildLayerUpdate(layer, originalStyle));
                await stSvc.DeleteStyleAsync(GlobalStyle, purge: true);
                var exGone = await Record.ExceptionAsync(() => stSvc.GetStyleAsync(GlobalStyle));
                GsKit.AssertRequest(exGone, 404, "解绑删除后 GET 样式");
            }
            finally
            {
                // 兜底：先尽力解绑再删（工作空间删除不影响全局样式）
                try
                {
                    var l = await lySvc.GetLayerAsync(LayerName);
                    if (l?.DefaultStyle?.Name == GlobalStyle)
                        await lySvc.UpdateLayerAsync(LayerName, BuildLayerUpdate(l, originalStyle));
                }
                catch { }
                await GsKit.WipeStyleAsync(Fx, GlobalStyle);
            }
        }

        /// <summary>PUT layer 必须回传填实的 resource 默认风格引用（null 引用 → 500 NPE 同族缺陷）。</summary>
        private static Layer BuildLayerUpdate(Layer orig, string defaultStyleName) => new Layer
        {
            Name = orig.Name,
            Type = orig.Type,
            DefaultStyle = new StyleReference { Name = defaultStyleName, Href = "" },
            Resource = new ResourceReference
            {
                Class = orig.Resource?.Class ?? "featureType",
                Name = orig.Resource?.Name ?? "",
                Href = "",
            },
            Href = "",
        };

        [Fact]
        public async Task Workspace_Style_Full_Crud()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var wsSvc = f.CreateWorkspaceService();
            var stSvc = f.CreateStyleService();
            try
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
                await wsSvc.CreateWorkspaceAsync(Ws);
                Fx.Cleanup.TrackWorkspace(Ws);

                // --- CREATE / READ ---
                await stSvc.CreateWorkspaceStyleAsync(Ws, WsStyle, SldTemplates.Red);
                var st = await stSvc.GetWorkspaceStyleAsync(Ws, WsStyle);
                Assert.Equal(WsStyle, st!.Name);
                var sld = await stSvc.GetWorkspaceStyleSldAsync(Ws, WsStyle);
                Assert.Contains("#FF0000", sld);
                var list = await stSvc.GetWorkspaceStylesAsync(Ws);
                Assert.Contains(list, s => s.Name == WsStyle);

                // --- UPDATE ---
                await stSvc.UpdateWorkspaceStyleAsync(Ws, WsStyle, SldTemplates.Blue);
                var sld2 = await stSvc.GetWorkspaceStyleSldAsync(Ws, WsStyle);
                Assert.Contains("#0000FF", sld2);

                // --- DELETE → 404 ---
                await stSvc.DeleteWorkspaceStyleAsync(Ws, WsStyle, purge: true);
                var ex = await Record.ExceptionAsync(() => stSvc.GetWorkspaceStyleAsync(Ws, WsStyle));
                GsKit.AssertRequest(ex, 404, "删除后 GET 工作空间样式");
            }
            finally
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws); // 级联清掉残留的工作空间样式
            }
        }
    }
}
