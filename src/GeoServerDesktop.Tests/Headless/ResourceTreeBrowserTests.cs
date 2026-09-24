using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.App.Models;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// 资源树浏览（MainWindowViewModel 资源树部分）无头测试：
    /// 离线守卫 + 连真 GeoServer 的选中联动延迟加载/展开/幂等/裸 REST 交叉校验。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class ResourceTreeBrowserTests : GeoServerTestBase
    {
        private const string Ws = "gdtest_vm_rt";
        private const string DsName = "gdtest_vm_rt_ds1";
        private const string FtName = "gdtest_vm_rt_poly";

        public ResourceTreeBrowserTests(GeoServerFixture fx) : base(fx) { }

        // ---------- 离线（不依赖服务器） ----------

        [Fact]
        public async Task SelectedNode_NotConnected_NoExpandNoLoad()
        {
            var vm = new MainWindowViewModel();
            var node = new ResourceTreeNode { Name = "ws", Type = ResourceType.Workspace };

            vm.SelectedNode = node;
            await Task.Delay(200); // 观察窗口：若守卫失效，即发即忘的加载会被捕获

            Assert.False(node.IsExpanded);
            Assert.False(node.IsLoaded);
        }

        // ---------- 集成（连真 GeoServer） ----------

        [Fact]
        public async Task CascadeLoad_SelectedNodes_MatchBareRest()
        {
            if (!RequireGeoServer()) return;
            Assert.True(TestEnv.GeneratedDataExists(), "缺少测试数据目录 " + TestEnv.GeneratedDataDir);

            await PrepareLevelsAsync();
            var vm = new MainWindowViewModel { BaseUrl = TestEnv.BaseUrl, Username = TestEnv.User, Password = TestEnv.Pass };
            try
            {
                await vm.ConnectCommand.ExecuteAsync(null);
                Assert.True(vm.IsConnected);

                // 树结构：GeoServer > Workspaces > ws，数据存储容器为未加载占位
                var root = vm.ResourceTree.Single();
                var wsContainer = root.Children.Single(c => c.Type == ResourceType.WorkspacesContainer);
                var wsNode = wsContainer.Children.Single(c => c.Name == Ws);
                var dsContainer = wsNode.Children.Single(c => c.Type == ResourceType.DataStoresContainer);
                Assert.False(dsContainer.IsLoaded);

                // --- 选中工作空间 → 即发即忘加载数据存储 ---
                vm.SelectedNode = wsNode;
                Assert.True(await WaitUntilAsync(() => dsContainer.IsLoaded && dsContainer.IsExpanded),
                    $"等待数据存储加载超时；StatusMessage={vm.StatusMessage}");

                var dsNode = dsContainer.Children.Single(c => c.Name == DsName);
                Assert.True(wsNode.IsExpanded, "选中工作空间后应展开");
                var rawStores = VmRest.Get("/rest/workspaces/" + Ws + "/datastores.json");
                Assert.NotNull(rawStores);
                Assert.Contains(DsName, rawStores!); // 交叉校验：裸 REST

                // --- 幂等：重复加载不重复添加 ---
                await vm.LoadDataStoresAsync(wsNode);
                Assert.Single(dsContainer.Children);

                // --- 选中数据存储 → 即发即忘加载图层 ---
                var layersContainer = dsNode.Children.Single(c => c.Type == ResourceType.LayersContainer);
                vm.SelectedNode = dsNode;
                Assert.True(await WaitUntilAsync(() => layersContainer.IsLoaded && layersContainer.IsExpanded),
                    $"等待图层加载超时；StatusMessage={vm.StatusMessage}");

                var layerNode = layersContainer.Children.Single(c => c.Name == FtName);
                Assert.Equal(ResourceType.Layer, layerNode.Type);
                Assert.True(dsNode.IsExpanded, "选中数据存储后应展开");
                var rawFts = VmRest.Get("/rest/workspaces/" + Ws + "/datastores/" + DsName + "/featuretypes.json");
                Assert.NotNull(rawFts);
                Assert.Contains(FtName, rawFts!); // 交叉校验：裸 REST

                // --- 幂等（图层级） ---
                await vm.LoadLayersAsync(dsNode);
                Assert.Single(layersContainer.Children);

                // --- 选中图层 → 预览联动（纯本地 URL 拼接，状态消息含 ws:layer） ---
                vm.SelectedNode = layerNode;
                Assert.True(await WaitUntilAsync(() => vm.StatusMessage.Contains(FtName)),
                    $"等待图层预览联动超时；StatusMessage={vm.StatusMessage}");
                Assert.Contains(FtName, vm.MapPreviewViewModel.PreviewUrl ?? string.Empty);
            }
            finally
            {
                vm.DisconnectCommand.Execute(null);
                VmTestKit.TryDeleteWorkspace(Ws);
            }
        }

        /// <summary>准备三级层级：ws + shapefile 数据存储 + 要素类型（发布自动派生同名 Layer）。</summary>
        private async Task PrepareLevelsAsync()
        {
            VmTestKit.TryDeleteWorkspace(Ws);
            using var f = Fx.Factory();
            var wsSvc = f.CreateWorkspaceService();
            await wsSvc.CreateWorkspaceAsync(Ws);
            Fx.Cleanup.TrackWorkspace(Ws);
            await f.CreateDataStoreService().CreateDataStoreAsync(Ws, NewShapefileStore());
            await f.CreateFeatureTypeService().CreateFeatureTypeAsync(Ws, DsName,
                new FeatureType { Name = FtName, NativeName = VmTestKit.PolyNativeName, Enabled = true, Title = FtName, Srs = "EPSG:4326" });
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
                    new ConnectionParameterEntry { Key = "url", Value = VmTestKit.ShapefileUrl },
                    new ConnectionParameterEntry { Key = "namespace", Value = "http://" + Ws },
                }
            }
        };

        private static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs = 15000)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (condition()) return true;
                await Task.Delay(100);
            }
            return condition();
        }
    }
}
