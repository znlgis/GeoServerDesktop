using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Integration;
using Newtonsoft.Json;

namespace GeoServerDesktop.Tests.Infrastructure
{
    /// <summary>
    /// 端到端共享发布物的幂等保障器（Layer 2 集成测试与 Layer 3 端到端测试复用）。
    /// 资源命名固定：工作空间 gdtest_ws_e2e；shapefile 存储 gdtest_ds（featuretypes
    /// gdtest_poly / gdtest_lines）；栅格存储 gdtest_cs + coverage gdtest_dem；全局样式 gdtest_red；
    /// 图层与资源同名（GeoServer 发布 FeatureType/Coverage 时自动创建同名 layer）。
    /// 所有 Ensure* 幂等可重入：先 GET 探测存在（404 才创建），并发/重复调用安全（409 视为已存在），
    /// 全局 lock 串行化，绝不并行互踩。
    /// </summary>
    public static class E2ePublisher
    {
        public const string Ws = TestEnv.Prefix + "_ws_e2e";   // gdtest_ws_e2e
        public const string Ds = TestEnv.Prefix + "_ds";       // gdtest_ds
        public const string Poly = TestEnv.Prefix + "_poly";   // gdtest_poly
        public const string Lines = TestEnv.Prefix + "_lines"; // gdtest_lines
        public const string Cs = TestEnv.Prefix + "_cs";        // gdtest_cs
        public const string Cov = TestEnv.Prefix + "_dem";      // gdtest_dem
        public const string Style = TestEnv.Prefix + "_red";    // gdtest_red

        /// <summary>图层限定名（GWC/图层组 publishable 等场景使用）。</summary>
        public static string PolyLayerQualified => Ws + ":" + Poly;

        private static readonly object Gate = new object();

        /// <summary>确保工作空间 + shapefile 存储 + 两个要素类型（Enabled=true）齐备。幂等。</summary>
        public static void EnsureShapefileStore()
        {
            lock (Gate)
            {
                EnsureShapefileStoreAsync().GetAwaiter().GetResult();
            }
        }

        /// <summary>确保栅格存储 gdtest_cs + coverage gdtest_dem 齐备。幂等。</summary>
        public static void EnsureCoverage()
        {
            lock (Gate)
            {
                EnsureCoverageAsync().GetAwaiter().GetResult();
            }
        }

