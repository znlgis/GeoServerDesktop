using System;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.GeoServerClient.Import;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// BatchOperationsViewModel 无头测试（M4）：多选列表加载（与裸 REST 交叉复核）→
    /// 全选切换 → 批量改默认样式（落库复核）→ 存储批量启停（VmRest.DataStoreEnabled 复核）→
    /// 工作空间列表态。连接守卫与部分失败语义在库层 L1/L2 覆盖。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class BatchOperationsViewModelTests : GeoServerTestBase
    {
        private const string Ws = "gdtest_vm_batch";
        private const string Ds = "vmb_ds";
        private const string Layer = "vm_bat_poly";

        public BatchOperationsViewModelTests(GeoServerFixture fx) : base(fx) { }

        private async Task PublishFixtureAsync()
        {
            var conn = VmTestKit.Connected();
            try
            {
                try { await conn.GetWorkspaceService().GetWorkspaceAsync(Ws); }
                catch { await conn.GetWorkspaceService().CreateWorkspaceAsync(Ws); }
                var r = await conn.GetImportWizardService().PublishShapefileAsync(new ImportSourceRequest
                {
                    Kind = ImportDataSourceKind.ShapefileDirectory,
                    Workspace = Ws,
                    StoreName = Ds,
                    LayerName = Layer,
                    NativeName = VmTestKit.PolyNativeName,
                    FileRef = VmTestKit.ShapefileUrl,
                    Srs = "EPSG:4326",
                });
                Assert.True(r.Success, r.Message);
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public async Task LoadItems_LayerScope_MatchesRest_And_SelectAllToggles()
        {
            if (!RequireGeoServer()) return;
            await PublishFixtureAsync();
            var conn = VmTestKit.Connected();
            var vm = new BatchOperationsViewModel(conn);
            try
            {
                await vm.LoadItemsCommand.ExecuteAsync(null);

                Assert.Contains(Ws, vm.Workspaces);
                Assert.Equal("Layer", vm.Scope);
                var mine = vm.Items.FirstOrDefault(i => i.Display == Ws + ":" + Layer);
                Assert.NotNull(mine);
                // 交叉复核：VM 条目集合与裸 REST 工作空间图层列表一致
                var rest = VmRest.Get("/rest/workspaces/" + Ws + "/layers.json") ?? "";
                Assert.Contains(Layer, rest);
                Assert.Single(vm.Items, i => i.Display.StartsWith(Ws + ":", StringComparison.Ordinal));

                // 全选切换
                vm.ToggleSelectAllCommand.Execute(null);
                Assert.All(vm.Items, i => Assert.True(i.IsSelected));
                vm.ToggleSelectAllCommand.Execute(null);
                Assert.All(vm.Items, i => Assert.False(i.IsSelected));

                // 未选中时批量改样式给出引导提示（不发请求）
                await vm.ApplyDefaultStyleCommand.ExecuteAsync(null);
                Assert.Equal(vm.L.BatchStatusNeedLayers, vm.StatusMessage);
            }
            finally
            {
                conn.Disconnect();
                VmTestKit.TryDeleteWorkspace(Ws);
            }
        }

        [Fact]
        public async Task BulkDefaultStyle_Applies_AndPersists()
        {
            if (!RequireGeoServer()) return;
            await PublishFixtureAsync();
            var conn = VmTestKit.Connected();
            var vm = new BatchOperationsViewModel(conn);
            try
            {
                await vm.LoadItemsCommand.ExecuteAsync(null);
                var item = vm.Items.Single(i => i.Display == Ws + ":" + Layer);
                item.IsSelected = true;

                vm.SelectedStyleName = "polygon";
                await vm.ApplyDefaultStyleCommand.ExecuteAsync(null);

                Assert.Equal(string.Format(vm.L.BatchStatusAllOk, 1), vm.StatusMessage);

                // 落库复核（裸 REST）
                var (name, _) = VmRest.LayerDefaultStyleRef(Ws, Layer);
                Assert.Equal("polygon", name);
            }
            finally
            {
                conn.Disconnect();
                VmTestKit.TryDeleteWorkspace(Ws);
            }
        }

        [Fact]
        public async Task StoreScope_BulkEnableDisable_PersistsViaRest()
        {
            if (!RequireGeoServer()) return;
            await PublishFixtureAsync();
            var conn = VmTestKit.Connected();
            var vm = new BatchOperationsViewModel(conn);
            try
            {
                vm.SelectedWorkspace = Ws;
                vm.Scope = "Store";
                await VmTestKit.Settle();
                await vm.LoadItemsCommand.ExecuteAsync(null);

                var store = vm.Items.Single(i => i.Display == Ws + ":" + Ds);
                store.IsSelected = true;

                await vm.DisableSelectedCommand.ExecuteAsync(null);
                Assert.Contains("1", vm.StatusMessage);
                Assert.Equal(false, VmRest.DataStoreEnabled(Ws, Ds));

                await vm.EnableSelectedCommand.ExecuteAsync(null);
                Assert.Equal(true, VmRest.DataStoreEnabled(Ws, Ds));
            }
            finally
            {
                conn.Disconnect();
                VmTestKit.TryDeleteWorkspace(Ws);
            }
        }

        [Fact]
        public async Task WorkspaceScope_ListsAll()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new BatchOperationsViewModel(conn);
            try
            {
                vm.Scope = "Workspace";
                await vm.LoadItemsCommand.ExecuteAsync(null);

                var expected = VmRest.WorkspaceCount() ?? 0;
                Assert.Equal(expected, vm.Items.Count);
                Assert.All(vm.Items, i => Assert.Equal(i.Name, i.Display));
            }
            finally { conn.Disconnect(); }
        }
    }
}
