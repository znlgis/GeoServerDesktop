using System;
using System.Linq;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// LayerGroupsManagementViewModel 集成测试。
    /// 固化现状：VM 的 CreateLayerGroup 只发空 publishables，GeoServer 3.0.1 直接 400 拒绝。
    /// 改用服务层先建合法全局图层组（含一个发布项）验证 VM 的“列表 → 选中 → 删除”往返。
    /// 集合仅在 Settle 后枚举，避免后台重载竞态；删除结果以裸 REST 为准。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class LayerGroupsManagementViewModelTests : GeoServerTestBase
    {
        private const string Ws = "gdtest_vm_ws_lg";
        private const string Store = "gdtest_vm_lgstore";
        private const string FtName = "gdtest_vm_lg_layer";
        private const string BadLg = "gdtest_vm_lg_bad";
        private const string GoodLg = "gdtest_vm_lg_good";

        public LayerGroupsManagementViewModelTests(GeoServerFixture fx) : base(fx) { }

        private static int LgStatus(string name) => VmRest.GetStatus("/rest/layergroups/" + Uri.EscapeDataString(name) + ".json");

        [Fact]
        public async Task EmptyPublishables_Fails_And_ServiceCreatedGroup_ListsAndDeletes()
        {
            if (!RequireGeoServer()) return;
            VmTestKit.TryDeleteLayerGroup(BadLg);
            VmTestKit.TryDeleteLayerGroup(GoodLg);
            VmTestKit.TryDeleteWorkspace(Ws);
            Assert.Equal(201, VmRest.PostJson("/rest/workspaces", "{\"workspace\":{\"name\":\"" + Ws + "\"}}"));
            var conn = VmTestKit.Connected();
            try
            {
                await conn.GetDataStoreService().CreateDataStoreAsync(Ws, new DataStore
                {
                    Name = Store,
                    Type = "Shapefile",
                    Enabled = true,
                    ConnectionParameters = new ConnectionParameters
                    {
                        Entries = new[] { new ConnectionParameterEntry { Key = "url", Value = VmTestKit.ShapefileUrl } }
                    }
                });
                await conn.GetFeatureTypeService().CreateFeatureTypeAsync(Ws, Store, new FeatureType
                {
                    Name = FtName,
                    NativeName = VmTestKit.PolyNativeName,
                    Srs = "EPSG:4326",
                    Enabled = true
                });

                var vm = new LayerGroupsManagementViewModel(conn);
                vm.SelectedWorkspace = "All Workspaces";
                await vm.LoadLayerGroupsCommand.ExecuteAsync(null);
                await VmTestKit.Settle();

                // 1) VM 创建（空发布项）→ GeoServer 拒绝
                vm.NewLayerGroupName = BadLg;
                await vm.CreateLayerGroupCommand.ExecuteAsync(null);
                var failPrefix = string.Format(vm.L.StatusLayerGroupCreateFailed, "");
                Assert.StartsWith(failPrefix, vm.StatusMessage);
                Assert.NotEqual(200, LgStatus(BadLg));

                // 2) 服务层建合法全局图层组（含一个发布项）
                await conn.GetLayerGroupService().CreateLayerGroupAsync(new LayerGroup
                {
                    Name = GoodLg,
                    Mode = "SINGLE",
                    Title = GoodLg,
                    Publishables = new PublishableList
                    {
                        Published = new[] { new PublishedItem { Type = "layer", Name = Ws + ":" + FtName } }
                    }
                });
                await vm.LoadLayerGroupsCommand.ExecuteAsync(null);
                await VmTestKit.Settle();
                Assert.Contains(GoodLg, vm.LayerGroups.Select(g => g.Name).ToList());

                // 3) 选中 → VM 删除 → 消失
                vm.SelectedLayerGroup = vm.LayerGroups.First(g => g.Name == GoodLg);
                await vm.DeleteLayerGroupCommand.ExecuteAsync(null);
                await VmTestKit.Settle();
                Assert.DoesNotContain(GoodLg, vm.LayerGroups.Select(g => g.Name).ToList());
                Assert.NotEqual(200, LgStatus(GoodLg));
                Assert.False(vm.IsLoading);
            }
            finally
            {
                VmTestKit.TryDeleteLayerGroup(BadLg);
                VmTestKit.TryDeleteLayerGroup(GoodLg);
                VmTestKit.TryDeleteWorkspace(Ws);
                conn.Disconnect();
            }
        }
    }
}
