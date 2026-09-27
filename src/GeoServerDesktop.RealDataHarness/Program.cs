using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.GeoServerClient.Import;
using GeoServerDesktop.GeoServerClient.Migration;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.GeoServerClient.Sld;
using GeoServerDesktop.Tests.Infrastructure;
using GeoServerDesktop.Tests.RealData;
using Xunit;

namespace GeoServerDesktop.RealDataHarness
{
    /// <summary>
    /// GeoServerDesktop 真实数据控制台 harness：
    ///   1) 环境探测（GeoServer / PostGIS / 测试数据目录）——缺失即 SKIPPED，不报错；
    ///   2) 数据完整性：SHP/DBF/TIFF 文件头独立解析交叉校验（不经任何 GIS 库）；
    ///   3) 真实发布 + WFS/WMS/WCS/WMTS/GWC 服务面端到端校验（期望值全部来自数据文件本身）；
    ///   4) 汇总 Pass/Warn/Fail/Skip，Fail>0 → 退出码 1（CI 可直接接入）。
    /// 复用 GeoServerDesktop.Tests 的检查函数（与 xunit 同一套逻辑，双形态）。
    /// </summary>
    public static class Program
    {
        /// <summary>--only 段过滤器（可多次或逗号分隔）：只跑命中的检查段。空=全跑。</summary>
        private static readonly HashSet<string> OnlyTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static bool Want(string tag) => OnlyTags.Count == 0 || OnlyTags.Contains(tag);