        /// <summary>确保全局样式 gdtest_red（纯红 SLD）存在。幂等。</summary>
        public static void EnsureStyle()
        {
            lock (Gate)
            {
                EnsureStyleAsync().GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// 确保指定工作空间的指定图层已发布可查。当前实现覆盖 e2e 全套（矢量两图层 + 栅格图层 + 样式）；
        /// 非 e2e 工作空间仅做存在性断言式检查（Layer 3 按需扩展）。
        /// </summary>
        public static void EnsurePublishedLayer(string workspace, string layerName)
        {
            lock (Gate)
            {
                EnsurePublishedLayerAsync(workspace, layerName).GetAwaiter().GetResult();
            }
        }

        // ---------- 异步实现 ----------

        private static GeoServerClientFactory NewFactory() => new GeoServerClientFactory(GeoServerAvailability.Options());

        /// <summary>GET 探测单体资源：true=存在，false=404，其他异常抛出（避免把故障当"不存在"）。用裸 HttpClient，独立于幂等写路径。</summary>
        private static async Task<bool> ExistsAsync(string path)
        {
            using (var probe = new HttpClient { Timeout = TimeSpan.FromSeconds(60) })
            {
                probe.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(Encoding.ASCII.GetBytes(TestEnv.User + ":" + TestEnv.Pass)));
                var resp = await probe.GetAsync(TestEnv.RestBase.TrimEnd('/') + path).ConfigureAwait(false);
                if (resp.IsSuccessStatusCode) return true;
                if ((int)resp.StatusCode == 404) return false;
                var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new GeoServerRequestException("E2ePublisher 探测失败: " + path, (int)resp.StatusCode, body);
            }
        }

        /// <summary>并发/幂等创建冲突：409 视为已存在；500 仅当报文含 "already exists"（GeoServer 个别端点冲突回 500）。</summary>
        private static bool AlreadyExists(Exception e) =>
            e is GeoServerRequestException gre &&
            (gre.StatusCode == 409 ||
             (gre.StatusCode == 500 && (gre.ResponseContent ?? "").Contains("already exists", StringComparison.OrdinalIgnoreCase)));

        private static async Task EnsureShapefileStoreAsync()
        {
            using var f = NewFactory();

            if (!await ExistsAsync("/rest/workspaces/" + Ws + ".json"))
            {
                try { await f.CreateWorkspaceService().CreateWorkspaceAsync(Ws); }
                catch (Exception e) when (AlreadyExists(e)) { }
            }

            if (!await ExistsAsync($"/rest/workspaces/{Ws}/datastores/{Ds}.json"))
            {
                // 实测要点：shapefile 存储用 entry @key="url"（值 file:gdtest_data，相对 data_dir），不是 directory
                var ds = new DataStore
                {
                    Name = Ds,
                    Type = "Shapefile", // 实测：请求体 type:null 会丢失服务器类型推断（GET 回 ""）
                    Enabled = true,
                    ConnectionParameters = new ConnectionParameters
                    {
                        Entries = new[]
                        {
                            new ConnectionParameterEntry { Key = "url", Value = "file:" + TestEnv.Prefix + "_data" },
                            new ConnectionParameterEntry { Key = "namespace", Value = "http://" + Ws },
                        }
                    }
                };
                try { await f.CreateDataStoreService().CreateDataStoreAsync(Ws, ds); }
                catch (Exception e) when (AlreadyExists(e)) { }
            }

            foreach (var ftName in new[] { Poly, Lines })
            {
                if (await ExistsAsync($"/rest/workspaces/{Ws}/datastores/{Ds}/featuretypes/{ftName}.json")) continue;
                // 实测要点：FeatureType.Enabled 默认 false 必须显式 true；且请求体里 srs 为 null 会
                // 触发 500 "the layer srs seems to be mis-configured"（curl 二分定位），必须显式给 Srs。
                var ft = new FeatureType
                {
                    Name = ftName,
                    NativeName = ftName,
                    Enabled = true,
                    Title = ftName,
                    Srs = "EPSG:4326"
                };
                try { await f.CreateFeatureTypeService().CreateFeatureTypeAsync(Ws, Ds, ft); }
                catch (Exception e) when (AlreadyExists(e)) { }
            }
        }

        private static async Task EnsureCoverageAsync()
        {
            using var f = NewFactory();

            if (!await ExistsAsync("/rest/workspaces/" + Ws + ".json"))
            {
                try { await f.CreateWorkspaceService().CreateWorkspaceAsync(Ws); }
                catch (Exception e) when (AlreadyExists(e)) { }
            }

            if (!await ExistsAsync($"/rest/workspaces/{Ws}/coveragestores/{Cs}.json"))
            {
                var cs = new CoverageStore
                {
                    Name = Cs,
                    Type = "GeoTIFF",
                    Enabled = true,
                    Url = "file:" + TestEnv.Prefix + "_data/" + Cov + ".tif",
                    Workspace = new WorkspaceReference { Name = Ws },
                };
                try { await f.CreateCoverageStoreService().CreateCoverageStoreAsync(Ws, cs); }
                catch (Exception e) when (AlreadyExists(e)) { }
            }

            if (!await ExistsAsync($"/rest/workspaces/{Ws}/coveragestores/{Cs}/coverages/{Cov}.json"))
            {
                var cov = new Coverage { Name = Cov, NativeName = Cov, Enabled = true, Title = Cov, Srs = "EPSG:32754" };
                try { await f.CreateCoverageService().CreateCoverageAsync(Ws, Cs, cov); }
                catch (Exception e) when (AlreadyExists(e)) { }
            }
        }

        private static async Task EnsureStyleAsync()
        {
            using var f = NewFactory();
            if (await ExistsAsync("/rest/styles/" + Style + ".json")) return;
            try { await f.CreateStyleService().CreateStyleAsync(Style, SldTemplates.Red); }
            catch (Exception e) when (AlreadyExists(e)) { }
        }

        private static async Task EnsurePublishedLayerAsync(string workspace, string layerName)
        {
            if (workspace != Ws)
                throw new NotSupportedException("E2ePublisher 目前只管理 " + Ws + " 下的共享发布物，请求: " + workspace);

            // 矢量图层：GDAL 生成的 shapefile 与 tif 都在 data_dir/gdtest_data 下
            if (layerName == Poly || layerName == Lines) await EnsureShapefileStoreAsync();
            else if (layerName == Cov) await EnsureCoverageAsync();
            else throw new NotSupportedException("未知共享图层: " + workspace + ":" + layerName);

            // 图层由发布资源时自动创建；GET 校验兜底
            if (!await ExistsAsync("/rest/layers/" + layerName + ".json"))
                throw new InvalidOperationException("发布后图层未自动创建: " + layerName);
        }
    }
}
