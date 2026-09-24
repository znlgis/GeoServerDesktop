using System;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Headless;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// 资源树浏览数据源级联链路（真实 GeoServer 3.0.1）：
    /// 工作空间 → 数据存储 → 要素类型逐级加载（与资源树延迟加载同源的服务），
    /// 每级均与裸 REST 响应交叉校验（VmRest，独立解析）。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class ResourceTreeIT : GeoServerTestBase
    {
        public ResourceTreeIT(GeoServerFixture fx) : base(fx) { }

        private const string Ws = TestEnv.Prefix + "_ws_rt";
        private const string DsName = TestEnv.Prefix + "_ds_rt1";
        private const string FtName = TestEnv.Prefix + "_rt_poly";
        private const string FtNative = TestEnv.Prefix + "_poly";

        [Fact]
        public async Task Cascade_Workspace_Store_Layer_MatchesBareRest()
        {
            if (!RequireGeoServer()) return;
            Assert.True(TestEnv.GeneratedDataExists(), "缺少测试数据目录 " + TestEnv.GeneratedDataDir);
            using var f = Fx.Factory();
            var wsSvc = f.CreateWorkspaceService();
            var dsSvc = f.CreateDataStoreService();
            var ftSvc = f.CreateFeatureTypeService();
            try
            {
                // --- 准备：ws + shapefile store + ft（资源树三级延迟加载的完整层级） ---
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
                await wsSvc.CreateWorkspaceAsync(Ws);
                Fx.Cleanup.TrackWorkspace(Ws);
                await dsSvc.CreateDataStoreAsync(Ws, NewShapefileStore());
                await ftSvc.CreateFeatureTypeAsync(Ws, DsName,
                    new FeatureType { Name = FtName, NativeName = FtNative, Enabled = true, Title = FtName, Srs = "EPSG:4326" });

                // --- 一级：工作空间列表（资源树根的子级数据源） ---
                var workspaces = await wsSvc.GetWorkspacesAsync();
                Assert.Contains(workspaces, w => w.Name == Ws);

                // --- 二级：数据存储列表（选中工作空间节点时的延迟加载） ---
                var stores = await dsSvc.GetDataStoresAsync(Ws);
                Assert.Contains(stores, s => s.Name == DsName);
                var rawStores = VmRest.Get("/rest/workspaces/" + Ws + "/datastores.json");
                Assert.NotNull(rawStores);
                Assert.Contains(DsName, rawStores!); // 交叉校验：裸 REST

                // --- 三级：要素类型列表（选中数据存储节点时的延迟加载） ---
                var fts = await ftSvc.GetFeatureTypesAsync(Ws, DsName);
                Assert.Contains(fts, t => t.Name == FtName);
                var rawFts = VmRest.Get("/rest/workspaces/" + Ws + "/datastores/" + DsName + "/featuretypes.json");
                Assert.NotNull(rawFts);
                Assert.Contains(FtName, rawFts!); // 交叉校验：裸 REST
            }
            finally
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
            }
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
