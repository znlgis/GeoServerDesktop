using System;
using System.Linq;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// LayersManagementViewModel 集成测试：先经服务层建好 shapefile 存储，再经 VM 发布图层
    /// （FT 发布会自动派生同名 Layer），删除后仅删 Layer。并断言 "All Workspaces" 哨兵走全量加载路径。
    /// 存在性以裸 REST 为唯一事实来源，视图模型集合仅在 Settle 后取快照枚举（规避后台重载竞态）。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class LayersManagementViewModelTests : GeoServerTestBase
    {
        private const string Ws = "gdtest_vm_ws_layer";
        private const string Store = "gdtest_vm_store";
        private const string LayerName = "gdtest_vm_lyr";

        public LayersManagementViewModelTests(GeoServerFixture fx) : base(fx) { }

        private static DataStore ShapefileStore(string name) => new DataStore
        {
            Name = name,
            Type = "Shapefile",
            Enabled = true,
            ConnectionParameters = new ConnectionParameters
            {
                Entries = new[] { new ConnectionParameterEntry { Key = "url", Value = VmTestKit.ShapefileUrl } }
            }
        };

        [Fact]
        public async Task PublishViaDialog_And_Sentinel_Reload_And_Delete()
        {
            if (!RequireGeoServer()) return;
            VmTestKit.TryDeleteWorkspace(Ws); // 前置清理，避免残留
            Assert.Equal(201, VmRest.PostJson("/rest/workspaces", "{\"workspace\":{\"name\":\"" + Ws + "\"}}"));
            var conn = VmTestKit.Connected();
            try
            {
                await conn.GetDataStoreService().CreateDataStoreAsync(Ws, ShapefileStore(Store));

                var vm = new LayersManagementViewModel(conn);
                await vm.LoadWorkspacesCommand.ExecuteAsync(null);
                await VmTestKit.Settle();
                Assert.Equal("All Workspaces", vm.Workspaces[0]); // 哨兵在首项
                Assert.Contains(Ws, vm.Workspaces.ToList());

                // 选中具体工作空间 → 发布对话框加载存储列表
                vm.SelectedWorkspace = Ws;
                await vm.LoadLayersCommand.ExecuteAsync(null);
                await VmTestKit.Settle();
                await vm.ShowCreateLayerDialogCommand.ExecuteAsync(null);
                Assert.True(vm.IsCreateLayerDialogVisible);
                Assert.Contains(Store, vm.DataStoreNames.ToList());

                // 发布新图层（nativeName 指向已备好的 gdtest_poly → 自动派生 Layer）
                vm.NewLayerName = LayerName;
                vm.NewNativeName = VmTestKit.PolyNativeName;
                vm.NewLayerSrs = "EPSG:4326";
                vm.NewLayerDataStore = Store;
                await vm.CreateLayerCommand.ExecuteAsync(null);
                Assert.Equal(vm.L.StatusLayerPublished, vm.StatusMessage);
                await VmTestKit.Settle();
                Assert.True(VmRest.LayerExists(Ws, LayerName), "GD 发布 FT 应自动派生 Layer");

                // 哨兵 "All Workspaces"：走全量 GetLayersAsync 分支（不断言具体项，规避后台重载与集合枚举竞态）
                vm.SelectedWorkspace = "All Workspaces";
                await vm.LoadLayersCommand.ExecuteAsync(null);
                await VmTestKit.Settle();
                Assert.False(vm.IsLoading);
                Assert.Equal(string.Format(vm.L.StatusLayersLoaded, vm.Layers.Count), vm.StatusMessage);
                Assert.NotEmpty(vm.Layers); // 全量列表应非空

                // 删除图层：以合成 Layer 目标（仅用其 Name），避免与后台重载集合的枚举竞态；仅删 Layer，FT 保留
                vm.SelectedLayer = new Layer { Name = LayerName };
                await vm.DeleteLayerCommand.ExecuteAsync(null);
                await VmTestKit.Settle();
                Assert.False(VmRest.LayerExists(Ws, LayerName), "GD 删除后 Layer 不应存在");
                Assert.True(VmRest.FeatureTypeExists(Ws, Store, LayerName), "GD recurse=false 只删 Layer 不删 FT");
                Assert.False(vm.IsLoading);
            }
            finally
            {
                VmTestKit.TryDeleteWorkspace(Ws);
                conn.Disconnect();
            }
        }

        [Fact]
        public async Task ShowCreateLayerDialog_OnAllWorkspaces_IsRejected()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new LayersManagementViewModel(conn);
            try
            {
                vm.SelectedWorkspace = "All Workspaces";
                await vm.ShowCreateLayerDialogCommand.ExecuteAsync(null);
                Assert.False(vm.IsCreateLayerDialogVisible);
                Assert.Equal(vm.L.StatusPleaseSelectSpecificWorkspace, vm.StatusMessage);
            }
            finally { conn.Disconnect(); }
        }
    }
}
