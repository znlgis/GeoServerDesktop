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
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Check.Reset();

            Console.WriteLine("== GeoServerDesktop RealData Harness ==");
            Console.WriteLine($"GeoServer : {TestEnv.BaseUrl}");
            Console.WriteLine($"DataDir   : {TestEnv.GeneratedDataDir}");
            Console.WriteLine($"RealDir   : {TestEnv.RealDataDir ?? "(未提供)"}");
            Console.WriteLine();

            var fixture = new GeoServerFixture();
            try
            {
                RunEnvChecks();
                RunDataIntegrityChecks();
                if (GeoServerAvailability.IsGeoServerReachable)
                {
                    RunPublication();
                    RunWizardPublishChecks();
                    RunStyleChecks();
                    RunBatchChecks();
                    RunMigrationChecks();
                    RunSettingsSyncChecks();
                    RunServicePlaneChecks();
                    RunGwcChecks();
                }
                else
                {
                    Check.Warn("Harness/GeoServer", "GeoServer 不可达，服务面检查全部跳过");
                }
                if (!string.IsNullOrEmpty(TestEnv.RealDataDir)) RunExternalRealData();
            }
            catch (Exception ex)
            {
                Check.Fail("Harness/Crash", ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                try { fixture.Dispose(); } catch { }
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
