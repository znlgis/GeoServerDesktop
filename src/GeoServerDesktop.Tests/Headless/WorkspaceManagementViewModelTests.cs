using System;
using System.Linq;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// WorkspaceManagementViewModel 真实连接集成测试（无头驱动命令流，连真 GeoServer）。
    /// 资源前缀 gdtest_vm，测后显式删除 + finally 兜底；不可达则 Skip。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class WorkspaceManagementViewModelTests : GeoServerTestBase
    {
        private const string Ws = "gdtest_vm_ws1";
        public WorkspaceManagementViewModelTests(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public async Task CreateLoadDelete_FullFlow()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new WorkspaceManagementViewModel(conn);
            try
            {
                // 1) 加载：含既有工作空间 "sf"
                await vm.LoadWorkspacesCommand.ExecuteAsync(null);
                Assert.False(vm.IsLoading);
                Assert.Contains("sf", vm.Workspaces);
                Assert.Equal(string.Format(vm.L.StatusWorkspacesLoaded, vm.Workspaces.Count), vm.StatusMessage);

                // 2) 创建 gdtest_vm_ws1 → 列表含之，状态为成功语义（重载后的 loaded）
                vm.NewWorkspaceName = Ws;
                await vm.CreateWorkspaceCommand.ExecuteAsync(null);
                Assert.Contains(Ws, vm.Workspaces);
                Assert.Equal(string.Format(vm.L.StatusWorkspacesLoaded, vm.Workspaces.Count), vm.StatusMessage);
                Assert.Equal(string.Empty, vm.NewWorkspaceName); // 成功后清空输入
                Assert.True(VmRest.WorkspaceExists(Ws));          // 独立校验确实落库

                // 3) 空名失败路径（早退，不重载）
                vm.NewWorkspaceName = "   ";
                await vm.CreateWorkspaceCommand.ExecuteAsync(null);
                Assert.Equal(vm.L.StatusWorkspaceNameRequired, vm.StatusMessage);

                // 4) 重名失败路径：再次创建已存在的 Ws → create-failed 语义
                vm.NewWorkspaceName = Ws;
                await vm.CreateWorkspaceCommand.ExecuteAsync(null);
                var failPrefix = string.Format(vm.L.StatusWorkspaceCreateFailed, "");
                Assert.StartsWith(failPrefix, vm.StatusMessage);

                // 5) 删除 → 不在列表 + 服务器消失
                vm.SelectedWorkspace = Ws;
                await vm.DeleteWorkspaceCommand.ExecuteAsync(null);
                Assert.DoesNotContain(Ws, vm.Workspaces);
                Assert.Equal(string.Format(vm.L.StatusWorkspacesLoaded, vm.Workspaces.Count), vm.StatusMessage);
                Assert.False(VmRest.WorkspaceExists(Ws));
                Assert.Null(vm.SelectedWorkspace);

                // 6) 全程 IsLoading 最终复位
                Assert.False(vm.IsLoading);
            }
            finally
            {
                VmTestKit.TryDeleteWorkspace(Ws);
                conn.Disconnect();
            }
        }

        [Fact]
        public async Task Load_WithoutConnection_ReportsNotConnected()
        {
            // 未连接分支：不触网即可断言（离线安全）
            var conn = new GeoServerConnectionService();
            var vm = new WorkspaceManagementViewModel(conn);
            await vm.LoadWorkspacesCommand.ExecuteAsync(null);
            Assert.Equal(vm.L.StatusNotConnected, vm.StatusMessage);
            Assert.Empty(vm.Workspaces);
        }
    }
}
