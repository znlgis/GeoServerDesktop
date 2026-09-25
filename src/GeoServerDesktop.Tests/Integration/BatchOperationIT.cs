using System;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Import;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Infrastructure;
using GeoServerDesktop.Tests.RealData;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// M4 批量操作集成测试：批量改默认样式（含 PUT 局部更新不破坏其它字段的实测复核）、
    /// 存储级批量启停（coverageStore 404 回落路径）、引用中样式删除 403 部分失败语义、
    /// 批量删图层/工作空间（recurse 级联并回收命名空间）。
    /// 独立复核走裸 REST（GsKit.RawStatusAsync / OgcProbe），期望值来自数据文件头。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class BatchOperationIT : GeoServerTestBase
    {
        private const string Ws = TestEnv.Prefix + "_ws_batch";     // gdtest_ws_batch
        private const string Ws2 = TestEnv.Prefix + "_ws_batch2";   // gdtest_ws_batch2
        private const string Ds = "bds1";
        private const string Cov = "bcov1";
        private const string LayerA = "bat_poly";
        private const string LayerB = "bat_lines";
        private const string StyleRed = TestEnv.Prefix + "_vm_bat_red";    // gdtest_vm_bat_red
        private const string StyleGreen = TestEnv.Prefix + "_vm_bat_green"; // gdtest_vm_bat_green

        public BatchOperationIT(GeoServerFixture fx) : base(fx) { }

        private static string QA => Ws + ":" + LayerA;
        private static string QB => Ws + ":" + LayerB;

        /// <summary>前置：发布源工作空间（shapefile 存储 + 两个图层）与第二工作空间（同构）。</summary>
        private async Task EnsureFixturesAsync()
        {
            using var f = Fx.Factory();
            var wsSvc = f.CreateWorkspaceService();
            foreach (var ws in new[] { Ws, Ws2 })
            {
                try { await wsSvc.GetWorkspaceAsync(ws); }
                catch { await wsSvc.CreateWorkspaceAsync(ws); }
                Fx.Cleanup.TrackWorkspace(ws);
            }
            var wiz = f.CreateImportWizardService();
            foreach (var ws in new[] { Ws, Ws2 })
            {
                foreach (var (layer, native) in new[] { (LayerA, "gdtest_poly"), (LayerB, "gdtest_lines") })
                {
                    var r = await wiz.PublishShapefileAsync(new ImportSourceRequest
                    {
                        Kind = ImportDataSourceKind.ShapefileDirectory,
                        Workspace = ws,
                        StoreName = Ds,
                        LayerName = layer,
                        NativeName = native,
                        FileRef = "file:gdtest_data",
                        Srs = "EPSG:4326",
                    });
                    Assert.True(r.Success, "fixture 发布失败：" + r.Message);
                }
            }
        }

        [Fact]
        public async Task BulkStyle_And_StoreEnableDisable_And_BulkDelete()
        {
            if (!RequireGeoServer()) return;
            await EnsureFixturesAsync();
            using var f = Fx.Factory();
            var batch = f.CreateBatchOperationService();
            var ly = f.CreateLayerService();
            var ds = f.CreateDataStoreService();
            try
            {
                // 1) 批量改默认样式（PUT 局部更新：type/resource 等字段保持，实测基线）
                var r1 = await batch.SetLayersDefaultStyleAsync(new[] { QA, QB }, "polygon");
                Assert.True(r1.AllSucceeded, r1.FailureSummary);
                var a = await ly.GetLayerAsync(QA);
                Assert.Equal("polygon", a!.DefaultStyle!.Name);
                Assert.NotNull(a.Resource); // 局部更新未破坏资源引用

                // 2) 存储级禁用 → WMS GetCapabilities 不再列出其图层；启用恢复
                var rOff = await batch.SetStoresEnabledAsync(
                    new[] { new BatchTarget { Workspace = Ws, Name = Ds } }, enabled: false);
                Assert.True(rOff.AllSucceeded, rOff.FailureSummary);
                var store = await ds.GetDataStoreAsync(Ws, Ds);
                Assert.Equal(false, store!.Enabled);
                var capsOff = OgcProbe.Get(OgcProbe.Wms("request=GetCapabilities", "version=1.1.1"));
                Assert.DoesNotContain(QA, System.Text.Encoding.UTF8.GetString(capsOff.Bytes));

                var rOn = await batch.SetStoresEnabledAsync(
                    new[] { new BatchTarget { Workspace = Ws, Name = Ds } }, enabled: true);
                Assert.True(rOn.AllSucceeded, rOn.FailureSummary);
                store = await ds.GetDataStoreAsync(Ws, Ds);
                Assert.Equal(true, store!.Enabled);
                var capsOn = OgcProbe.Get(OgcProbe.Wms("request=GetCapabilities", "version=1.1.1"));
                Assert.Contains(QA, System.Text.Encoding.UTF8.GetString(capsOn.Bytes));

                // 3) 批量删除两图层 → GET 404
                var r3 = await batch.DeleteLayersAsync(new[] { QA, QB }, recurse: false);
                Assert.True(r3.AllSucceeded, r3.FailureSummary);
                GsKit.AssertRequest(await Record.ExceptionAsync(() => ly.GetLayerAsync(QA)), 404, "删除后 GET 图层");

                // 4) 批量删除工作空间（recurse=true）→ 工作空间与命名空间均 404（实测级联回收命名空间）
                var r4 = await batch.DeleteWorkspacesAsync(new[] { Ws, Ws2 }, recurse: true);
                Assert.True(r4.AllSucceeded, r4.FailureSummary);
                Assert.Equal(404, await GsKit.RawStatusAsync(Fx, "/rest/workspaces/" + Ws + ".json"));
                Assert.Equal(404, await GsKit.RawStatusAsync(Fx, "/rest/namespaces/" + Ws + ".json"));
                Assert.Equal(404, await GsKit.RawStatusAsync(Fx, "/rest/workspaces/" + Ws2 + ".json"));
            }
            finally
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
                await GsKit.WipeWorkspaceAsync(Fx, Ws2);
            }
        }

        [Fact]
        public async Task BulkDeleteStyles_ReportsInUse403AsPartialFailure()
        {
            if (!RequireGeoServer()) return;
            await EnsureFixturesAsync();
            using var f = Fx.Factory();
            var batch = f.CreateBatchOperationService();
            var st = f.CreateStyleService();
            var ly = f.CreateLayerService();
            try
            {
                await GsKit.WipeStyleAsync(Fx, StyleRed);
                await GsKit.WipeStyleAsync(Fx, StyleGreen);
                var redSld = E2ePublishHelper.RedSld.Replace(E2ePublishHelper.RedStyle, StyleRed);
                var greenSld = redSld.Replace("#FF0000", "#00FF00");
                await st.CreateStyleAsync(StyleRed, redSld);
                Fx.Cleanup.TrackStyle(StyleRed);
                await st.CreateStyleAsync(StyleGreen, greenSld);
                Fx.Cleanup.TrackStyle(StyleGreen);

                // 绑定 red 到图层 A（green 保持未引用）
                await batch.SetLayersDefaultStyleAsync(new[] { QA }, StyleRed);

                // 批量删两个样式：green 成功，red 403 部分失败（实测删除保护基线）
                var r = await batch.DeleteStylesAsync(
                    new[] { BatchTarget.GlobalStyle(StyleRed), BatchTarget.GlobalStyle(StyleGreen) }, purge: true);
                Assert.Equal(1, r.Succeeded);
                Assert.Equal(1, r.Failed);
                var redItem = r.Items.Single(i => i.Target == StyleRed);
                Assert.False(redItem.Success);
                Assert.Contains("403", redItem.Message);
                Assert.NotNull(await st.GetStyleAsync(StyleRed));

                // 解绑后再批量删 → 全部成功
                await batch.SetLayersDefaultStyleAsync(new[] { QA }, "polygon");
                var r2 = await batch.DeleteStylesAsync(
                    new[] { BatchTarget.GlobalStyle(StyleRed) }, purge: true);
                Assert.True(r2.AllSucceeded, r2.FailureSummary);
                GsKit.AssertRequest(await Record.ExceptionAsync(() => st.GetStyleAsync(StyleRed)), 404, "解绑后删除样式");
            }
            finally
            {
                await GsKit.WipeStyleAsync(Fx, StyleRed);
                await GsKit.WipeStyleAsync(Fx, StyleGreen);
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
                await GsKit.WipeWorkspaceAsync(Fx, Ws2);
            }
        }

        [Fact]
        public async Task CoverageStoreFallback_And_UnknownStore_Reported()
        {
            if (!RequireGeoServer()) return;
            if (!System.IO.File.Exists(System.IO.Path.Combine(TestEnv.GeneratedDataDir, "gdtest_dem.tif")))
            {
                SkipLog.Skip("gdtest_dem.tif 不存在，跳过 coverageStore 回落用例");
                return;
            }
            using var f = Fx.Factory();
            var wsSvc = f.CreateWorkspaceService();
            try { await wsSvc.GetWorkspaceAsync(Ws2); }
            catch { await wsSvc.CreateWorkspaceAsync(Ws2); }
            Fx.Cleanup.TrackWorkspace(Ws2);
            var cs = f.CreateCoverageStoreService();
            try { await cs.GetCoverageStoreAsync(Ws2, Cov); }
            catch
            {
                await cs.CreateCoverageStoreAsync(Ws2, new GeoServerDesktop.GeoServerClient.Models.CoverageStore
                {
                    Name = Cov,
                    Type = "GeoTIFF",
                    Enabled = true,
                    Workspace = new WorkspaceReference { Name = Ws2 },
                    Url = "file:gdtest_data/gdtest_dem.tif",
                });
            }
            var batch = f.CreateBatchOperationService();

            // 1) coverageStore：qualified 目标先按 dataStore 尝试 → 404 回落 coverageStore 成功
            var r = await batch.SetStoresEnabledAsync(
                new[] { new BatchTarget { Name = Ws2 + ":" + Cov } }, enabled: false);
            Assert.True(r.AllSucceeded, r.FailureSummary);
            var detail = await cs.GetCoverageStoreAsync(Ws2, Cov);
            Assert.Equal(false, detail!.Enabled);

            // 2) 不存在的存储 → 单项失败（两种 store 均 404）
            var r2 = await batch.SetStoresEnabledAsync(
                new[] { new BatchTarget { Workspace = Ws2, Name = "nope" } }, enabled: true);
            Assert.Equal(1, r2.Failed);
        }
    }
}
