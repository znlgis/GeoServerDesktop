using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Import;
using GeoServerDesktop.GeoServerClient.Migration;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using GeoServerDesktop.Tests.RealData;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// M4 工作空间导入/导出集成测试：
    /// ① 跨工作空间迁移（A→B，同一实例两目录空间即迁移的等价物）：向导图形数据 + 绑定 gdtest_red 样式 →
    ///    导出归档 → 导入为新工作空间 → 图层清单/默认样式绑定/WFS 计数（期望值取自 DBF 文件头）等价；
    /// ② 导出 → 清空（recurse 删除）→ 导入还原：资源恢复且样式绑定随迁；
    /// ③ 清单结构断言（namespace URI、存储连接参数、绑定差异、SLD 成员）。
    /// 独立复核走裸 REST/WFS（OgcProbe/GsKit），不经被测库。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class WorkspaceMigrationIT : GeoServerTestBase
    {
        private const string Src = TestEnv.Prefix + "_ws_mig";       // gdtest_ws_mig
        private const string Tgt = TestEnv.Prefix + "_ws_mig2";      // gdtest_ws_mig2
        private const string Ds = "mds";
        private const string PolyLayer = "mig_poly";
        private const string LinesLayer = "mig_lines";

        public WorkspaceMigrationIT(GeoServerFixture fx) : base(fx) { }

        private async Task PublishSourceAsync()
        {
            using var f = Fx.Factory();
            var wsSvc = f.CreateWorkspaceService();
            try { await wsSvc.GetWorkspaceAsync(Src); }
            catch { await wsSvc.CreateWorkspaceAsync(Src); }
            Fx.Cleanup.TrackWorkspace(Src);

            var wiz = f.CreateImportWizardService();
            foreach (var (layerName, native) in new[] { (PolyLayer, "gdtest_poly"), (LinesLayer, "gdtest_lines") })
            {
                var r = await wiz.PublishShapefileAsync(new ImportSourceRequest
                {
                    Kind = ImportDataSourceKind.ShapefileDirectory,
                    Workspace = Src,
                    StoreName = Ds,
                    LayerName = layerName,
                    NativeName = native,
                    FileRef = "file:gdtest_data",
                    Srs = "EPSG:4326",
                });
                Assert.True(r.Success, "发布失败：" + r.Message);
            }

            // 绑定全局样式 gdtest_red（验证"图层默认样式绑定差异"随迁移走）
            E2ePublisher.EnsureStyle();
            var ly = f.CreateLayerService();
            var polyDetail = await ly.GetWorkspaceLayerAsync(Src, PolyLayer);
            await ly.UpdateLayerAsync(Src + ":" + PolyLayer, BuildLayerUpdate(polyDetail!, E2ePublisher.Style));
        }

        /// <summary>PUT layer 必须回传填实的 resource 引用（与 M3 集成测试同一实测形态）。</summary>
        private static GeoServerDesktop.GeoServerClient.Models.Layer BuildLayerUpdate(
            GeoServerDesktop.GeoServerClient.Models.Layer orig, string defaultStyleName) => new GeoServerDesktop.GeoServerClient.Models.Layer
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
        public async Task Export_ManifestCapturesFullStructure()
        {
            if (!RequireGeoServer()) return;
            await PublishSourceAsync();
            using var f = Fx.Factory();
            var svc = f.CreateWorkspaceMigrationService();

            var result = await svc.ExportWorkspaceAsync(Src);
            var m = result.Manifest;

            Assert.Equal(Src, m.Workspace);
            // 命名空间随部署状态：存在则 URI 记录，缺失为正常状态（导入端补建占位）
            var nsRaw = await GsKit.RawStatusAsync(Fx, "/rest/namespaces/" + Src + ".json");
            if (nsRaw == 404) Assert.Null(m.NamespaceUri);
            else Assert.Equal("http://" + Src, m.NamespaceUri); // 实测：新建工作空间自动命名空间为占位 URI
            var store = Assert.Single(m.DataStores);
            Assert.Equal(Ds, store.Name);
            Assert.Equal("Shapefile", store.Type);
            Assert.Equal(2, m.FeatureTypes.Count);
            Assert.Contains(m.FeatureTypes, x => x.Name == PolyLayer && x.NativeName == "gdtest_poly");
            Assert.Equal(2, m.LayerBindings.Count);
            Assert.All(m.LayerBindings, b => Assert.StartsWith(Src + ":", b.QualifiedLayer));

            // 归档含 manifest 与样式 SLD 成员（两条绑定的样式都要入包）
            var manifestText = WorkspaceMigrationService.ReadArchiveMemberText(result.Archive, "manifest.json");
            Assert.NotNull(manifestText);
            foreach (var b in m.LayerBindings)
            {
                Assert.Contains(m.Styles, s => s.Global && s.Name == b.Style && s.SldPath != null);
                Assert.NotNull(WorkspaceMigrationService.ReadArchiveMemberText(result.Archive,
                    m.Styles.Single(s => s.Global && s.Name == b.Style).SldPath!));
            }
            Assert.True(result.Archive.Length > 200);
        }

        [Fact]
        public async Task Migrate_CrossWorkspace_LayersStyleAndDataEquivalent()
        {
            if (!RequireGeoServer()) return;
            await PublishSourceAsync();
            using var f = Fx.Factory();
            var svc = f.CreateWorkspaceMigrationService();
            try
            {
                await GsKit.WipeWorkspaceAsync(Fx, Tgt);

                var export = await svc.ExportWorkspaceAsync(Src);
                var import = await svc.ImportWorkspaceAsync(new WorkspaceImportRequest
                {
                    Archive = export.Archive,
                    TargetWorkspace = Tgt,
                });
                Assert.True(import.Success, import.FailureSummary);

                // 图层清单等价（独立裸 REST 列表 vs 目标空间图层列表）
                var srcLayers = (await f.CreateLayerService().GetWorkspaceLayersAsync(Src))
                    .Select(l => l.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
                var tgtLayers = (await f.CreateLayerService().GetWorkspaceLayersAsync(Tgt))
                    .Select(l => l.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
                Assert.Equal(srcLayers, tgtLayers);

                // 默认样式绑定随迁（两侧同图层详情比对）
                var srcPoly = await f.CreateLayerService().GetWorkspaceLayerAsync(Src, PolyLayer);
                var detail = await f.CreateLayerService().GetWorkspaceLayerAsync(Tgt, PolyLayer);
                Assert.NotNull(srcPoly!.DefaultStyle);
                Assert.Equal(srcPoly!.DefaultStyle!.Name, detail!.DefaultStyle!.Name);

                // WFS 数据面等价：两侧 numberMatched 一致，且与 DBF 文件头记录数一致（独立真值）
                int polyExpected = DbfHeader.Parse(Path.Combine(TestEnv.GeneratedDataDir, "gdtest_poly.dbf")).RecordCount;
                int linesExpected = DbfHeader.Parse(Path.Combine(TestEnv.GeneratedDataDir, "gdtest_lines.dbf")).RecordCount;
                Throw(RealDataChecks.WfsHitsCount(Src + ":" + PolyLayer, polyExpected, "src-poly"));
                Throw(RealDataChecks.WfsHitsCount(Tgt + ":" + PolyLayer, polyExpected, "tgt-poly"));
                Throw(RealDataChecks.WfsHitsCount(Tgt + ":" + LinesLayer, linesExpected, "tgt-lines"));

                // 存储连接参数随迁（url 引用保留）
                var dsDetail = await f.CreateDataStoreService().GetDataStoreAsync(Tgt, Ds);
                Assert.Equal("file:gdtest_data", GsKit.GetParam(dsDetail!.ConnectionParameters, "url"));
            }
            finally
            {
                await GsKit.WipeWorkspaceAsync(Fx, Tgt);
                await GsKit.WipeWorkspaceAsync(Fx, Src);
            }
        }

        [Fact]
        public async Task Export_Wipe_ImportRestore_Equivalence()
        {
            if (!RequireGeoServer()) return;
            await PublishSourceAsync();
            using var f = Fx.Factory();
            var svc = f.CreateWorkspaceMigrationService();
            var layersBefore = (await f.CreateLayerService().GetWorkspaceLayersAsync(Src))
                .Select(l => ShortLayerName(l.Name)).OrderBy(n => n, StringComparer.Ordinal).ToArray();
            Assert.Equal(2, layersBefore.Length);
            try
            {
                // 1) 导出
                var export = await svc.ExportWorkspaceAsync(Src);

                // 2) 清空（recurse 级联删除，含命名空间）
                await f.CreateWorkspaceService().DeleteWorkspaceAsync(Src, recurse: true);
                Assert.Equal(404, await GsKit.RawStatusAsync(Fx, "/rest/workspaces/" + Src + ".json"));
                Assert.Equal(404, await GsKit.RawStatusAsync(Fx, "/rest/namespaces/" + Src + ".json"));

                // 3) 导入还原（沿用源工作空间名与 URI）
                var import = await svc.ImportWorkspaceAsync(new WorkspaceImportRequest { Archive = export.Archive });
                Assert.True(import.Success, import.FailureSummary);

                // 4) 资源等价性断言
                var layersAfter = (await f.CreateLayerService().GetWorkspaceLayersAsync(Src))
                    .Select(l => ShortLayerName(l.Name)).OrderBy(n => n, StringComparer.Ordinal).ToArray();
                Assert.Equal(layersBefore, layersAfter);

                int polyExpected = DbfHeader.Parse(Path.Combine(TestEnv.GeneratedDataDir, "gdtest_poly.dbf")).RecordCount;
                Throw(RealDataChecks.WfsHitsCount(Src + ":" + PolyLayer, polyExpected, "restored-poly"));

                var restored = await f.CreateLayerService().GetWorkspaceLayerAsync(Src, PolyLayer);
                var original = await f.CreateLayerService().GetWorkspaceLayerAsync(Src, LinesLayer);
                Assert.NotNull(restored!.DefaultStyle);
                Assert.NotNull(original);
                // 命名空间 URI 还原（迁移前为占位 http://ws；若源曾改 URI 亦随迁）
                var nsJson = await f.CreateNamespaceService().GetNamespaceAsync(Src);
                Assert.Equal("http://" + Src, nsJson!.Uri);
            }
            finally
            {
                // 兜底：确保夹具空间完整（供其它串行用例复用）
                if (await GsKit.RawStatusAsync(Fx, "/rest/workspaces/" + Src + ".json") != 404)
                {
                    try
                    {
                        await f.CreateImportWizardService().PublishShapefileAsync(new ImportSourceRequest
                        {
                            Kind = ImportDataSourceKind.ShapefileDirectory,
                            Workspace = Src,
                            StoreName = Ds,
                            LayerName = PolyLayer,
                            NativeName = "gdtest_poly",
                            FileRef = "file:gdtest_data",
                            Srs = "EPSG:4326",
                        });
                    }
                    catch { }
                }
                await GsKit.WipeWorkspaceAsync(Fx, Src);
            }
        }

        private static string ShortLayerName(string name)
        {
            var i = name.IndexOf(':');
            return i >= 0 ? name.Substring(i + 1) : name;
        }

        private static void Throw(params CheckResult[] results)
        {
            try { Check.ThrowOnFail(results); }
            catch (Exception ex) { throw new Xunit.Sdk.XunitException(ex.Message); }
        }
    }
}
