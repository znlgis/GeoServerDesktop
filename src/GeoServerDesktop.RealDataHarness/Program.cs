using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Xunit;
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.Tests.Infrastructure;
using GeoServerDesktop.Tests.RealData;

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
        static int Main(string[] args)
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
        static void RunEnvChecks()
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
        static void RunDataIntegrityChecks()
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

        static IEnumerable<(string shp, string dbf)> DiscoverPairs(string dir) =>
            Directory.EnumerateFiles(dir, "*.shp", SearchOption.AllDirectories)
                .Where(f => File.Exists(Path.ChangeExtension(f, ".dbf")))
                .Select(f => (f, Path.ChangeExtension(f, ".dbf")));

        static void CheckExternalHeaders(string dir)
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
        static void RunPublication()
        {
            Console.WriteLine("-- 发布（被测客户端库路径）");
            E2ePublishHelper.EnsurePublished(new GeoServerFixture());
            var r = OgcProbe.Get(TestEnv.RestBase + "/rest/layers/" + E2ePublishHelper.Ws + ":" + E2ePublishHelper.PolyLayer + ".json");
            Check.Cond(r.Ok, "Publish/layer-rest", "shapefile 图层经独立通道可见", "HTTP " + r.Status);
        }

        // ---------------- 4. 服务面 ----------------
        static void RunServicePlaneChecks()
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
        static void Throw(params CheckResult[] rs)
        {
            try { Check.ThrowOnFail(rs); }
            catch (Exception ex) { Console.WriteLine("  [x] " + ex.Message); }
        }

        // ---------------- 5. GWC 缓存 ----------------
        static void RunGwcChecks()
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

        static int CountFiles(string dir)
        {
            try { return Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length; }
            catch { return -1; }
        }

        // ---------------- 6. 外部真实数据（可选） ----------------
        static void RunExternalRealData()
        {
            Console.WriteLine("-- 外部真实数据泛化检查");
            // 头交叉校验已在数据完整性段完成；服务面发布需数据位于容器 data_dir 挂载内：
            if (!IsUnder(DataEnv.ContainerDataRoot, TestEnv.RealDataDir))
            { Check.Warn("Ext/service", "外部目录不在容器 data_dir 挂载内，仅做文件头校验（服务面跳过）"); return; }
            string rel = "file:" + Uri.UnescapeDataString(new Uri(DataEnv.ContainerDataRoot + Path.DirectorySeparatorChar)
                .MakeRelativeUri(new Uri(TestEnv.RealDataDir + Path.DirectorySeparatorChar)).ToString()).Replace('/', '\\').TrimEnd('\\');
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

        static bool IsUnder(string root, string dir)
        {
            var r = Path.GetFullPath(root).TrimEnd('\\', '/');
            var d = Path.GetFullPath(dir).TrimEnd('\\', '/');
            return d.StartsWith(r + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                   d.Equals(r, StringComparison.OrdinalIgnoreCase);
        }

        static void PublishViaLib(string ws, string store, string dirRef)
        {
            using var f = new GeoServerClientFactory(GeoServerAvailability.Options());
            var wsvc = f.CreateWorkspaceService();
            try { wsvc.GetWorkspaceAsync(ws); } catch { wsvc.CreateWorkspaceAsync(ws).GetAwaiter().GetResult(); }
            var ds = f.CreateDataStoreService();
            try { ds.GetDataStoreAsync(ws, store); }
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

        static void PublishFeatureType(string ws, string store, string layer)
        {
            using var f = new GeoServerClientFactory(GeoServerAvailability.Options());
            var ft = f.CreateFeatureTypeService();
            try { ft.GetFeatureTypeAsync(ws, store, layer).GetAwaiter().GetResult(); return; }
            catch (GeoServerDesktop.GeoServerClient.Http.GeoServerRequestException)
            {
                ft.CreateFeatureTypeAsync(ws, store, new GeoServerDesktop.GeoServerClient.Models.FeatureType
                {
                    Name = layer, NativeName = layer, Enabled = true,
                }).GetAwaiter().GetResult();
            }
        }

        // ---------------- 汇总 ----------------
        static int ReportAndExit()
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
