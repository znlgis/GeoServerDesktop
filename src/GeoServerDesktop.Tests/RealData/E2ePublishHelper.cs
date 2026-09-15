using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient;
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.RealData
{
    /// <summary>
    /// Layer 3 真实数据端到端发布器（库路径版，Internal）：用 GeoServerClient 服务把 gdtest_* 数据集
    /// 发布到固定工作空间，供服务面（WFS/WMS/WCS/WMTS/GWC）独立校验。幂等：已存在的资源跳过创建。
    /// 固定资源名与 Layer 2 的 E2ePublisher 一致（gdtest_ws_e2e / gdtest_ds / gdtest_poly …），
    /// 因此二者任一侧先发布均可复用，不会产生重复；本类不引用 E2ePublisher 以避免并行编译耦合。
    /// 发布走被测库；服务面读取一律用裸 HttpClient（见 RealDataChecks/OgcProbe），保证期望值独立回抄。
    /// </summary>
    public static class E2ePublishHelper
    {
        public const string Ws = "gdtest_ws_e2e";
        public const string PolyStore = "gdtest_ds";
        public const string PolyLayer = "gdtest_poly";
        public const string LinesLayer = "gdtest_lines";
        public const string PgStore = "gdtest_pg_e2e";
        public const string PgPolyLayer = "gdtest_poly_pg";      // 同工作空间内避免与 shapefile gdtest_poly 图层重名
        public const string PgNative = "gdtest_poly";           // PostGIS 表名
        public const string CovStore = "gdtest_cs";
        public const string DemLayer = "gdtest_dem";
        public const string RedStyle = "gdtest_red_e2e";         // 纯色红 SLD（WMS 像素真值用）

        // 数据目录相对容器 data_dir 的引用（file: 前缀 = data_dir 相对路径）
        private const string DataDirRef = "file:gdtest_data";
        private const string DemFileRef = "file:gdtest_data/gdtest_dem.tif";

        /// <summary>纯色不透明红 SLD（经典 sld/ogc 命名空间，本 GeoServer 1.1.1 WMS 可渲染）。</summary>
        public static string RedSld =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<StyledLayerDescriptor version=\"1.0.0\" xmlns=\"http://www.opengis.net/sld\" xmlns:ogc=\"http://www.opengis.net/ogc\">" +
            "<NamedLayer><Name>" + RedStyle + "</Name><UserStyle><FeatureTypeStyle><Rule>" +
            "<PolygonSymbolizer><Fill><CssParameter name=\"fill\">#FF0000</CssParameter>" +
            "<CssParameter name=\"fill-opacity\">1</CssParameter></Fill>" +
            "<Stroke><CssParameter name=\"stroke\">#FF0000</CssParameter></Stroke>" +
            "</PolygonSymbolizer></Rule></FeatureTypeStyle></UserStyle></NamedLayer></StyledLayerDescriptor>";

        /// <summary>用被测库幂等发布全部 fixture；返回实际使用的工厂供后续（若需要）。</summary>
        public static void EnsurePublished(GeoServerFixture fx)
        {
            using var factory = fx.Factory();
            EnsureWorkspace(factory);
            EnsureNamespace(factory);
            EnsureShapefileLayer(factory, PolyStore, PolyLayer);
            EnsureShapefileLayer(factory, PolyStore, LinesLayer);
            EnsureCoverage(factory);
            EnsurePostgisLayer(factory);
            EnsureRedStyle(factory);
        }

        private static void EnsureWorkspace(GeoServerClientFactory f)
        {
            var ws = f.CreateWorkspaceService();
            if (Exists(() => ws.GetWorkspaceAsync(Ws))) return;
            CreateOrVerify(() => ws.CreateWorkspaceAsync(Ws), () => Exists(() => ws.GetWorkspaceAsync(Ws)), "workspace " + Ws);
        }

        /// <summary>
        /// 幂等确保命名空间存在。工作空间被中断/半创建时可能缺同名命名空间，导致 WFS 报
        /// “Unknown namespace”，此处自愈（已存在则 500 被吞）。
        /// </summary>
        private static void EnsureNamespace(GeoServerClientFactory f)
        {
            var ns = f.CreateNamespaceService();
            if (Exists(() => ns.GetNamespaceAsync(Ws))) return;
            CreateOrVerify(() => ns.CreateNamespaceAsync(Ws, "http://" + Ws + "/"), () => Exists(() => ns.GetNamespaceAsync(Ws)), "namespace " + Ws);
        }

        private static void EnsureShapefileLayer(GeoServerClientFactory f, string store, string layer)
        {
            var dsSvc = f.CreateDataStoreService();
            if (!Exists(() => dsSvc.GetDataStoreAsync(Ws, store)))
            {
                CreateOrVerify(() => dsSvc.CreateDataStoreAsync(Ws, new DataStore
                {
                    Name = store,
                    Type = "Shapefile",
                    Enabled = true,
                    ConnectionParameters = new ConnectionParameters
                    {
                        Entries = new[] { new ConnectionParameterEntry { Key = "url", Value = DataDirRef } }
                    }
                }), () => Exists(() => dsSvc.GetDataStoreAsync(Ws, store)), "shapefile store " + store);
            }
            var ftSvc = f.CreateFeatureTypeService();
            if (Exists(() => ftSvc.GetFeatureTypeAsync(Ws, store, layer))) return;
            CreateOrVerify(() => ftSvc.CreateFeatureTypeAsync(Ws, store, new FeatureType
            {
                Name = layer,
                NativeName = layer,                 // shapefile 基名（gdtest_poly/gdtest_lines）
                Srs = "EPSG:4326",
                Enabled = true,                     // 非空 bool：必须显式 true，否则发布即禁用
                Namespace = new NamespaceReference { Name = Ws },
                NativeBoundingBox = new BoundingBox { MinX = 0, MaxX = 11, MinY = 0, MaxY = 6, Crs = "EPSG:4326" },
                LatLonBoundingBox = new BoundingBox { MinX = 0, MaxX = 11, MinY = 0, MaxY = 6, Crs = "EPSG:4326" },
            }), () => Exists(() => ftSvc.GetFeatureTypeAsync(Ws, store, layer)), "shapefile FT " + store + "/" + layer);
        }

        private static void EnsureCoverage(GeoServerClientFactory f)
        {
            var csSvc = f.CreateCoverageStoreService();
            if (!Exists(() => csSvc.GetCoverageStoreAsync(Ws, CovStore)))
            {
                CreateOrVerify(() => csSvc.CreateCoverageStoreAsync(Ws, new CoverageStore
                {
                    Name = CovStore,
                    Type = "GeoTIFF",
                    Enabled = true,
                    Workspace = new WorkspaceReference { Name = Ws },
                    Url = DemFileRef,
                }), () => Exists(() => csSvc.GetCoverageStoreAsync(Ws, CovStore)), "coverage store " + CovStore);
            }

            // 覆盖度（coverage）发布走裸 REST：产品 CreateCoverageAsync 会序列化 nativeBoundingBox/nativeCRS
            // 等 null 字段（Newtonsoft 默认 Include），GeoServer 收到 "nativeBoundingBox":null 时【不】从数据重算
            // 范围，产出零维 (0,0,0,0) 覆盖度 → WCS 只能返回 1x1（新发现产品缺陷，已记入报告）。裸 REST 最小体
            // 省略这些字段即令 GeoServer 正确计算 EPSG:32754 全幅 81x41。逐项自愈：存在但范围塌缩亦重建。
            var covUrl = TestEnv.RestBase + "/rest/workspaces/" + Ws + "/coveragestores/" + CovStore
                         + "/coverages/" + DemLayer;
            if (!CoverageHealthy(covUrl + ".json"))
            {
                // 先尽力删掉可能存在的损坏/残留覆盖度（含其自动图层），再按最小体重建。
                DeleteCoverage(covUrl);
                var created = OgcProbe.Post(
                    TestEnv.RestBase + "/rest/workspaces/" + Ws + "/coveragestores/" + CovStore + "/coverages",
                    "{\"coverage\":{\"name\":\"" + DemLayer + "\",\"nativeName\":\"" + DemLayer +
                    "\",\"srs\":\"EPSG:32754\",\"enabled\":true,\"title\":\"" + DemLayer + "\"}}",
                    "application/json");
                if (!created.Ok && created.Status != 409)
                    throw new InvalidOperationException("覆盖度裸发布失败 HTTP " + created.Status);
            }
        }

        /// <summary>覆盖度是否“健康”：存在且 nativeBoundingBox 非零（minx!=maxx）。</summary>
        private static bool CoverageHealthy(string metaUrl)
        {
            var r = OgcProbe.Get(metaUrl);
            if (!r.Ok) return false;
            try
            {
                var j = Newtonsoft.Json.Linq.JObject.Parse(r.Text);
                var bb = j["coverage"]?["nativeBoundingBox"];
                if (bb == null) return false;
                double minx = (double?)bb["minx"] ?? 0, maxx = (double?)bb["maxx"] ?? 0;
                double miny = (double?)bb["miny"] ?? 0, maxy = (double?)bb["maxy"] ?? 0;
                return !(Math.Abs(maxx - minx) < 1e-9 && Math.Abs(maxy - miny) < 1e-9);
            }
            catch { return false; }
        }

        private static void DeleteCoverage(string covUrl)
        {
            try { HttpDelete(covUrl + "?recurse=true"); } catch { }
        }

        private static void HttpDelete(string url)
        {
            using var c = RawHttp();
            c.DeleteAsync(url).GetAwaiter().GetResult();
        }

        private static void EnsurePostgisLayer(GeoServerClientFactory f)
        {
            var dsSvc = f.CreateDataStoreService();
            if (!Exists(() => dsSvc.GetDataStoreAsync(Ws, PgStore)))
            {
                CreateOrVerify(() => dsSvc.CreateDataStoreAsync(Ws, new DataStore
                {
                    Name = PgStore,
                    Type = "PostGIS",
                    Enabled = true,
                    ConnectionParameters = new ConnectionParameters
                    {
                        Entries = new[]
                        {
                            new ConnectionParameterEntry { Key = "dbtype", Value = "postgis" },
                            new ConnectionParameterEntry { Key = "host", Value = PgGeoserverHost() },
                            new ConnectionParameterEntry { Key = "port", Value = TestEnv.PgPort.ToString() },
                            new ConnectionParameterEntry { Key = "database", Value = TestEnv.PgDb },
                            new ConnectionParameterEntry { Key = "user", Value = TestEnv.PgUser },
                            new ConnectionParameterEntry { Key = "passwd", Value = TestEnv.PgPass },
                            new ConnectionParameterEntry { Key = "Expose primary keys", Value = "true" },
                        }
                    }
                }), () => Exists(() => dsSvc.GetDataStoreAsync(Ws, PgStore)), "postgis store " + PgStore);
            }
            var ftSvc = f.CreateFeatureTypeService();
            if (Exists(() => ftSvc.GetFeatureTypeAsync(Ws, PgStore, PgPolyLayer))) return;
            CreateOrVerify(() => ftSvc.CreateFeatureTypeAsync(Ws, PgStore, new FeatureType
            {
                Name = PgPolyLayer,                 // 发布名（避免与 shapefile 同名）
                NativeName = PgNative,              // PostGIS 表名 gdtest_poly
                Srs = "EPSG:4326",
                Enabled = true,
                Namespace = new NamespaceReference { Name = Ws },
                NativeBoundingBox = new BoundingBox { MinX = 0, MaxX = 11, MinY = 0, MaxY = 6, Crs = "EPSG:4326" },
                LatLonBoundingBox = new BoundingBox { MinX = 0, MaxX = 11, MinY = 0, MaxY = 6, Crs = "EPSG:4326" },
            }), () => Exists(() => ftSvc.GetFeatureTypeAsync(Ws, PgStore, PgPolyLayer)), "postgis FT " + PgStore + "/" + PgPolyLayer);
        }

        private static void EnsureRedStyle(GeoServerClientFactory f)
        {
            // 红填充 SLD 属像素真值配套（非发布链路被测点），任务允许直接用 REST 建：两步且幂等。
            var sldUrl = TestEnv.RestBase + "/rest/styles/" + RedStyle;
            var exist = OgcProbe.Get(sldUrl + ".json");
            if (!exist.Ok)
                OgcProbe.Post(TestEnv.RestBase + "/rest/styles",
                    "{\"style\":{\"name\":\"" + RedStyle + "\",\"filename\":\"" + RedStyle + ".sld\"}}", "application/json");
            OgcProbe.Put(sldUrl, RedSld, "application/vnd.ogc.sld+xml");
        }

        /// <summary>
        /// GeoServer 容器可见的 PostGIS 主机名：容器内 127.0.0.1 指向 GeoServer 自己，绝不可用；
        /// 实测本部署（geoserver 与 postgis 同在默认 bridge 网、由 Docker 内嵌 DNS 提供名字解析）
        /// getent hosts postgis → 172.17.0.2，host=postgis 直连 5432 成功，故默认取容器名 postgis。
        /// 若目标环境两者不同网（名字解析失败），用 GSD_TEST_PG_GEOSERVER_HOST 覆盖为宿主机可达地址
        /// （如网关 172.17.0.1 或 host.docker.internal）。
        /// </summary>
        public static string PgGeoserverHost() => TestEnv.Env("GSD_TEST_PG_GEOSERVER_HOST", "postgis");

        // ---- 幂等辅助：存在性探测 + 创建后复核 ----
        private static bool Exists(Func<Task> probe)
        {
            try { probe().GetAwaiter().GetResult(); return true; }
            catch (GeoServerRequestException ex) { return ex.StatusCode is >= 200 and < 300; }
            catch { return false; }
        }

        /// <summary>
        /// 创建并复核：吞"已存在/竞态"类异常，但创建后资源仍不存在则【抛出】——
        /// 杜绝旧版 TryCreate 把"pg store 在而 FT 缺"之类半成功静默吞掉、导致下游服务面测试盲失败。
        /// </summary>
        private static void CreateOrVerify(Func<Task> act, Func<bool> nowExists, string what)
        {
            Exception err = null;
            try { act().GetAwaiter().GetResult(); }
            catch (Exception e) { err = e; }
            if (nowExists()) return;
            throw new InvalidOperationException("fixture 创建失败: " + what
                + (err == null ? "（POST 未抛但资源不存在）" : " :: " + err.Message));
        }

        /// <summary>删除本层发布的全部资源（工作空间级联 + 样式）。best-effort。</summary>
        public static void RemoveAll()
        {
            using var f = new GeoServerClientFactory(GeoServerAvailability.Options());
            try { f.CreateWorkspaceService().DeleteWorkspaceAsync(Ws, true).GetAwaiter().GetResult(); } catch { }
            try { f.CreateStyleService().DeleteStyleAsync(RedStyle, true).GetAwaiter().GetResult(); } catch { }
        }

        /// <summary>gdtest_ws_e2e 是否已发布（供 Layer2/本层二选一探测复用）。</summary>
        public static bool WorkspaceAlreadyPublished()
        {
            try
            {
                using var c = RawHttp();
                var r = c.GetAsync(TestEnv.RestBase + "/rest/workspaces/" + Ws + ".json").GetAwaiter().GetResult();
                return r.StatusCode == HttpStatusCode.OK;
            }
            catch { return false; }
        }

        public static HttpClient RawHttp()
        {
            var c = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes(TestEnv.User + ":" + TestEnv.Pass)));
            return c;
        }
    }
}
