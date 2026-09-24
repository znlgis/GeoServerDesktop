using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Headless;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// 欢迎页仪表盘数据源级联链路（真实 GeoServer 3.0.1）：
    /// 仪表盘速览所依赖的三个服务调用——版本（AboutService）、全部工作空间与全部图层——
    /// 发布级联（ws → store → ft）后计数增量正确，且均与裸 REST 独立解析交叉校验。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class DashboardIT : GeoServerTestBase
    {
        public DashboardIT(GeoServerFixture fx) : base(fx) { }

        private const string Ws = TestEnv.Prefix + "_ws_dash";
        private const string DsName = TestEnv.Prefix + "_ds_dash1";
        private const string FtName = TestEnv.Prefix + "_dash_poly";
        private const string FtNative = TestEnv.Prefix + "_poly";

        [Fact]
        public async Task OverviewDataSources_CascadeAndMatchBareRest()
        {
            if (!RequireGeoServer()) return;
            Assert.True(TestEnv.GeneratedDataExists(), "缺少测试数据目录 " + TestEnv.GeneratedDataDir);
            using var f = Fx.Factory();
            var wsSvc = f.CreateWorkspaceService();
            var dsSvc = f.CreateDataStoreService();
            var ftSvc = f.CreateFeatureTypeService();
            var layerSvc = f.CreateLayerService();
            var aboutSvc = f.CreateAboutService();
            try
            {
                // 基线：仪表盘两个计数数据源（全部工作空间 / 全部图层）
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
                int wsBefore = (await wsSvc.GetWorkspacesAsync()).Length;
                int layersBefore = (await layerSvc.GetLayersAsync()).Length;

                // 发布级联：ws → shapefile store → ft（发布自动派生同名图层）
                await wsSvc.CreateWorkspaceAsync(Ws);
                Fx.Cleanup.TrackWorkspace(Ws);
                await dsSvc.CreateDataStoreAsync(Ws, NewShapefileStore());
                await ftSvc.CreateFeatureTypeAsync(Ws, DsName,
                    new FeatureType { Name = FtName, NativeName = FtNative, Enabled = true, Title = FtName, Srs = "EPSG:4326" });

                // 工作空间计数：+1，且与裸 REST 独立统计一致
                var workspaces = await wsSvc.GetWorkspacesAsync();
                Assert.Equal(wsBefore + 1, workspaces.Length);
                Assert.Equal(VmRest.WorkspaceCount(), workspaces.Length);

                // 图层计数：+1（发布派生；全量图层列表中的名称为 ws:layer 限定形式），且与裸 REST 独立统计一致
                var layerFullName = Ws + ":" + FtName;
                var layers = await WaitForLayerAsync(layerSvc, layerFullName);
                Assert.Contains(layers, l => l.Name == layerFullName);
                Assert.Equal(layersBefore + 1, layers.Length);
                Assert.Equal(VmRest.LayerCount(), layers.Length);

                // 版本数据源：与裸 REST 独立解析一致
                var v = await aboutSvc.GetVersionAsync();
                var gs = Assert.Single(v!.About!.Resources, r => r.Name == "GeoServer");
                Assert.Equal(VmRest.GeoServerVersion(VmRest.Get("/rest/about/version.json")), gs.Version);
            }
            finally
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
            }
        }

        /// <summary>轮询等待发布派生的限定名图层（ws:layer）出现在全量图层列表中（容忍发布传播延迟）。</summary>
        private static async Task<Layer[]> WaitForLayerAsync(LayerService svc, string layerName)
        {
            var sw = Stopwatch.StartNew();
            Layer[] last = Array.Empty<Layer>();
            while (sw.ElapsedMilliseconds < 15000)
            {
                last = await svc.GetLayersAsync();
                if (last.Any(l => l.Name == layerName)) return last;
                await Task.Delay(150);
            }
            return last;
        }

        private static DataStore NewShapefileStore() => new DataStore
        {
            Name = DsName,
            Type = "Shapefile",
            Enabled = true,
            Workspace = new WorkspaceReference { Name = Ws, Href = "" },
            Href = "",
            ConnectionParameters = new ConnectionParameters
            {
                Entries = new[]
                {
                    new ConnectionParameterEntry { Key = "url", Value = "file:" + TestEnv.Prefix + "_data" },
                    new ConnectionParameterEntry { Key = "namespace", Value = "http://" + Ws },
                }
            }
        };
    }
}
