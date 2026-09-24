using System;
using System.Linq;
using GeoServerDesktop.App.Models;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// MainWindowViewModel 无头测试。仅覆盖不依赖 DI 的行为（内部自建连接服务）：
    /// 默认初值、语言切换并恢复、各 Show* 命令切换 CurrentView、Disconnect 复位；
    /// 集成 Connect 走真实工厂并校验资源树；错误 URL 断言 FIXED-E32（Connect 后真实探测，失败即未连接）。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class MainWindowViewModelTests : GeoServerTestBase
    {
        private const string MwWs = "gdtest_vm_mw";

        public MainWindowViewModelTests(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public void Defaults_MatchKnownInitialValues()
        {
            var vm = new MainWindowViewModel();
            Assert.Equal("http://localhost:8080/geoserver", vm.BaseUrl);
            Assert.Equal("admin", vm.Username);
            Assert.Equal("geoserver", vm.Password);
            Assert.False(vm.IsConnected);
            Assert.Equal("Not connected", vm.StatusMessage);
            Assert.Empty(vm.ResourceTree);
            Assert.IsType<PlaceholderViewModel>(vm.CurrentView);
        }

        [Fact]
        public void ToggleLanguage_FlipsAndRestores()
        {
            var vm = new MainWindowViewModel();
            bool before = vm.L.AppHeaderSubtitle != "Administration";
            try
            {
                vm.ToggleLanguageCommand.Execute(null);
                Assert.NotEqual(before, vm.L.AppHeaderSubtitle != "Administration");
                Assert.Equal(vm.L.StatusNotConnected, vm.StatusMessage); // 切语言后按当前语言回显

                vm.ToggleLanguageCommand.Execute(null);
                Assert.Equal(before, vm.L.AppHeaderSubtitle != "Administration");
            }
            finally
            {
                if ((vm.L.AppHeaderSubtitle != "Administration") != before) vm.ToggleLanguageCommand.Execute(null);
            }
        }

        [Fact]
        public async Task ShowCommands_SwitchCurrentView_AllVariants()
        {
            var vm = new MainWindowViewModel { IsConnected = true }; // 直接置 VM 级标志以越过“请先连接”守卫

            // 记录 (命令, 期望目标视图) —— 覆盖全部 Show* 分支
            vm.ShowAboutCommand.Execute(null); Assert.Same(vm.AboutViewModel, vm.CurrentView);
            vm.ShowLayerPreviewCommand.Execute(null); Assert.Same(vm.MapPreviewViewModel, vm.CurrentView);
            await vm.ShowWorkspacesCommand.ExecuteAsync(null); Assert.Same(vm.WorkspaceManagementViewModel, vm.CurrentView);
            vm.ShowStoresCommand.Execute(null); Assert.Same(vm.StoresManagementViewModel, vm.CurrentView);
            vm.ShowLayersCommand.Execute(null); Assert.Same(vm.LayersManagementViewModel, vm.CurrentView);
            vm.ShowLayerGroupsCommand.Execute(null); Assert.Same(vm.LayerGroupsManagementViewModel, vm.CurrentView);
            vm.ShowStylesCommand.Execute(null); Assert.Same(vm.StyleManagementViewModel, vm.CurrentView);
            vm.ShowWMSSettingsCommand.Execute(null); Assert.Same(vm.WmsSettingsViewModel, vm.CurrentView);
            vm.ShowWFSSettingsCommand.Execute(null); Assert.Same(vm.WfsSettingsViewModel, vm.CurrentView);
            vm.ShowWCSSettingsCommand.Execute(null); Assert.Same(vm.WcsSettingsViewModel, vm.CurrentView);
            vm.ShowGlobalSettingsCommand.Execute(null); Assert.Same(vm.GlobalSettingsViewModel, vm.CurrentView);
            vm.ShowLoggingCommand.Execute(null); Assert.Same(vm.LoggingViewModel, vm.CurrentView);
            vm.ShowCachingDefaultsCommand.Execute(null); Assert.Same(vm.CachingDefaultsViewModel, vm.CurrentView);
            vm.ShowGridsetsCommand.Execute(null); Assert.Same(vm.GridsetsViewModel, vm.CurrentView);
            vm.ShowDiskQuotaCommand.Execute(null); Assert.Same(vm.DiskQuotaViewModel, vm.CurrentView);
            vm.ShowSecuritySettingsCommand.Execute(null); Assert.Same(vm.SecuritySettingsViewModel, vm.CurrentView);
            vm.ShowUsersGroupsCommand.Execute(null); Assert.Same(vm.UsersGroupsRolesViewModel, vm.CurrentView);
        }

        [Fact]
        public void DisconnectCommand_ResetsState()
        {
            var vm = new MainWindowViewModel { IsConnected = true };
            vm.CurrentView = vm.AboutViewModel;
            vm.ResourceTree.Add(new ResourceTreeNode { Name = "x", Type = ResourceType.GeoServer });

            vm.DisconnectCommand.Execute(null);

            Assert.False(vm.IsConnected);
            Assert.Empty(vm.ResourceTree);
            Assert.IsType<PlaceholderViewModel>(vm.CurrentView);
            Assert.Equal(vm.L.StatusDisconnected, vm.StatusMessage);
        }

        [Fact]
        public async Task ConnectCommand_RealFactory_PopulatesResourceTree()
        {
            if (!RequireGeoServer()) return;
            Assert.Equal(201, VmRest.PostJson("/rest/workspaces", "{\"workspace\":{\"name\":\"" + MwWs + "\"}}"));
            var vm = new MainWindowViewModel { BaseUrl = TestEnv.BaseUrl, Username = TestEnv.User, Password = TestEnv.Pass };
            try
            {
                await vm.ConnectCommand.ExecuteAsync(null);
                Assert.True(vm.IsConnected);
                Assert.NotEmpty(vm.ResourceTree);

                var root = vm.ResourceTree[0];
                var wsContainer = root.Children.FirstOrDefault(c => c.Type == ResourceType.WorkspacesContainer);
                Assert.NotNull(wsContainer);
                Assert.Contains(MwWs, wsContainer!.Children.Select(c => c.Name)); // 工作空间子节点含 gdtest 前缀

                await vm.RefreshResourcesCommand.ExecuteAsync(null); // 刷新不抛
                Assert.NotEmpty(vm.ResourceTree);
            }
            finally
            {
                vm.DisconnectCommand.Execute(null);
                VmTestKit.TryDeleteWorkspace(MwWs);
            }
        }

        [Fact]
        public async Task ConnectCommand_BadUrl_ProbeFails_Disconnected_Fixed_E32()
        {
            // FIXED-E32 翻转：Connect 后经 AboutService.GetVersionAsync 真实探测，
            // 不可达 URL → IsConnected 回退 false，状态为本地化“连接验证失败”而非“已连接”。
            var vm = new MainWindowViewModel { BaseUrl = "http://127.0.0.1:1/" };
            await vm.ConnectCommand.ExecuteAsync(null);

            Assert.False(vm.IsConnected);
            Assert.Empty(vm.ResourceTree);
            var failPrefix = string.Format(vm.L.StatusConnectVerifyFailed, "");
            Assert.StartsWith(failPrefix, vm.StatusMessage);
            vm.DisconnectCommand.Execute(null);
        }
    }
}
