using System;
using System.Linq;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// StoresManagementViewModel 集成测试。FIXED-E40 翻转：VM 创建 shapefile 存储的载荷现含
    /// type=Shapefile + namespace/url 连接参数（约定 url="file:"+存储名，目录相对 data_dir 与存储同名）
    /// 及 description；独立裸 GET 回读应为可用存储（enabled/type/url/description 一致），删除闭环。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class StoresManagementViewModelTests : GeoServerTestBase
    {
        private const string Ws = "gdtest_vm_ws_store";
        private const string VmDs = "gdtest_vm_good_ds"; // VM 载荷产物（FIXED-E40 后可用）
        private const string Desc = "vm created store";

        public StoresManagementViewModelTests(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public async Task CreateDataStore_ShapefilePayloadWithUrl_Fixed_E40()
        {
            if (!RequireGeoServer()) return;
            Assert.Equal(201, VmRest.PostJson("/rest/workspaces", "{\"workspace\":{\"name\":\"" + Ws + "\"}}"));

            var conn = VmTestKit.Connected();
            var vm = new StoresManagementViewModel(conn);
            try
            {
                await vm.LoadWorkspacesCommand.ExecuteAsync(null);
                Assert.Contains(Ws, vm.Workspaces);
                vm.SelectedWorkspace = Ws;
                await vm.LoadDataStoresCommand.ExecuteAsync(null);

                // VM 创建：默认目录留空 → 约定 url = "file:" + 存储名；带描述
                vm.NewDataStoreName = VmDs;
                vm.NewDataStoreDescription = Desc;
                await vm.CreateDataStoreCommand.ExecuteAsync(null);
                await VmTestKit.Settle();

                // 列表出现该存储，状态为创建成功
                Assert.Contains(VmDs, vm.DataStores.Select(d => d.Name).ToList());
                Assert.Equal(vm.L.StatusDataStoreCreated, vm.StatusMessage);

                // 独立 GET 交叉校验（期望值来自服务器原始响应）：可用 Shapefile 存储
                Assert.True(VmRest.DataStoreExists(Ws, VmDs));
                Assert.Equal("Shapefile", VmRest.DataStoreType(Ws, VmDs));
                Assert.True(VmRest.DataStoreEnabled(Ws, VmDs) == true, "存储应 enabled");
                Assert.Equal("file:" + VmDs, VmRest.DataStoreConnectionParam(Ws, VmDs, "url")); // 同名目录约定
                Assert.Equal(Desc, VmRest.DataStoreDescription(Ws, VmDs));                      // 描述回读一致

                // 删除闭环
                vm.SelectedDataStore = vm.DataStores.First(d => d.Name == VmDs);
                await vm.DeleteDataStoreCommand.ExecuteAsync(null);
                await VmTestKit.Settle();
                Assert.DoesNotContain(VmDs, vm.DataStores.Select(d => d.Name).ToList());
            }
            finally
            {
                VmTestKit.TryDeleteWorkspace(Ws);
                conn.Disconnect();
            }
        }

        [Fact]
        public async Task LoadDataStores_WithoutWorkspace_ReportsNoWorkspace()
        {
            var conn = new GeoServerConnectionService();
            var vm = new StoresManagementViewModel(conn);
            await vm.LoadDataStoresCommand.ExecuteAsync(null);
            Assert.Equal(vm.L.StatusNoWorkspaceSelected, vm.StatusMessage);
        }
    }
}
