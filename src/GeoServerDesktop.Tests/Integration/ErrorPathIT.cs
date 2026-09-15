using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// 错误路径与轻量并发基线。命名族缩写：err。
    /// 实测基线（3.0.1）：
    ///  - 不存在资源 GET → 404，旧式 catalog 报文纯文本 "No such workspace: 'x' found" / "No such datastore..."（含 "No such"），
    ///    新式 problem+json 端点则 {"detail":"No endpoint..."}——本类只断言 catalog 族含 "No such"；
    ///  - 重复创建同名 workspace → 409（HTML/文本冲突页，报文含 "Conflict"），实测非 2xx。
    /// 并发部分：全部资源写测试挂串行集合（不并行互踩）；此处仅验证只读端点的并行读不炸。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class ErrorPathIT : GeoServerTestBase
    {
        public ErrorPathIT(GeoServerFixture fx) : base(fx) { }

        private const string Ws = TestEnv.Prefix + "_ws_err";

        [Fact]
        public async Task Missing_Resources_All_Return_404_NoSuch()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var wsSvc = f.CreateWorkspaceService();
            var stSvc = f.CreateStyleService();
            var lySvc = f.CreateLayerService();
            var dsSvc = f.CreateDataStoreService();
            var ftSvc = f.CreateFeatureTypeService();

            // 工作空间
            var e1 = await Record.ExceptionAsync(() => wsSvc.GetWorkspaceAsync("gdtest_missing_ws"));
            Assert.Contains("No such", GsKit.AssertRequest(e1, 404, "缺失 workspace").ResponseContent ?? "");

            // 样式
            var e2 = await Record.ExceptionAsync(() => stSvc.GetStyleAsync("gdtest_missing_style"));
            Assert.Contains("No such", GsKit.AssertRequest(e2, 404, "缺失 style").ResponseContent ?? "");

            // 图层
            var e3 = await Record.ExceptionAsync(() => lySvc.GetLayerAsync("gdtest_missing_layer"));
            Assert.Contains("No such", GsKit.AssertRequest(e3, 404, "缺失 layer").ResponseContent ?? "");

            // 存储/FT：借一个真实存在的工作空间（e2e，幂等就绪）
            E2ePublisher.EnsureShapefileStore();
            var e4 = await Record.ExceptionAsync(() => dsSvc.GetDataStoreAsync(E2ePublisher.Ws, "gdtest_missing_ds"));
            var gre4 = GsKit.AssertRequest(e4, 404, "缺失 datastore");
            Assert.True((gre4.ResponseContent ?? "").Contains("No such") || (gre4.ResponseContent ?? "").Contains("Cannot find"),
                "datastore 404 报文形态: " + GsKit.Trim(gre4.ResponseContent));

            var e5 = await Record.ExceptionAsync(() => ftSvc.GetFeatureTypeAsync(E2ePublisher.Ws, E2ePublisher.Ds, "gdtest_missing_ft"));
            Assert.Contains("No such", GsKit.AssertRequest(e5, 404, "缺失 featuretype").ResponseContent ?? "");
        }

        [Fact]
        public async Task Duplicate_Workspace_Create_Not2xx_Conflict_Baseline()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateWorkspaceService();
            try
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
                await svc.CreateWorkspaceAsync(Ws);
                Fx.Cleanup.TrackWorkspace(Ws);

                // 重复创建：实测 409 Conflict
                var ex = await Record.ExceptionAsync(() => svc.CreateWorkspaceAsync(Ws));
                var gre = GsKit.AssertRequest(ex, 409, "重复创建 workspace");
                Assert.True((int)System.Net.HttpStatusCode.Conflict == gre.StatusCode); // 非 2xx 基线
            }
            finally
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
            }
        }

        [Fact]
        public async Task Parallel_Reads_DoNotInterfere()
        {
            if (!RequireGeoServer()) return;
            // 串行集合保证写-写不并发；这里验证同一 catalog 下并发只读安全（各读各的资源，互不踩）
            using var f1 = Fx.Factory();
            using var f2 = Fx.Factory();
            var t1 = f1.CreateWorkspaceService().GetWorkspacesAsync();
            var t2 = f2.CreateLayerService().GetLayersAsync();
            var t3 = f1.CreateStyleService().GetStylesAsync();
            await Task.WhenAll(t1, t2, t3);
            Assert.NotNull(t1.Result);
            Assert.NotNull(t2.Result);
            Assert.NotNull(t3.Result);
        }
    }
}
