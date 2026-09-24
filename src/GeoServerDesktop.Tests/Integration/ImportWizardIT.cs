using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Import;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// 数据导入向导（ImportWizardService）发布闭环：向导路径发布 → REST 复核 → WFS 计数比对。
    /// 期望值独立推导：矢量计数取 DBF 头记录数（不经被测库）；栅格范围取生成器确定性参数
    /// （W=81/H=41/RES=100，原点 500000/2200060，north-up）。WFS 与覆盖度复核走裸 HTTP（不经被测库）。
    /// 命名族缩写：wiz；资源名全局唯一——发布名带 _wiz_ 前缀，nativeName 指真实文件/表。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class ImportWizardIT : GeoServerTestBase
    {
        public ImportWizardIT(GeoServerFixture fx) : base(fx) { }

        private const string Ws = TestEnv.Prefix + "_ws_wiz";
        private const string PolyStore = TestEnv.Prefix + "_ds_wiz";
        private const string PolyLayer = TestEnv.Prefix + "_wiz_poly";
        private const string PolyNative = TestEnv.Prefix + "_poly";
        private const string DemStore = TestEnv.Prefix + "_cs_wiz";
        private const string DemLayer = TestEnv.Prefix + "_wiz_dem";
        private const string DemNative = TestEnv.Prefix + "_dem";
        private const string PgStore = TestEnv.Prefix + "_ds_wiz_pg";
        private const string PgLayer = TestEnv.Prefix + "_wiz_pg_poly";

        [Fact]
        public async Task Shapefile_WizardPublish_Closed_Loop_With_Wfs_Count()
        {
            if (!RequireGeoServer()) return;
            Assert.True(TestEnv.GeneratedDataExists(), "缺少测试数据目录 " + TestEnv.GeneratedDataDir);
            using var f = Fx.Factory();
            var wiz = f.CreateImportWizardService();
            try
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
                await f.CreateWorkspaceService().CreateWorkspaceAsync(Ws);

                var req = new ImportSourceRequest
                {
                    Kind = ImportDataSourceKind.ShapefileDirectory,
                    Workspace = Ws,
                    LayerName = PolyLayer,
                    NativeName = PolyNative,
                    StoreName = PolyStore,
                    FileRef = "file:" + TestEnv.Prefix + "_data",
                    Srs = "EPSG:4326",
                };

                var r = await wiz.PublishShapefileAsync(req);
                Assert.True(r.Success, r.Message);
                Assert.Equal(Ws + ":" + PolyLayer, r.QualifiedName);

                // REST 复核：存储类型与要素类型原始名
                var ds = await f.CreateDataStoreService().GetDataStoreAsync(Ws, PolyStore);
                Assert.Equal("Shapefile", ds!.Type);
                var ft = await f.CreateFeatureTypeService().GetFeatureTypeAsync(Ws, PolyStore, PolyLayer);
                Assert.Equal(PolyNative, ft!.NativeName);
                Assert.True(ft.Enabled, "向导发布 FT 应 enabled=true");

                // WFS 计数比对：期望 = DBF 头记录数（独立解析；生成器确定性 12 条）
                var expected = DbfRecordCount(TestEnv.AbsDataPath(PolyNative + ".dbf"));
                Assert.Equal(12, expected);
                Assert.Equal(expected, WfsHits(Ws + ":" + PolyLayer));

                // 幂等重入：已存在时复用不报错
                var r2 = await wiz.PublishShapefileAsync(req);
                Assert.True(r2.Success, r2.Message);
            }
            finally
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
            }
        }

        [Fact]
        public async Task GeoTiff_WizardPublish_Closed_Loop()
        {
            if (!RequireGeoServer()) return;
            Assert.True(TestEnv.GeneratedDataExists(), "缺少测试数据目录 " + TestEnv.GeneratedDataDir);
            using var f = Fx.Factory();
            var wiz = f.CreateImportWizardService();
            try
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
                await f.CreateWorkspaceService().CreateWorkspaceAsync(Ws);

                var req = new ImportSourceRequest
                {
                    Kind = ImportDataSourceKind.GeoTiffFile,
                    Workspace = Ws,
                    LayerName = DemLayer,
                    NativeName = DemNative,
                    StoreName = DemStore,
                    FileRef = "file:" + TestEnv.Prefix + "_data/" + DemNative + ".tif",
                    Srs = "EPSG:32754",
                };

                var r = await wiz.PublishGeoTiffAsync(req);
                Assert.True(r.Success, r.Message);

                var cs = await f.CreateCoverageStoreService().GetCoverageStoreAsync(Ws, DemStore);
                Assert.Equal("GeoTIFF", cs!.Type);

                // 覆盖度已发布且范围非零维（零维回归防护：GeoServerJson.Request null 省略修复锚点）。
                // 单体 GET 走裸 HTTP（独立通道；产品库路径的幂等性由下方第二次发布断言覆盖）。
                var json = RawGet(TestEnv.RestBase + "/rest/workspaces/" + Ws + "/coveragestores/"
                    + DemStore + "/coverages/" + DemLayer + ".json");
                var bb = Newtonsoft.Json.Linq.JObject.Parse(json)["coverage"]!["nativeBoundingBox"]!;
                Assert.Equal(500000.0, (double)bb["minx"]!, 3);
                Assert.Equal(508100.0, (double)bb["maxx"]!, 3);
                Assert.Equal(2195960.0, (double)bb["miny"]!, 3);
                Assert.Equal(2200060.0, (double)bb["maxy"]!, 3);

                // 幂等重入
                var r2 = await wiz.PublishGeoTiffAsync(req);
                Assert.True(r2.Success, r2.Message);
            }
            finally
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
            }
        }

        [Fact]
        public async Task Postgis_WizardProbe_And_Publish_With_Wfs_Count()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var wiz = f.CreateImportWizardService();
            try
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
                await f.CreateWorkspaceService().CreateWorkspaceAsync(Ws);

                var pg = new PostgisConnectionParameters
                {
                    Host = TestEnv.GeoServerVisiblePgHost,
                    Port = TestEnv.PgPort,
                    Database = TestEnv.PgDb,
                    User = TestEnv.PgUser,
                    Password = TestEnv.PgPass,
                };

                // 探测（经 GeoServer 试连）；PostGIS 不可用时按协议跳过
                var probe = await wiz.ProbePostgisConnectionAsync(Ws, pg);
                if (!probe.Success)
                {
                    SkipLog.Skip("PostGIS 不可用：" + probe.Message);
                    return;
                }

                var r = await wiz.PublishPostgisAsync(new ImportSourceRequest
                {
                    Kind = ImportDataSourceKind.Postgis,
                    Workspace = Ws,
                    LayerName = PgLayer,
                    NativeName = PolyNative,   // 表 gdtest_poly
                    StoreName = PgStore,
                    Srs = "EPSG:4326",
                    Postgis = pg,
                });
                Assert.True(r.Success, r.Message);

                // WFS 计数 = DBF 独立解析的期望（表由同一 shapefile 载入）
                var expected = DbfRecordCount(TestEnv.AbsDataPath(PolyNative + ".dbf"));
                Assert.Equal(expected, WfsHits(Ws + ":" + PgLayer));
            }
            finally
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
            }
        }

        // ---------- 独立通道辅助（不经被测库） ----------

        /// <summary>裸 HTTP GET（Basic 鉴权）；非 2xx 直接断言失败。</summary>
        private static string RawGet(string url)
        {
            using var c = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            c.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(TestEnv.User + ":" + TestEnv.Pass)));
            var resp = c.GetAsync(url).GetAwaiter().GetResult();
            var text = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.True(resp.IsSuccessStatusCode,
                "GET " + url + " → HTTP " + (int)resp.StatusCode + "：" + text.Substring(0, Math.Min(200, text.Length)));
            return text;
        }

        /// <summary>WFS 2.0 hits 计数（typeName 带工作空间限定）。</summary>
        private static int WfsHits(string qualifiedLayer)
        {
            var url = TestEnv.OgcUrl("wfs") + "?service=WFS&request=GetFeature&version=2.0.0"
                + "&resultType=hits&outputFormat=application/json&typeName=" + Uri.EscapeDataString(qualifiedLayer)
                + "&count=1";
            var text = RawGet(url);
            var m = System.Text.RegularExpressions.Regex.Match(text, "numberMatched[\"':=\\s]+(\\d+)");
            Assert.True(m.Success, "WFS hits 响应解析失败：" + text.Substring(0, Math.Min(200, text.Length)));
            return int.Parse(m.Groups[1].Value);
        }

        /// <summary>DBF 头记录数（offset 4，小端 int32）——独立于被测库的期望值推导。</summary>
        private static int DbfRecordCount(string dbfPath)
        {
            var head = new byte[8];
            using (var fs = System.IO.File.OpenRead(dbfPath))
            {
                fs.Read(head, 0, head.Length);
            }
            return BitConverter.ToInt32(head, 4);
        }
    }
}