        /// <summary>分步跟踪：让 stdout 成为心跳，挂在哪一步一目了然。</summary>
        private static void Step(string what) => Console.WriteLine("   · " + what);

        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] != "--only" && args[i] != "-t") continue;
                foreach (var t in args[i + 1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    OnlyTags.Add(t.Trim());
            }
            if (OnlyTags.Count > 0) Console.WriteLine("段过滤：--only " + string.Join(",", OnlyTags));
            Check.Reset();

            Console.WriteLine("== GeoServerDesktop RealData Harness ==");
            Console.WriteLine($"GeoServer : {TestEnv.BaseUrl}");
            Console.WriteLine($"DataDir   : {TestEnv.GeneratedDataDir}");
            Console.WriteLine($"RealDir   : {TestEnv.RealDataDir ?? "(未提供)"}");
            Console.WriteLine();

            var fixture = new GeoServerFixture();
            try
            {
                if (Want("env")) RunEnvChecks();
                if (Want("data")) RunDataIntegrityChecks();
                if (GeoServerAvailability.IsGeoServerReachable)
                {
                    if (Want("pub")) RunPublication();
                    if (Want("wizard")) RunWizardPublishChecks();
                    if (Want("style")) RunStyleChecks();
                    if (Want("batch")) RunBatchChecks();
                    if (Want("mig")) RunMigrationChecks();
                    if (Want("sync")) RunSettingsSyncChecks();
                    if (Want("plane")) RunServicePlaneChecks();
                    if (Want("gwc")) RunGwcChecks();
                }
                else
                {
                    Check.Warn("Harness/GeoServer", "GeoServer 不可达，服务面检查全部跳过");
                }
                if (Want("ext")) RunExtendedFidelityChecks();
                if (Want("raster")) RunRasterChecks();
                if (Want("extdata") && !string.IsNullOrEmpty(TestEnv.RealDataDir)) RunExternalRealData();
                if (Want("extfid") && !string.IsNullOrEmpty(TestEnv.RealDataDir)) RunExternalFidelityChecks();
                if (Want("audit")) RunResidueAudit();
                if (Want("diag")) RunDiagnosticDump();
            }
            catch (Exception ex)
            {
                Check.Fail("Harness/Crash", ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                // GSD_KEEP=1：跳过孤儿兜底清理，保留现场供手工复现（诊断真实数据服务面行为时用）
                if (!ExtendedFixture.Keep) { try { fixture.Dispose(); } catch { } }
            }

            return ReportAndExit();
        }

        // ---------------- 1. 环境 ----------------
        private static void RunEnvChecks()
        {
            Console.WriteLine("-- 环境探测");
            Check.Cond(Directory.Exists(TestEnv.GeneratedDataDir), "Env/DataDir",
                "测试数据目录存在", "数据目录缺失：请先运行 tests/testdata/generate_testdata.py");
            Check.Cond(GeoServerAvailability.IsGeoServerReachable, "Env/GeoServer",
                "GeoServer 可达", "GeoServer 不可达（服务面检查将跳过）");
            if (Directory.Exists(TestEnv.GeneratedDataDir))
                Check.Cond(DataEnv.GeneratedDataInMount(), "Env/Mount",
                    "数据目录位于容器 data_dir 挂载内", "数据不在挂载卷内（GSD_CONTAINER_DATA_DIR 可覆盖）");
        }

        // ---------------- 2. 数据完整性（文件头独立解析交叉校验） ----------------
        private static void RunDataIntegrityChecks()
        {
            Console.WriteLine("-- 数据完整性");
            if (!Directory.Exists(TestEnv.GeneratedDataDir)) { Check.Warn("Data", "无数据目录，跳过"); return; }
            foreach (var baseName in new[] { "gdtest_poly", "gdtest_lines" })
            {
                var shp = Path.Combine(TestEnv.GeneratedDataDir, baseName + ".shp");
                var dbf = Path.Combine(TestEnv.GeneratedDataDir, baseName + ".dbf");
                if (!File.Exists(shp) || !File.Exists(dbf)) { Check.Warn("Data/" + baseName, "文件缺失"); continue; }
                var h = ShapefileHeader.Parse(shp);
                int shpRecords = ShapefileHeader.CountRecords(shp);
                var d = DbfHeader.Parse(dbf);
                var rows = DbfRecords.Read(dbf);
                Check.Cond(shpRecords == d.RecordCount && shpRecords == rows.Count,
                    "Data/" + baseName + "/count", $"SHP 逐记录={shpRecords} == DBF 头={d.RecordCount} == 行解析={rows.Count}",
                    $"不一致 shp={shpRecords} dbfHdr={d.RecordCount} rows={rows.Count}");
                Check.Cond(h.FileLengthBytes == new FileInfo(shp).Length,
                    "Data/" + baseName + "/flen", "SHP 头文件长与实际一致",
                    $"{h.FileLengthBytes} vs {new FileInfo(shp).Length}");
                Check.Cond(d.Fields.Any(f => f.Name == "NAME"), "Data/" + baseName + "/fields",
                    "字段含 NAME", "字段表缺 NAME");
            }
            var tif = Path.Combine(TestEnv.GeneratedDataDir, "gdtest_dem.tif");
            if (File.Exists(tif))
            {
                var t = TiffHeader.Parse(tif);
                Check.Cond(t.Width == 81 && t.Height == 41 && t.BitsPerSample == 32,
                    "Data/tif/hdr", "81x41 Float32", $"{t.Width}x{t.Height} bps{t.BitsPerSample}");
                Check.Cond(t.GeoTiffProjCS == 32754, "Data/tif/crs", "ProjCS=EPSG:32754", "ProjCS=" + t.GeoTiffProjCS);
                double v = TiffPixels.ReadFloat32(tif, 40, 20);
                Check.Cond(Math.Abs(v - RealDataChecks.DemFormula(40, 20)) < 1e-3,
                    "Data/tif/pixel", "像元(40,20)与公式一致", $"{v} vs {RealDataChecks.DemFormula(40, 20)}");
            }
            // 外部真实数据若提供：对其做同样的头交叉校验（数据无关）
            if (!string.IsNullOrEmpty(TestEnv.RealDataDir) && Directory.Exists(TestEnv.RealDataDir))
                CheckExternalHeaders(TestEnv.RealDataDir);
        }

        private static IEnumerable<(string shp, string dbf)> DiscoverPairs(string dir) =>
            Directory.EnumerateFiles(dir, "*.shp", SearchOption.AllDirectories)
                .Where(f => File.Exists(Path.ChangeExtension(f, ".dbf")))
                .Select(f => (f, Path.ChangeExtension(f, ".dbf")));

        private static void CheckExternalHeaders(string dir)
        {
            foreach (var (shp, dbf) in DiscoverPairs(dir))
            {
                var name = Path.GetFileNameWithoutExtension(shp);
                try
                {
                    int cnt = ShapefileHeader.CountRecords(shp);
                    var d = DbfHeader.Parse(dbf);
                    Check.Cond(cnt == d.RecordCount, "Ext/" + name, $"记录数交叉一致={cnt}（type={ShapefileHeader.Parse(shp).ShapeType}）",
                        $"SHP={cnt} DBF={d.RecordCount}");
                }
                catch (Exception ex) { Check.Warn("Ext/" + name, ex.Message); }
            }
        }

        // ---------------- 3. 发布 ----------------
        private static void RunPublication()
        {
            Console.WriteLine("-- 发布（被测客户端库路径）");
            E2ePublishHelper.EnsurePublished(new GeoServerFixture());
            var r = OgcProbe.Get(TestEnv.RestBase + "/rest/layers/" + E2ePublishHelper.Ws + ":" + E2ePublishHelper.PolyLayer + ".json");
            Check.Cond(r.Ok, "Publish/layer-rest", "shapefile 图层经独立通道可见", "HTTP " + r.Status);
        }

        // ---------------- 3.5 向导路径发布（M2：ImportWizardService） ----------------
        private static void RunWizardPublishChecks()
        {
            Console.WriteLine("-- 向导路径发布（ImportWizardService）");
            string ws = "gdtest_ws_wiz";
            try
            {
                using var f = new GeoServerClientFactory(GeoServerAvailability.Options());
                try { f.CreateWorkspaceService().DeleteWorkspaceAsync(ws, true).GetAwaiter().GetResult(); } catch { }
                f.CreateWorkspaceService().CreateWorkspaceAsync(ws).GetAwaiter().GetResult();
                var wiz = f.CreateImportWizardService();

                // 1) 内置数据：向导路径发布 shapefile（发布名全局唯一，nativeName 指磁盘基名）
                const string polyLayer = "gdtest_wiz_poly";
                var r = wiz.PublishShapefileAsync(new ImportSourceRequest
                {
                    Kind = ImportDataSourceKind.ShapefileDirectory,
                    Workspace = ws,
                    LayerName = polyLayer,
                    NativeName = "gdtest_poly",
                    StoreName = "gdtest_ds_wiz",
                    FileRef = "file:gdtest_data",
                    Srs = "EPSG:4326",
                }).GetAwaiter().GetResult();
                Check.Cond(r.Success, "Wizard/shapefile-publish", "向导路径发布成功（" + r.QualifiedName + "）", r.Message);
                if (r.Success)
                {
                    var rest = OgcProbe.Get(TestEnv.RestBase + "/rest/layers/" + ws + ":" + polyLayer + ".json");
                    Check.Cond(rest.Ok, "Wizard/shapefile-rest", "向导图层经独立通道可见", "HTTP " + rest.Status);
                    int expected = DbfHeader.Parse(Path.Combine(TestEnv.GeneratedDataDir, "gdtest_poly.dbf")).RecordCount;
                    Throw(RealDataChecks.WfsHitsCount(ws + ":" + polyLayer, expected, "wizard"));
                }

                // 2) 外部真实数据（提供 GSD_REAL_DATA_DIR 且位于挂载内时）：向导路径一键发布 + WFS 计数比对
                var extDir = TestEnv.RealDataDir;
                if (!string.IsNullOrEmpty(extDir))
                {
                    var extRef = DataEnv.HostPathToDataDirRef(extDir);
                    var extPairs = DataEnv.DiscoverShapefilePairs(extDir);
                    if (extRef == null)
                    {
                        Check.Warn("Wizard/ext", "外部数据目录不在容器 data_dir 挂载内，跳过向导外部数据发布：" + extDir);
                    }
                    else if (extPairs.Count == 0)
                    {
                        Check.Warn("Wizard/ext", "GSD_REAL_DATA_DIR 下未发现成对 .shp/.dbf，跳过");
                    }
                    else
                    {
                        Console.WriteLine("   外部真实数据：" + extPairs.Count + " 个 shapefile（" + extRef + "）");
                        foreach (var (extShp, extDbf) in extPairs)
                        {
                            string extName = Path.GetFileNameWithoutExtension(extShp);
                            var rext = wiz.PublishShapefileAsync(new ImportSourceRequest
                            {
                                Kind = ImportDataSourceKind.ShapefileDirectory,
                                Workspace = ws,
                                LayerName = extName,
                                NativeName = extName,
                                StoreName = "gdtest_ds_wiz_ext",
                                FileRef = extRef,
                                Srs = null, // 数据为自定义 Lambert（无 EPSG）：不声明 SRS（请求体省略该字段）
                            }).GetAwaiter().GetResult();
                            Check.Cond(rext.Success, "Wizard/ext-publish:" + extName,
                                "向导路径发布成功（" + rext.QualifiedName + "）", rext.Message);
                            if (rext.Success)
                            {
                                int extExpected = DbfHeader.Parse(extDbf).RecordCount;
                                Throw(RealDataChecks.WfsHitsCount(ws + ":" + extName, extExpected, "wizard-ext/" + extName));
                            }
                        }
                    }
                }

                // 3) PostGIS：探测 + 向导发布（环境不可用时跳过）
                var pg = new PostgisConnectionParameters
                {
                    Host = TestEnv.GeoServerVisiblePgHost,
                    Port = TestEnv.PgPort,
                    Database = TestEnv.PgDb,
                    User = TestEnv.PgUser,
                    Password = TestEnv.PgPass,
                };
                var probe = wiz.ProbePostgisConnectionAsync(ws, pg).GetAwaiter().GetResult();
                if (!probe.Success)
                {
                    Check.Warn("Wizard/postgis", "PostGIS 不可用，跳过向导 PostGIS 发布：" + probe.Message);
                    return;
                }
                const string pgLayer = "gdtest_wiz_pg_poly";
                var rp = wiz.PublishPostgisAsync(new ImportSourceRequest
                {
                    Kind = ImportDataSourceKind.Postgis,
                    Workspace = ws,
                    LayerName = pgLayer,
                    NativeName = "gdtest_poly",
                    StoreName = "gdtest_ds_wiz_pg",
                    Srs = "EPSG:4326",
                    Postgis = pg,
                }).GetAwaiter().GetResult();
                Check.Cond(rp.Success, "Wizard/postgis-publish", "向导 PostGIS 发布成功（" + rp.QualifiedName + "）", rp.Message);
                if (rp.Success)
                {
                    int expected = DbfHeader.Parse(Path.Combine(TestEnv.GeneratedDataDir, "gdtest_poly.dbf")).RecordCount;
                    Throw(RealDataChecks.WfsHitsCount(ws + ":" + pgLayer, expected, "wizard-pg"));
                }
            }
            catch (Exception ex)
            {
                Check.Fail("Wizard/crash", ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                // 段内自清理：早前只在下一次运行开头删，导致 harness 跑完后服务器残留 gdtest_ws_wiz
                try
                {
                    using var fz = new GeoServerClientFactory(GeoServerAvailability.Options());
                    fz.CreateWorkspaceService().DeleteWorkspaceAsync(ws, true).GetAwaiter().GetResult();
                }
                catch { }
            }
        }

        // ---------------- 3.7 样式路径（M3：SLD 编辑器 + 样式库） ----------------
        private static void RunStyleChecks()
        {
            Console.WriteLine("-- 样式路径（SLD 编辑器 + 样式库）");
            string styleName = "gdtest_sld_harness";
            string layerFull = E2ePublishHelper.Ws + ":" + E2ePublishHelper.PolyLayer;
            string originalStyle = "polygon";
            try
            {
                using var f = new GeoServerClientFactory(GeoServerAvailability.Options());
                var stSvc = f.CreateStyleService();
                var lySvc = f.CreateLayerService();

                try { stSvc.DeleteStyleAsync(styleName, purge: true).GetAwaiter().GetResult(); } catch { }
                var orig = lySvc.GetLayerAsync(E2ePublishHelper.PolyLayer).GetAwaiter().GetResult();
                originalStyle = orig?.DefaultStyle?.Name ?? originalStyle;

                // 1) 结构化模型 → 生成 SLD → 本地校验（M3 生成器/校验器）
                var redSld = SldBuilder.Build(PolygonDoc(styleName, "#FF0000"));
                Throw(Check.Cond(SldValidator.Validate(redSld).IsValid, "Style/sld-validate",
                    "生成 SLD 本地校验通过", "生成 SLD 本地校验失败"));

                // 2) 保存（POST 元数据 + PUT SLD 两步）→ 读回解析（round-trip；颜色保留）
                stSvc.CreateStyleAsync(styleName, redSld).GetAwaiter().GetResult();
                var fetched = stSvc.GetStyleSldAsync(styleName).GetAwaiter().GetResult();
                var parsed = SldParser.Parse(fetched);
                Throw(Check.Cond(parsed.Success, "Style/sld-roundtrip", "读回可解析", "读回解析失败：" + parsed.Error));
                string fill = null;
                if (parsed.Success && parsed.Document != null && parsed.Document.Rules.Count > 0)
                    fill = (parsed.Document.Rules[0].Symbolizer as SldPolygonSymbolizer)?.FillColor;
                Throw(Check.Cond(fill == "#FF0000", "Style/sld-roundtrip-color", "读回填充色保留 #FF0000", "读回填充色=" + (fill ?? "(null)")));

                // 3) 绑定图层 → 使用关系聚合（库层）→ 全量交叉核对（裸 REST 重建期望）
                var layer = lySvc.GetLayerAsync(E2ePublishHelper.PolyLayer).GetAwaiter().GetResult();
                lySvc.UpdateLayerAsync(E2ePublishHelper.PolyLayer, BuildLayerUpdate(layer, styleName)).GetAwaiter().GetResult();
                var bound = lySvc.GetLayerAsync(E2ePublishHelper.PolyLayer).GetAwaiter().GetResult();
                Throw(Check.Cond(bound?.DefaultStyle?.Name == styleName, "Style/bind",
                    "图层默认样式绑定=" + styleName, "绑定后默认样式=" + (bound?.DefaultStyle?.Name ?? "(null)")));

                var usages = f.CreateStyleUsageService().GetStyleUsageAsync().GetAwaiter().GetResult();
                var mine = usages.FirstOrDefault(u => u.StyleName == styleName);
                Throw(Check.Cond(mine != null && mine.IsUsed && mine.Layers.Contains(layerFull), "Style/usage-bound",
                    "聚合结果含 " + styleName + " → [" + layerFull + "]",
                    "聚合未记录绑定关系：" + (mine == null ? "样式缺失" : "used=" + mine.IsUsed + " layers=[" + string.Join(",", mine.Layers) + "]")));
                Throw(StyleChecks.UsageCrossCheck(usages));

                // 4) WMS 出图像素验证：红 → 编辑为蓝 → 蓝（更新即时生效）
                Throw(RealDataChecks.WmsColorPixels(layerFull, styleName, "red", "style/red"));
                stSvc.UpdateStyleAsync(styleName, SldBuilder.Build(PolygonDoc(styleName, "#0000FF"))).GetAwaiter().GetResult();
                Throw(RealDataChecks.WmsColorPixels(layerFull, styleName, "blue", "style/blue"));

                // 5) 删除保护：引用中 DELETE → 403 拒绝且样式仍在（probe6 实测基线）
                var del = OgcProbe.Delete(TestEnv.RestBase + "/rest/styles/" + Uri.EscapeDataString(styleName) + "?purge=true");
                var still = OgcProbe.Get(TestEnv.RestBase + "/rest/styles/" + Uri.EscapeDataString(styleName) + ".json");
                Throw(Check.Cond(del.Status == 403 && still.Ok, "Style/delete-protected",
                    $"引用中删除被拒（HTTP {del.Status}）且样式仍在", $"删除 HTTP {del.Status}，样式在否={still.Ok}"));

                // 6) 解绑（恢复原始默认样式）→ 删除 → 404
                lySvc.UpdateLayerAsync(E2ePublishHelper.PolyLayer, BuildLayerUpdate(layer, originalStyle)).GetAwaiter().GetResult();
                stSvc.DeleteStyleAsync(styleName, purge: true).GetAwaiter().GetResult();
                var gone = OgcProbe.Get(TestEnv.RestBase + "/rest/styles/" + Uri.EscapeDataString(styleName) + ".json");
                Throw(Check.Cond(gone.Status == 404, "Style/delete-404", "解绑后删除 → GET 404", "删除后 GET HTTP " + gone.Status));
            }
            catch (Exception ex)
            {
                Check.Fail("Style/crash", ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                // 兜底：恢复图层默认样式 + 清理探针样式
                try
                {
                    using var f2 = new GeoServerClientFactory(GeoServerAvailability.Options());
                    var ly = f2.CreateLayerService();
                    var l = ly.GetLayerAsync(E2ePublishHelper.PolyLayer).GetAwaiter().GetResult();
                    if (l?.DefaultStyle?.Name == styleName)
                        ly.UpdateLayerAsync(E2ePublishHelper.PolyLayer, BuildLayerUpdate(l, originalStyle)).GetAwaiter().GetResult();
                    try { f2.CreateStyleService().DeleteStyleAsync(styleName, purge: true).GetAwaiter().GetResult(); } catch { }
                }
                catch { }
            }
        }

        /// <summary>纯色面文档（Fill + Stroke 同色）。</summary>
        private static SldDocument PolygonDoc(string styleName, string color)
        {
            var doc = new SldDocument { LayerName = styleName };
            doc.Rules.Add(new SldRule
            {
                Symbolizer = new SldPolygonSymbolizer
                {
                    FillColor = color,
                    FillOpacity = 1,
                    StrokeColor = color,
                    StrokeWidth = 1,
                },
            });
            return doc;
        }

        /// <summary>PUT layer 必须回传填实的 resource 引用（与 L2 集成测试同一形态）。</summary>
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

        // ---------------- 3.8 批量操作路径（M4：BatchOperationService） ----------------
        private static void RunBatchChecks()
        {
            Console.WriteLine("-- 批量操作路径（BatchOperationService）");
            string ws = "gdtest_ws_hbatch";
            string style = "gdtest_hbatch_style";
            const string layer = "hbatch_poly";
            try
            {
                using var f = new GeoServerClientFactory(GeoServerAvailability.Options());
                var batch = f.CreateBatchOperationService();
                try { f.CreateWorkspaceService().DeleteWorkspaceAsync(ws, true).GetAwaiter().GetResult(); } catch { }
                f.CreateWorkspaceService().CreateWorkspaceAsync(ws).GetAwaiter().GetResult();

                var pub = f.CreateImportWizardService().PublishShapefileAsync(new ImportSourceRequest
                {
                    Kind = ImportDataSourceKind.ShapefileDirectory,
                    Workspace = ws,
                    StoreName = "hbatch_ds",
                    LayerName = layer,
                    NativeName = "gdtest_poly",
                    FileRef = "file:gdtest_data",
                    Srs = "EPSG:4326",
                }).GetAwaiter().GetResult();
                Throw(Check.Cond(pub.Success, "Batch/publish", "批量夹具图层发布", pub.Message));
                if (!pub.Success) return;

                // 1) 批量改默认样式 → 裸 REST 复核（先备两个探针样式：一个绑定、一个保持未引用；
                //    实测：绑定不存在的样式名会被服务端静默忽略而非报错）
                const string bindStyle = "gdtest_hbatch_bind";
                try { f.CreateStyleService().DeleteStyleAsync(bindStyle, true).GetAwaiter().GetResult(); } catch { }
                f.CreateStyleService().CreateStyleAsync(bindStyle, SldBuilder.Build(PolygonDoc(bindStyle, "#FF0000"))).GetAwaiter().GetResult();
                try { f.CreateStyleService().DeleteStyleAsync(style, true).GetAwaiter().GetResult(); } catch { }
                f.CreateStyleService().CreateStyleAsync(style, SldBuilder.Build(PolygonDoc(style, "#00FF00"))).GetAwaiter().GetResult();
                var rStyle = batch.SetLayersDefaultStyleAsync(new[] { ws + ":" + layer }, bindStyle).GetAwaiter().GetResult();
                Throw(Check.Cond(rStyle.AllSucceeded, "Batch/set-style", "批量改默认样式成功", rStyle.FailureSummary));
                var bound = OgcProbe.Get(TestEnv.RestBase + "/rest/workspaces/" + ws + "/layers/" + layer + ".json");
                Throw(Check.Cond(bound.Ok && System.Text.Encoding.UTF8.GetString(bound.Bytes).Contains("\"" + bindStyle + "\""),
                    "Batch/set-style-rest", "裸 REST 复核默认样式=" + bindStyle, "复核失败 HTTP " + bound.Status));

                // 2) 存储批量禁用 → WMS GetCapabilities 不再列出该图层；启用恢复
                var rOff = batch.SetStoresEnabledAsync(new[] { new BatchTarget { Workspace = ws, Name = "hbatch_ds" } }, false)
                    .GetAwaiter().GetResult();
                Throw(Check.Cond(rOff.AllSucceeded, "Batch/disable-store", "存储批量禁用成功", rOff.FailureSummary));
                var capsOff = OgcProbe.Get(OgcProbe.Wms("request=GetCapabilities", "version=1.1.1"));
                Throw(Check.Cond(!System.Text.Encoding.UTF8.GetString(capsOff.Bytes).Contains(ws + ":" + layer),
                    "Batch/disable-wms", "禁用后 WMS 能力不含该图层", "GetCapabilities 仍列出"));
                var rOn = batch.SetStoresEnabledAsync(new[] { new BatchTarget { Workspace = ws, Name = "hbatch_ds" } }, true)
                    .GetAwaiter().GetResult();
                var capsOn = OgcProbe.Get(OgcProbe.Wms("request=GetCapabilities", "version=1.1.1"));
                Throw(Check.Cond(rOn.AllSucceeded && System.Text.Encoding.UTF8.GetString(capsOn.Bytes).Contains(ws + ":" + layer),
                    "Batch/enable-wms", "启用后 WMS 能力恢复该图层", rOn.FailureSummary));

                // 3) 部分成功语义：批量删样式 [引用中(403), 未引用(200)]
                const string bindStyle2 = "gdtest_hbatch_bind";
                var delTargets = new[] { BatchTarget.GlobalStyle(bindStyle2), BatchTarget.GlobalStyle(style) };
                var rDel = batch.DeleteStylesAsync(delTargets, purge: true).GetAwaiter().GetResult();
                Throw(Check.Cond(rDel.Succeeded == 1 && rDel.Failed == 1
                        && rDel.Items[0].Message.Contains("403") && rDel.Items[1].Success,
                    "Batch/partial", "引用中 403 + 未引用成功 的部分成功语义",
                    "succeeded=" + rDel.Succeeded + " failed=" + rDel.Failed + " " + rDel.FailureSummary));
                try
                {
                    f.CreateLayerService().UpdateLayerAsync(ws + ":" + layer,
                        BuildLayerUpdate(f.CreateLayerService().GetLayerAsync(ws + ":" + layer).GetAwaiter().GetResult(), "polygon"))
                        .GetAwaiter().GetResult();
                    f.CreateStyleService().DeleteStyleAsync(bindStyle2, true).GetAwaiter().GetResult();
                }
                catch { }
                try { f.CreateStyleService().DeleteStyleAsync(style, true).GetAwaiter().GetResult(); } catch { }

                // 4) 批量删工作空间（级联）→ 404
                var rWipe = batch.DeleteWorkspacesAsync(new[] { ws }, true).GetAwaiter().GetResult();
                var gone = OgcProbe.Get(TestEnv.RestBase + "/rest/workspaces/" + ws + ".json");
                Throw(Check.Cond(rWipe.AllSucceeded && gone.Status == 404, "Batch/delete-ws",
                    "批量删除工作空间级联生效（404）", rWipe.FailureSummary + " HTTP " + gone.Status));
            }
            catch (Exception ex)
            {
                Check.Fail("Batch/crash", ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                try
                {
                    using var f2 = new GeoServerClientFactory(GeoServerAvailability.Options());
                    f2.CreateWorkspaceService().DeleteWorkspaceAsync(ws, true).GetAwaiter().GetResult();
                }
                catch { }
            }
        }

        // ---------------- 3.9 迁移路径（M4：WorkspaceMigrationService，导出→清空→导入等价） ----------------
        private static void RunMigrationChecks()
        {
            Console.WriteLine("-- 迁移路径（WorkspaceMigrationService：导出→清空→导入）");
            string src = "gdtest_ws_hmig";
            string tgt = "gdtest_ws_hmig2";
            const string layer = "hmig_poly";
            try
            {
                using var f = new GeoServerClientFactory(GeoServerAvailability.Options());
                var mig = f.CreateWorkspaceMigrationService();
                WipeWs(f, src); WipeWs(f, tgt);
                f.CreateWorkspaceService().CreateWorkspaceAsync(src).GetAwaiter().GetResult();
                var pub = f.CreateImportWizardService().PublishShapefileAsync(new ImportSourceRequest
                {
                    Kind = ImportDataSourceKind.ShapefileDirectory,
                    Workspace = src,
                    StoreName = "hmig_ds",
                    LayerName = layer,
                    NativeName = "gdtest_poly",
                    FileRef = "file:gdtest_data",
                    Srs = "EPSG:4326",
                }).GetAwaiter().GetResult();
                Throw(Check.Cond(pub.Success, "Mig/publish", "迁移夹具发布", pub.Message));
                if (!pub.Success) return;

                // 1) 导出：清单结构断言
                var exp = mig.ExportWorkspaceAsync(src).GetAwaiter().GetResult();
                int dbfCount = DbfHeader.Parse(Path.Combine(TestEnv.GeneratedDataDir, "gdtest_poly.dbf")).RecordCount;
                Throw(Check.Cond(exp.Manifest.DataStores.Count == 1
                        && exp.Manifest.FeatureTypes.Count == 1
                        && exp.Manifest.FeatureTypes[0].NativeName == "gdtest_poly"
                        && exp.Manifest.LayerBindings.Count == 1,
                    "Mig/export-manifest", "清单含 1 存储/1 资源/1 绑定",
                    "ds=" + exp.Manifest.DataStores.Count + " ft=" + exp.Manifest.FeatureTypes.Count));

                // 2) 导入到新工作空间 → 资源等价
                var imp = mig.ImportWorkspaceAsync(new WorkspaceImportRequest
                {
                    Archive = exp.Archive,
                    TargetWorkspace = tgt,
                }).GetAwaiter().GetResult();
                Throw(Check.Cond(imp.Success, "Mig/import", "导入 " + tgt + " 全部步骤成功", imp.FailureSummary));
                var srcLayers = LayerNames(f, src);
                var tgtLayers = LayerNames(f, tgt);
                Throw(Check.Cond(srcLayers.Length == tgtLayers.Length && srcLayers == string.Join(",", tgtLayers),
                    "Mig/equivalence", "两侧图层清单等价 [" + srcLayers + "]", "tgt=[" + string.Join(",", tgtLayers) + "]"));
                Throw(RealDataChecks.WfsHitsCount(tgt + ":" + layer, dbfCount, "mig-tgt"));

                // 3) 导出→清空→导入还原（A→B→A'）
                WipeWs(f, src);
                var missing = OgcProbe.Get(TestEnv.RestBase + "/rest/workspaces/" + src + "/layers/" + layer + ".json");
                Throw(Check.Cond(missing.Status == 404, "Mig/wiped", "清空后源图层 404", "HTTP " + missing.Status));
                var back = mig.ImportWorkspaceAsync(new WorkspaceImportRequest { Archive = exp.Archive }).GetAwaiter().GetResult();
                var restored = OgcProbe.Get(TestEnv.RestBase + "/rest/workspaces/" + src + "/layers/" + layer + ".json");
                Throw(Check.Cond(back.Success && restored.Ok, "Mig/restore",
                    "清空后导入还原（GET 200）", back.FailureSummary + " HTTP " + restored.Status));
                Throw(RealDataChecks.WfsHitsCount(src + ":" + layer, dbfCount, "mig-restored"));
            }
            catch (Exception ex)
            {
                Check.Fail("Mig/crash", ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                try
                {
                    using var f2 = new GeoServerClientFactory(GeoServerAvailability.Options());
                    WipeWs(f2, src); WipeWs(f2, tgt);
                }
                catch { }
            }
        }

        private static void WipeWs(GeoServerClientFactory f, string ws)
        {
            try { f.CreateWorkspaceService().DeleteWorkspaceAsync(ws, true).GetAwaiter().GetResult(); } catch { }
        }

        private static string LayerNames(GeoServerClientFactory f, string ws)
        {
            return string.Join(",", f.CreateLayerService().GetWorkspaceLayersAsync(ws).GetAwaiter().GetResult()
                .Select(l => l.Name.Contains(":") ? l.Name.Substring(l.Name.IndexOf(':') + 1) : l.Name)
                .OrderBy(n => n, StringComparer.Ordinal));
        }

        // ---------------- 3.10 设置同步（M4：SettingsCompare/Service，双连接读-比-应用-恢复） ----------------
        private static void RunSettingsSyncChecks()
        {
            Console.WriteLine("-- 设置同步（SettingsCompare 双连接）");
            string marker = "gdtest_hsync_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            try
            {
                using var a = new GeoServerClientFactory(GeoServerAvailability.Options());
                using var b = new GeoServerClientFactory(GeoServerAvailability.Options());
                var svcA = a.CreateSettingsCompareService();
                var svcB = b.CreateSettingsCompareService();

                var initial = SettingsCompareService.CompareAsync(svcA, svcB, SettingsDomain.Global).GetAwaiter().GetResult();
                Throw(Check.Cond(initial.IsIdentical, "Sync/initial", "双连接初始无差异",
                    string.Join("|", initial.Items.Select(i => i.Path))));

                var original = svcA.ReadRawAsync(SettingsDomain.Global).GetAwaiter().GetResult();
                try
                {
                    var parsed = Newtonsoft.Json.Linq.JObject.Parse(original);
                    var contact = parsed["global"]["settings"]["contact"] as Newtonsoft.Json.Linq.JObject;
                    contact["addressCity"] = marker;
                    svcA.PutRawAsync("/rest/settings", parsed.ToString(Newtonsoft.Json.Formatting.None)).GetAwaiter().GetResult();

                    var src = svcA.ReadRawAsync(SettingsDomain.Global).GetAwaiter().GetResult();
                    // 构造"另一实例"基底（该键为旧值），验证差异发现 + 选择性应用合并语义
                    var targetBase = Newtonsoft.Json.Linq.JObject.Parse(original);
                    targetBase["global"]["settings"]["contact"]["addressCity"] = "OTHER-INSTANCE";
                    var diff = SettingsCompare.Compare(SettingsDomain.Global, src, targetBase.ToString(Newtonsoft.Json.Formatting.None));
                    var item = diff.Items.FirstOrDefault(i => i.Path.EndsWith("addressCity", StringComparison.Ordinal));
                    Throw(Check.Cond(item != null && item.SourceValue.Contains(marker), "Sync/diff",
                        "差异发现 addressCity=" + marker, "差异集=" + diff.Items.Count));

                    if (item != null)
                    {
                        var payload = SettingsCompare.Apply(SettingsDomain.Global, src,
                            targetBase.ToString(Newtonsoft.Json.Formatting.None), new[] { item.Path });
                        var applied = Newtonsoft.Json.Linq.JObject.Parse(payload);
                        Throw(Check.Cond(
                            (string)applied["global"]["settings"]["contact"]["addressCity"] == marker &&
                            Equals((bool?)applied["global"]["settings"]["verbose"], (bool?)targetBase["global"]["settings"]["verbose"]),
                            "Sync/apply", "选择性应用合并：勾选路径覆写 + 未勾选键保留", "合并结果不符"));
                    }
                }
                finally
                {
                    svcA.PutRawAsync("/rest/settings", original).GetAwaiter().GetResult();
                }
                var after = SettingsCompareService.CompareAsync(svcA, svcB, SettingsDomain.Global).GetAwaiter().GetResult();
                Throw(Check.Cond(after.IsIdentical, "Sync/restore", "恢复后双连接再次无差异",
                    string.Join("|", after.Items.Select(i => i.Path))));
            }
            catch (Exception ex)
            {
                Check.Fail("Sync/crash", ex.GetType().Name + ": " + ex.Message);
            }
        }


        // ---------------- 4. 服务面 ----------------
        private static void RunServicePlaneChecks()
        {
            Console.WriteLine("-- 服务面端到端（WFS/WMS/WCS/WMTS）");
            string dataDir = TestEnv.GeneratedDataDir;
            string poly = E2ePublishHelper.Ws + ":" + E2ePublishHelper.PolyLayer;
            string lines = E2ePublishHelper.Ws + ":" + E2ePublishHelper.LinesLayer;
            string pg = E2ePublishHelper.Ws + ":" + E2ePublishHelper.PgPolyLayer;
            string dem = E2ePublishHelper.DemLayer; // WCS 2.0.1 capabilities 以裸 coverage id 列出（与 xunit 基线一致）

            Throw(RealDataChecks.WfsCapabilitiesHasTypes(poly, lines));
            Throw(RealDataChecks.WfsHitsCount(poly, 12, "poly"));
            Throw(RealDataChecks.WfsHitsCount(lines, 8, "lines"));
            Throw(RealDataChecks.WfsAttributesMatchFileHeader(dataDir, poly, "poly"));
            Throw(RealDataChecks.WfsGeometryBboxMatchesShpHeader(dataDir, poly, "gdtest_poly.shp", "poly"));
            Throw(RealDataChecks.WfsGeometryStructure(dataDir, poly, "poly"));
            Throw(RealDataChecks.WfsCqlIdGreaterThan(dataDir, poly, 100, "poly"));
            Throw(RealDataChecks.WfsBboxFilter(dataDir, poly, "poly"));
            Throw(RealDataChecks.WfsReprojectionTransforms(poly, "poly"));

            var pgR = OgcProbe.Get(TestEnv.RestBase + "/rest/layers/" + pg + ".json");
            if (pgR.Ok) Throw(RealDataChecks.WfsPostgisMatchesShapefile(dataDir, pg, poly, "pg"));
            else Check.Warn("WFS/PostGIS", "PostGIS 图层未发布（PG 或 GeoServer→PG 网络不可用），跳过交叉比对");

            Throw(RealDataChecks.WmsCapabilitiesHasLayer(poly, "1.1.1"));
            Throw(RealDataChecks.WmsCapabilitiesHasLayer(poly, "1.3.0"));
            Throw(RealDataChecks.WmsGetMapPngDims(poly, 220, 120));
            Throw(RealDataChecks.WmsRedPixels(poly, E2ePublishHelper.RedStyle));
            // 演示数据状态面实际为 topp:states（本机无 sf:states——此前 Warn 根因是图层名不存在）
            Throw(RealDataChecks.WmsDemoLayerFromCaps("topp:states", "topp-states"));
            Throw(RealDataChecks.WmsFeatureInfoPoly00(dataDir, poly, "poly"));

            Throw(RealDataChecks.WcsCapabilitiesHasCoverage(dem, "dem"));
            Throw(RealDataChecks.WcsCoverage201(dataDir, dem, "gdtest_dem.tif", "dem"));
            Throw(RealDataChecks.WcsCoverage100Compat(dem, "dem"));

            Throw(RealDataChecks.WmtsHasLayer(poly));
            Throw(RealDataChecks.WmtsTile(poly));
        }

        /// <summary>单条检查失败不中断整个 harness：记录后继续跑完全部检查（与 xunit 独立用例语义对齐）。</summary>
        private static void Throw(params CheckResult[] rs)
        {
            try { Check.ThrowOnFail(rs); }
            catch (Exception ex) { Console.WriteLine("  [x] " + ex.Message); }
        }

        // ---------------- 5. GWC 缓存 ----------------
        private static void RunGwcChecks()
        {
            Console.WriteLine("-- GWC seed/truncate");
            var gwcDir = DataEnv.GwcDir;
            string poly = E2ePublishHelper.Ws + ":" + E2ePublishHelper.PolyLayer;
            if (!Directory.Exists(gwcDir)) { Check.Warn("Gwc/dir", "宿主不可见 gwc 目录（" + gwcDir + "），跳过文件增量核对"); return; }
            // 只统计本图层缓存目录，避免其它层/历史瓦片噪声；先 truncate 清零再 seed，保证增量可见。
            string layerCache = Path.Combine(gwcDir, (E2ePublishHelper.Ws + "_" + E2ePublishHelper.PolyLayer).Replace(':', '_'));
            var truncPre = "<seedRequest><name>" + poly + "</name><zoomStart>0</zoomStart><zoomStop>1</zoomStop>" +
                           "<format>image/png</format><type>truncate</type><threadCount>1</threadCount></seedRequest>";
            OgcProbe.Post(OgcProbe.GwcRest("seed/" + Uri.EscapeDataString(poly)), truncPre, "application/xml");
            Thread.Sleep(4000);
            int before = CountFiles(layerCache);
            var seedXml = "<seedRequest><name>" + poly + "</name><zoomStart>0</zoomStart><zoomStop>1</zoomStop>" +
                          "<format>image/png</format><type>seed</type><threadCount>1</threadCount></seedRequest>";
            var post = OgcProbe.Post(OgcProbe.GwcRest("seed/" + Uri.EscapeDataString(poly)), seedXml, "application/xml");
            if (!post.Ok && post.Status != 201)
            { Check.Warn("Gwc/seed", "seed POST HTTP " + post.Status + "（GWC REST 形态按现状 Warn）"); return; }
            int after = before;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.Elapsed < TimeSpan.FromSeconds(60))
            {
                after = CountFiles(layerCache);
                if (after > before) break;
                Thread.Sleep(2000);
            }
            Check.Cond(after > before, "Gwc/seed-increment", $"图层缓存文件 {before}→{after}（{layerCache}）", "seed 后无新瓦片文件");
            // 实测：GWC 1.x/2.x REST 无 /truncate/{layer} 端点（404）；单图层清除的正确形态是
            // POST /gwc/rest/seed/{layer} 且 body 必须含 zoomStart/zoomStop（缺省字段服务端 NPE 500）。
            var truncXml = "<seedRequest><name>" + poly + "</name><zoomStart>0</zoomStart><zoomStop>1</zoomStop>" +
                           "<format>image/png</format><type>truncate</type><threadCount>1</threadCount></seedRequest>";
            var trunc = OgcProbe.Post(OgcProbe.GwcRest("seed/" + Uri.EscapeDataString(poly)), truncXml, "application/xml");
            Check.Cond(trunc.Ok, "Gwc/truncate", "单图层 truncate（seed type=truncate）HTTP " + trunc.Status,
                "HTTP " + trunc.Status);
        }

        private static int CountFiles(string dir)
        {
            try { return Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length; }
            catch { return -1; }
        }

        // ---------------- 6. 外部真实数据（可选） ----------------
        private static void RunExternalRealData()
        {
            Console.WriteLine("-- 外部真实数据泛化检查");
            // 头交叉校验已在数据完整性段完成；服务面发布需数据位于容器 data_dir 挂载内：
            if (!IsUnder(DataEnv.ContainerDataRoot, TestEnv.RealDataDir))
            { Check.Warn("Ext/service", "外部目录不在容器 data_dir 挂载内，仅做文件头校验（服务面跳过）"); return; }
            // 相对 data_dir 的 file: 引用（正斜杠——容器内 Linux 路径语义；旧实现对多层子目录会产出反斜杠而失效）
            string rel = DataEnv.HostPathToDataDirRef(TestEnv.RealDataDir);
            var ws = "gdtest_ext";
            PublishViaLib(ws, "gdtest_ext_ds", rel);
            foreach (var (shp, dbf) in DiscoverPairs(TestEnv.RealDataDir))
            {
                string layer = Path.GetFileNameWithoutExtension(shp);
                int expected = DbfHeader.Parse(dbf).RecordCount;
                try
                {
                    PublishFeatureType(ws, "gdtest_ext_ds", layer);
                    Throw(RealDataChecks.WfsHitsCount(ws + ":" + layer, expected, "ext/" + layer));
                }
                catch (Exception ex) { Check.Warn("Ext/" + layer, ex.Message); }
            }
            try
            {
                using var f = new GeoServerClientFactory(GeoServerAvailability.Options());
                f.CreateWorkspaceService().DeleteWorkspaceAsync(ws, true).GetAwaiter().GetResult();
            }
            catch { }
        }

        private static bool IsUnder(string root, string dir)
        {
            var r = Path.GetFullPath(root).TrimEnd('\\', '/');
            var d = Path.GetFullPath(dir).TrimEnd('\\', '/');
            return d.StartsWith(r + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                   d.Equals(r, StringComparison.OrdinalIgnoreCase);
        }

        private static void PublishViaLib(string ws, string store, string dirRef)
        {
            using var f = new GeoServerClientFactory(GeoServerAvailability.Options());
            var wsvc = f.CreateWorkspaceService();
            // 注意：存在性探测必须 GetAwaiter().GetResult() 等待——否则异常不被观察，catch 不执行（工作空间/store 永不创建）。
            try { wsvc.GetWorkspaceAsync(ws).GetAwaiter().GetResult(); }
            catch { wsvc.CreateWorkspaceAsync(ws).GetAwaiter().GetResult(); }
            var ds = f.CreateDataStoreService();
            try { ds.GetDataStoreAsync(ws, store).GetAwaiter().GetResult(); }
            catch
            {
                ds.CreateDataStoreAsync(ws, new GeoServerDesktop.GeoServerClient.Models.DataStore
                {
                    Name = store,
                    Enabled = true,
                    ConnectionParameters = new GeoServerDesktop.GeoServerClient.Models.ConnectionParameters
                    {
                        Entries = new[]
                        {
                            new GeoServerDesktop.GeoServerClient.Models.ConnectionParameterEntry { Key = "namespace", Value = "http://" + ws },
                            new GeoServerDesktop.GeoServerClient.Models.ConnectionParameterEntry { Key = "url", Value = dirRef },
                        }
                    }
                }).GetAwaiter().GetResult();
            }
        }

        private static void PublishFeatureType(string ws, string store, string layer)
        {
            using var f = new GeoServerClientFactory(GeoServerAvailability.Options());
            var ft = f.CreateFeatureTypeService();
            try { ft.GetFeatureTypeAsync(ws, store, layer).GetAwaiter().GetResult(); return; }
            catch (GeoServerDesktop.GeoServerClient.Http.GeoServerRequestException)
            {
                ft.CreateFeatureTypeAsync(ws, store, new GeoServerDesktop.GeoServerClient.Models.FeatureType
                {
                    Name = layer,
                    NativeName = layer,
                    Enabled = true,
                }).GetAwaiter().GetResult();
            }
        }

        // ---------------- 3.11 扩展真实数据集保真（字段类型/编码/几何形态/规模/转义/负路径） ----------------
        private static void RunExtendedFidelityChecks()
        {
            Console.WriteLine("-- 扩展真实数据集保真（gdtest_vec / gdtest_vol / gdtest_bad）");
            if (!GeoServerAvailability.IsGeoServerReachable)
            { Check.Warn("ExtFix", "GeoServer 不可达，扩展保真检查跳过"); return; }
            if (!ExtendedFixture.DataPresent)
            { Check.Warn("ExtFix", "扩展数据集缺失：请先运行 tests/testdata/generate_testdata.py（生成 gdtest_vec/img/vol/bad）"); return; }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var fx = new ExtendedFixture();
            var sets = ExtendedFixture.Sets().Where(s => !s.IsCoverage).ToList();
            try
            {
                fx.EnsureWorkspace();
                foreach (var s in sets)
                {
                    if (s.DirKey == "bad") continue;                     // 脏数据在负路径段处理
                    var r = fx.EnsurePublished(s);
                    Check.Cond(r.Success, "ExtFix/publish:" + s.LayerName, "发布成功 " + s.LayerName, r.Message);
                }
                Console.WriteLine("   发布 " + fx.PublishTimings.Count + " 个数据集，累计 "
                                  + (int)sw.Elapsed.TotalSeconds + "s");

                var byName = sets.ToDictionary(s => s.LayerName);
                var types = byName["gdtest_x_types"];
                var utf8 = byName["gdtest_x_enc_utf8"];
                var gbkCpg = byName["gdtest_x_enc_gbk_cpg"];
                var gbkNo = byName["gdtest_x_enc_gbk_nocpg"];
                var points = byName["gdtest_x_points"];
                var polyz = byName["gdtest_x_polyz"];
                var selfint = byName["gdtest_x_selfint"];
                var empty = byName["gdtest_x_empty"];
                var huge = byName["gdtest_x_huge"];
                var cjk = byName["湖泊 与 水库"];
                var pts20k = byName["gdtest_x_pts20k"];

                // 1) 逐字段属性保真（文本/整型/浮点/日期/逻辑型/NULL）
                Step("Attrs/types");
                Throw(RealDataFidelity.AttributesAllFields(ExtendedFixture.VecDir, "gdtest_types", types.Qualified, "types"));
                // 2) DBF 编码三变体：声明/探测真值 + 归因
                Step("Attrs/enc-utf8");
                Throw(RealDataFidelity.AttributesAllFields(ExtendedFixture.VecDir, "enc_utf8", utf8.Qualified, "enc-utf8"));
                Throw(RealDataFidelity.AttributesAllFields(ExtendedFixture.VecDir, "enc_gbk_cpg", gbkCpg.Qualified, "enc-gbk+cpg"));
                Step("Enc/gbk-nocpg 基线");
                Throw(RealDataFidelity.EncodingRiskBaseline(ExtendedFixture.VecDir, "enc_gbk_nocpg", gbkNo.Qualified, "gbk-nocpg", expectRisk: true));
                Throw(RealDataFidelity.EncodingRiskBaseline(ExtendedFixture.VecDir, "enc_utf8", utf8.Qualified, "utf8-cpg", expectRisk: false));
                Throw(RealDataFidelity.EncodingRiskBaseline(ExtendedFixture.VecDir, "enc_gbk_cpg", gbkCpg.Qualified, "gbk-cpg", expectRisk: false));
                // 3) 几何形态
                Step("NullGeom/points");
                Throw(RealDataFidelity.NullGeometryHandling(ExtendedFixture.VecDir, "gdtest_points", points.Qualified, "points"));
                Step("Z/polyz");
                Throw(RealDataFidelity.ZDimensionPreserved(ExtendedFixture.VecDir, "gdtest_polyz", polyz.Qualified, "polyz"));
                Step("SelfInt/selfint");
                Throw(RealDataFidelity.SelfIntersectionPreserved(ExtendedFixture.VecDir, "gdtest_selfint", selfint.Qualified, "selfint"));
                Step("Empty/empty");
                Throw(RealDataFidelity.EmptyLayerBehavior(ExtendedFixture.VecDir, "gdtest_empty", empty.Qualified, "empty"));
                Step("Verts/huge");
                Throw(RealDataFidelity.VertexCountParity(ExtendedFixture.VecDir, "gdtest_huge", huge.Qualified, "huge"));
                Step("Spatial/huge");
                Throw(RealDataFidelity.SpatialPredicatesMatch(ExtendedFixture.VecDir, "gdtest_huge", huge.Qualified, "huge"));
                Step("Spatial/types");
                Throw(RealDataFidelity.SpatialPredicatesMatch(ExtendedFixture.VecDir, "gdtest_types", types.Qualified, "types"));
                // 4) CJK / 含空格图层名全链路
                Step("CjkChain");
                Throw(RealDataFidelity.CjkNameChain(ExtendedFixture.VecDir, "湖泊 与 水库", cjk.Qualified, "cjk-layer"));
                // 5) 大表分页与计时基线
                Step("Paging/pts20k");
                Throw(RealDataFidelity.PagingConsistency(ExtendedFixture.VolDir, "gdtest_pts20k", pts20k.Qualified, "pts20k"));
                Step("Timing/pts20k");
                Throw(RealDataFidelity.TimingBaseline(pts20k.Qualified,
                                    DbfHeader.Parse(System.IO.Path.Combine(ExtendedFixture.VolDir, "gdtest_pts20k.dbf")).RecordCount, "pts20k"));
                // 6) 重投影自洽（原生 4326 → 3857 + 能力表 LL bbox）
                Step("Reproj/huge");
                Throw(RealDataFidelity.ReprojectionConsistency(ExtendedFixture.VecDir, "gdtest_huge", huge.Qualified, "huge"));
                Step("NativeBbox");
                Throw(RealDataFidelity.NativeBboxMatchesShpHeader(ExtendedFixture.VecDir, "gdtest_types",
                    ExtendedFixture.Ws, ExtendedFixture.StoreFor("vec"), types.LayerName, "types"));
                Throw(RealDataFidelity.NativeBboxMatchesShpHeader(ExtendedFixture.VolDir, "gdtest_pts20k",
                    ExtendedFixture.Ws, ExtendedFixture.StoreFor("vol"), pts20k.LayerName, "pts20k"));

                Step("Reproj/pts20k(4490)");
                // EPSG:4490（CGCS2000 地理坐标）与 4326 基准近乎一致：只核对范围自洽，不要求数值差异
                Throw(RealDataFidelity.ReprojectionConsistency(ExtendedFixture.VolDir, "gdtest_pts20k", pts20k.Qualified, "pts20k",
                    nativeCrs: "EPSG:4326", targetCrs: "EPSG:4490", expectTransform: false));
                Step("Axis/contract");
                Throw(RealDataFidelity.SpatialLiteralAxisOrderContract(ExtendedFixture.VecDir, "gdtest_huge", huge.Qualified, "huge"));
                // 7) 负路径：脏数据必须可诊断失败；脏目录中的合法对必须可发布
                foreach (var s in ExtendedFixture.Sets().Where(x => x.ExpectFailure))
                {
                    Step("Neg/" + s.BaseName);
                    Throw(RealDataFidelity.DiagnosableFailure(fx, s, s.BaseName));
                }
                // 缺 .prj 但数据本身合法：允许发布，但客户端预检必须把"投影未声明"报出来
                foreach (var s in ExtendedFixture.Sets().Where(x => x.BaseName == "bd_noprj"))
                {
                    Step("NoPrj/" + s.BaseName);
                    Throw(RealDataFidelity.MissingPrjSurfaced(s.HostDir, s.BaseName, "noprj"));
                }
                foreach (var s in ExtendedFixture.Sets().Where(x => x.BaseName == "bd_valid"))
                {
                    Step("Pos/" + s.BaseName);
                    Throw(RealDataFidelity.PositiveControlInDirtyDir(fx, s, "bd_valid"));
                }
            }
            catch (Exception ex)
            {
                Check.Fail("ExtFix/crash", ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                if (!ExtendedFixture.Keep) ExtendedFixture.Wipe(fx.Factory);
                else Console.WriteLine("   GSD_KEEP=1：保留 " + ExtendedFixture.Ws + " 供手工复现");
            }
        }

        // ---------------- 3.12 外部真实数据泛化保真（GSD_REAL_DATA_DIR，期望值全部来自文件本体） ----------------
        private static void RunExternalFidelityChecks()
        {
            var dir = TestEnv.RealDataDir;
            if (string.IsNullOrEmpty(dir) || !System.IO.Directory.Exists(dir)) return;
            Console.WriteLine("-- 外部真实数据泛化保真");
            var ref0 = DataEnv.HostPathToDataDirRef(dir);
            if (ref0 == null) { Check.Warn("ExtFid", "外部数据目录不在容器 data_dir 挂载内，泛化保真跳过"); return; }
            var pairs = DataEnv.DiscoverShapefilePairs(dir);
            if (pairs.Count == 0) { Check.Warn("ExtFid", "未发现成对 .shp/.dbf"); return; }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var f = new GeoServerClientFactory(GeoServerAvailability.Options());
            try
            {
                ExternalRealData.EnsureWorkspace(f);
                int done = 0;
                foreach (var (shp, dbf) in pairs)
                {
                    string name = System.IO.Path.GetFileNameWithoutExtension(shp);
                    string layer = ExternalRealData.Ws + ":" + name;
                    Step("ExtFid/" + name);
                    // 一律经产品向导发布（不手拼 store 参数：实测多给一个 namespace 项就会让
                    // 服务端匹配不到 CJK 基名，报 “no attributes were specified”）
                    var pubDiag = ExternalRealData.PublishViaWizard(f, ref0, name);
                    if (Check.Cond(pubDiag == null, "ExtFid/publish:" + name, "真实数据层经向导发布可用（" + layer + "）",
                        "该真实数据层未能发布/回读：" + pubDiag).Status != CheckStatus.Pass) continue;
                    var hostDir = System.IO.Path.GetDirectoryName(shp);
                    // 自定义投影真实数据的 GeoJSON 流完整性契约（服务端缺陷基线 + 规避路径证据）
                    Throw(RealDataFidelity.GeoJsonStreamIntegrity(hostDir, name, layer, name));
                    // 逐字段属性保真（中文 DBF：.cpg 或字节探测推导真值）；显式 CRS 规避截断
                    Throw(RealDataFidelity.AttributesAllFields(hostDir, name, layer, name, maxRecords: 120,
                        srsName: "EPSG:4326", encodingBaselineAsWarn: true));
                    // 几何保真：自定义投影图层的 GeoJSON 会被服务端截断（见 JsonStream 契约），
                    // 几何面因此走 GML 通道（原生坐标、完整响应），判据对环闭合/Multi* 归一不敏感
                    Throw(RealDataFidelity.GeometryParityViaGml(hostDir, name, layer, name));
                    // 空间谓词一致性（派生内点 → 本地判定集合 == 服务命中集合）
                    // 真实数据多为投影系（自定义 CRS 无 EPSG 权威码）→ 用原生字面量形式
                    Throw(RealDataFidelity.SpatialPredicatesMatch(hostDir, name, layer, name, keyField: null,
                        geographicLiteral: false, assertBbox: false));
                    Throw(RealDataFidelity.NativeBboxMatchesShpHeader(hostDir, name, ExternalRealData.Ws,
                        ExternalRealData.Store, name, name));
                    // 重投影自洽（自定义投影 → 4326 落全球范围 + 能力表 LL bbox 一致）
                    Throw(RealDataFidelity.ReprojectionConsistency(hostDir, name, layer, name,
                        nativeCrs: "(native)", targetCrs: "EPSG:4326", nativeGeographic: false,
                        fallbackSrsName: "EPSG:4326"));
                    done++;
                }
                Check.Pass("ExtFid/swept", $"泛化保真覆盖 {done}/{pairs.Count} 个真实 shapefile（累计 {sw.ElapsedMilliseconds}ms）");
            }
            catch (Exception ex)
            {
                Check.Fail("ExtFid/crash", ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                if (!ExtendedFixture.Keep) ExternalRealData.Wipe(f);
                else Console.WriteLine("   GSD_KEEP=1：保留 " + ExternalRealData.Ws + " 供手工复现");
            }
        }

        // ---------------- 4. 真实栅格保真（多波段/整型/nodata/瓦片+概览/CJK 名） ----------------
        private static void RunRasterChecks()
        {
            Console.WriteLine("-- 栅格真实数据保真（gdtest_img）");
            if (!GeoServerAvailability.IsGeoServerReachable) { Check.Warn("Raster", "GeoServer 不可达，跳过"); return; }
            if (!System.IO.Directory.Exists(ExtendedFixture.ImgDir))
            { Check.Warn("Raster", "栅格数据集缺失：请先运行 tests/testdata/generate_testdata.py"); return; }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var fx = new ExtendedFixture();
            try
            {
                fx.EnsureWorkspace();
                foreach (var s in ExtendedFixture.Sets().Where(x => x.IsCoverage))
                {
                    Step("Raster/publish " + s.LayerName);
                    var r = fx.EnsurePublished(s);
                    Check.Cond(r.Success, "Raster/publish:" + s.LayerName, "栅格发布成功 " + s.LayerName, r.Message);
                    if (!r.Success) continue;
                    var tif = System.IO.Path.Combine(s.HostDir, s.BaseName);
                    Step("Raster/formula " + s.BaseName);
                    Throw(RealDataRaster.SourceMatchesFormula(tif, s.LayerName, (px, py, b) => RasterFormula(s.BaseName, px, py, b)));
                    Step("Raster/rest " + s.BaseName);
                    Throw(RealDataRaster.CoverageRestMetadata(tif, ExtendedFixture.Ws,
                        ExtendedFixture.CoverageStoreFor(s), s.LayerName, s.LayerName));
                    Step("Raster/wcs-desc " + s.BaseName);
                    Throw(RealDataRaster.WcsDescribeMatchesSource(tif, s.Qualified, s.LayerName));
                    Step("Raster/wcs-pix " + s.BaseName);
                    Throw(RealDataRaster.WcsPixelsMatchSource(tif, s.Qualified, s.LayerName));
                    Step("Raster/wms " + s.BaseName);
                    Throw(RealDataRaster.WmsRasterRenders(tif, s.Qualified, s.LayerName));
                    Step("Raster/wmts " + s.BaseName);
                    Throw(RealDataRaster.WmtsRasterTile(s.Qualified, s.LayerName));
                }
                Console.WriteLine("   栅格段累计 " + (int)sw.Elapsed.TotalSeconds + "s");
            }
            catch (Exception ex) { Check.Fail("Raster/crash", ex.GetType().Name + ": " + ex.Message); }
            finally { if (!ExtendedFixture.Keep) ExtendedFixture.Wipe(fx.Factory); }
        }

        /// <summary>栅格像元生成公式（与 tests/testdata/generate_testdata_extra.py 同源；仅用于数据完整性核对）。</summary>
        private static double RasterFormula(string tifName, int px, int py, int band)
        {
            if (tifName == "gdtest_rgb.tif")
            {
                if (band == 1) return (px * 3 + py) % 251 + 1;
                if (band == 2) return (px + py * 5) % 241 + 10;
                if (px < 8 && py < 6) return 0;
                return (px * py) % 200 + 50;
            }
            if (tifName == "gdtest_int16.tif")
                return px < 10 && py < 10 ? -9999 : (px * py) % 3000 - 500;
            return (px * 2 + py * 3) % 256;                       // 中文 栅格.tif
        }

        private static string SanitizeTag(string name)
        {
            var sb = new StringBuilder();
            foreach (var c in name) sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            return sb.ToString();
        }

        // ---------------- 5. 残留审计（删除路径的真实数据检验） ----------------
        private static void RunResidueAudit()
        {
            Console.WriteLine("-- 残留审计（gdtest_* 孤儿资源）");
            if (!GeoServerAvailability.IsGeoServerReachable) { Check.Warn("Audit", "GeoServer 不可达，跳过"); return; }
            const string Shared = "gdtest_ws_e2e";
            var probs = new List<string>();

            var wsList = JsonNames(TestEnv.RestBase + "/rest/workspaces.json", "workspaces", "workspace");
            var orphans = wsList.Where(n => n.StartsWith("gdtest", StringComparison.OrdinalIgnoreCase) && n != Shared
                                          && !n.EndsWith("_e2e", StringComparison.OrdinalIgnoreCase)).ToList();
            Check.Cond(orphans.Count == 0, "Audit/workspaces",
                $"工作空间无残留（共 {wsList.Count} 个，仅保留共享夹具 {Shared}）",
                "残留工作空间：" + string.Join(",", orphans));

            // 共享 e2e 夹具（gdtest_ws_e2e / gdtest_red_e2e）按设计跨进程复用，不算残留
            var styles = JsonNames(TestEnv.RestBase + "/rest/styles.json", "styles", "style")
                .Where(n => n.StartsWith("gdtest", StringComparison.OrdinalIgnoreCase)
                            && !n.EndsWith("_e2e", StringComparison.OrdinalIgnoreCase)).ToList();
            Check.Cond(styles.Count == 0, "Audit/styles", "无 gdtest 样式残留（共享 e2e 夹具除外）", "残留样式：" + string.Join(",", styles));

            var layers = JsonNames(TestEnv.RestBase + "/rest/layers.json", "layers", "layer")
                .Where(n => n.Contains("gdtest_x") || n.StartsWith("gdtest_wiz", StringComparison.OrdinalIgnoreCase)
                            || n.StartsWith("hbatch_", StringComparison.OrdinalIgnoreCase) || n.StartsWith("hmig_", StringComparison.OrdinalIgnoreCase))
                .ToList();
            Check.Cond(layers.Count == 0, "Audit/layers", "无扩展/向导/批量/迁移临时图层残留", "残留图层：" + string.Join(",", layers));

            // 用户域与 GWC：本轮 harness 不写用户，若残留即为泄漏
            var users = JsonNames(TestEnv.RestBase + "/rest/security/users/", "users", "user")
                .Where(n => n.StartsWith("gdtest", StringComparison.OrdinalIgnoreCase)).ToList();
            Check.Cond(users.Count == 0, "Audit/users", "无 gdtest 用户残留", "残留用户：" + string.Join(",", users));

            // 数据目录侧：本轮生成的 layergroups/gwc 缓存孤儿目录（仅统计，不判定——服务端会保留已发布层的缓存）
            var gwcDir = DataEnv.GwcDir;
            if (System.IO.Directory.Exists(gwcDir))
            {
                var dirs = System.IO.Directory.GetDirectories(gwcDir)
                    .Select(System.IO.Path.GetFileName)
                    .Where(n => n.StartsWith("gdtest_ws_x", StringComparison.OrdinalIgnoreCase)
                                || n.StartsWith("gdtest_ext", StringComparison.OrdinalIgnoreCase)
                                || n.StartsWith("gdtest_ws_wiz", StringComparison.OrdinalIgnoreCase)
                                || n.StartsWith("gdtest_ws_hbatch", StringComparison.OrdinalIgnoreCase)
                                || n.StartsWith("gdtest_ws_hmig", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                Check.Cond(dirs.Count == 0, "Audit/gwc-cache",
                    "已删除图层在宿主 GWC 目录无缓存残留目录",
                    "GWC 残留目录：" + string.Join(",", dirs));
            }
        }

        private static List<string> JsonNames(string url, string root, string item)
        {
            var list = new List<string>();
            try
            {
                var r = OgcProbe.Get(url);
                if (!r.Ok || r.Text == null) return list;
                using var doc = System.Text.Json.JsonDocument.Parse(r.Text);
                if (doc.RootElement.TryGetProperty(root, out var g) && g.ValueKind == System.Text.Json.JsonValueKind.Object
                    && g.TryGetProperty(item, out var arr) && arr.ValueKind == System.Text.Json.JsonValueKind.Array)
                    foreach (var e in arr.EnumerateArray())
                        if (e.TryGetProperty("name", out var n)) list.Add(n.GetString() ?? "");
            }
            catch { }
            return list;
        }

        // ---------------- 诊断段（--only diag）：把服务侧响应原文完整落盘，避免 shell/截断误导 ----------------
        private static void RunDiagnosticDump()
        {
            Console.WriteLine("-- 诊断（服务响应原文）");
            var dir = TestEnv.RealDataDir;
            if (string.IsNullOrEmpty(dir) || !System.IO.Directory.Exists(dir))
            { Check.Warn("Diag", "未提供 GSD_REAL_DATA_DIR"); return; }
            var ref0 = DataEnv.HostPathToDataDirRef(dir);
            var pairs = DataEnv.DiscoverShapefilePairs(dir);
            using var f = new GeoServerClientFactory(GeoServerAvailability.Options());
            var sb = new StringBuilder();
            try
            {
                ExternalRealData.EnsureWorkspace(f);
                foreach (var (shp, dbf) in pairs.Take(3))
                {
                    string name = System.IO.Path.GetFileNameWithoutExtension(shp);
                    string layer = ExternalRealData.Ws + ":" + name;
                    sb.AppendLine("###### " + name);
                    sb.AppendLine("publish: " + (ExternalRealData.PublishViaWizard(f, ref0, name) ?? "OK"));
                    var rest = OgcProbe.Get(TestEnv.RestBase + "/rest/workspaces/" + ExternalRealData.Ws
                        + "/datastores/" + ExternalRealData.Store + "/featuretypes.json");
                    sb.AppendLine("store fts: " + Trim(rest.Text, 400));
                    var ft = OgcProbe.Get(TestEnv.RestBase + "/rest/workspaces/" + ExternalRealData.Ws
                        + "/datastores/" + ExternalRealData.Store + "/featuretypes/" + Uri.EscapeDataString(name) + ".json");
                    sb.AppendLine("ft GET " + ft.Status + ": " + Trim(ft.Text, 700));
                    var hits = OgcProbe.Get(OgcProbe.Wfs("request=GetFeature", "version=2.0.0", "resultType=hits",
                        "outputFormat=application/json", "typeName=" + layer, "count=1"));
                    sb.AppendLine("HITS " + hits.Status + " ct=" + hits.ContentType + ": " + Trim(hits.Text, 700));
                    var full = OgcProbe.Get(OgcProbe.Wfs("request=GetFeature", "version=2.0.0",
                        "outputFormat=application/json", "typeName=" + layer, "count=120"));
                    sb.AppendLine("FULL " + full.Status + " len=" + (full.Bytes?.Length ?? -1) + ": " + Trim(full.Text, 400));
                    var v11 = OgcProbe.Get(OgcProbe.Wfs("request=GetFeature", "version=1.1.1",
                        "outputFormat=application/json", "typeName=" + layer, "maxFeatures=2"));
                    sb.AppendLine("V111 " + v11.Status + ": " + Trim(v11.Text, 400));
                    var lyr = OgcProbe.Get(TestEnv.RestBase + "/rest/layers/" + Uri.EscapeDataString(layer) + ".json");
                    sb.AppendLine("LAYER GET " + lyr.Status + ": " + Trim(lyr.Text, 300));
                    sb.AppendLine();
                }
            }
            catch (Exception ex) { sb.AppendLine("EX " + ex); }
            finally
            {
                if (!ExtendedFixture.Keep) ExternalRealData.Wipe(f);
            }
            string dumpPath = Path.Combine(Path.GetTempPath(), "gsd_diag_dump.txt");
            try { File.WriteAllText(dumpPath, sb.ToString(), Encoding.UTF8); Console.WriteLine("   诊断输出：" + dumpPath); }
            catch (Exception ex) { Console.WriteLine(sb.ToString()); Console.WriteLine("写文件失败：" + ex.Message); }
        }

        private static string Trim(string s, int n)
        {
            if (s == null) return "(null)";
            s = s.Replace('\r', ' ').Replace('\n', ' ');
            return s.Length > n ? s.Substring(0, n) + "..." : s;
        }

        // ---------------- 汇总 ----------------
        private static int ReportAndExit()
        {
            Console.WriteLine();
            Console.WriteLine("== 汇总 ==");
            foreach (var g in new[] { CheckStatus.Fail, CheckStatus.Warn, CheckStatus.Pass })
            {
                var items = Check.All.Where(r => r.Status == g).ToList();
                Console.WriteLine($"  {g}: {items.Count}");
                if (g != CheckStatus.Pass) foreach (var i in items) Console.WriteLine("    " + i);
            }
            if (SkipLog.Entries.Count > 0)
            {
                Console.WriteLine($"  SKIPPED(登记): {SkipLog.Entries.Count}");
                foreach (var e in SkipLog.Entries) Console.WriteLine("    [SKIP] " + e);
            }
            var (p, w, f2) = Check.Summarize();
            Console.WriteLine($"总计: Pass={p} Warn={w} Fail={f2}");
            return f2 > 0 ? 1 : 0;
        }
    }
}
