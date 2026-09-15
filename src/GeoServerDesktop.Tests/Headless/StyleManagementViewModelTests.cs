using System;
using System.Linq;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// StyleManagementViewModel 集成测试。读取路径正常（GET 列表/单个 .sld 走扩展名协商）；
    /// 原 E41（GeoServerHttpClient 全局 “Accept: application/json” 致 style POST 500）已在产品端修复，
    /// 本层据新现实固化“创建成功”基线（UploadStyle_Create_Succeeds_E41Fixed）。样式名用 gdtest_vm 前缀。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class StyleManagementViewModelTests : GeoServerTestBase
    {
        private const string StyleName = "gdtest_vm_style";

        public StyleManagementViewModelTests(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public async Task LoadStyles_And_Content_ReadPath_Works()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new StyleManagementViewModel(conn);
            try
            {
                await vm.LoadStylesCommand.ExecuteAsync(null);
                Assert.NotEmpty(vm.Styles); // 内置样式存在（GeoServer 出厂即有若干）
                Assert.Equal(string.Format(vm.L.StatusStylesLoaded, vm.Styles.Count), vm.StatusMessage);

                // 选中一个既有样式，回读其 SLD（.sld GET 以扩展名协商，绕开 Accept 缺陷）
                vm.SelectedStyle = vm.Styles.First();
                await vm.LoadStyleContentCommand.ExecuteAsync(null);
                Assert.False(string.IsNullOrWhiteSpace(vm.SldContent));
                Assert.False(vm.IsLoading);
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public async Task UploadStyle_Create_Succeeds_E41Fixed()
        {
            if (!RequireGeoServer()) return;
            VmTestKit.TryDeleteStyle(StyleName);
            var conn = VmTestKit.Connected();
            var vm = new StyleManagementViewModel(conn);
            try
            {
                vm.CreateSampleSldCommand.Execute(null);
                vm.NewStyleName = StyleName;
                await vm.UploadStyleCommand.ExecuteAsync(null);

                // 新现实：产品端 GeoServerHttpClient Accept 头缺陷已修复 → style 创建 POST 成功，
                // 样式确实落库并被 VM 重载进列表（上传成功后内部 LoadStyles 回写 loaded 状态；
                // 原 E41 “500 拒绝”固化基线就此作废）。
                Assert.False(vm.IsLoading);
                Assert.True(VmRest.StyleExists(StyleName), "Accept 头修复后样式应创建成功");
                Assert.Contains(StyleName, vm.Styles);
                Assert.Equal(string.Format(vm.L.StatusStylesLoaded, vm.Styles.Count), vm.StatusMessage);
            }
            finally
            {
                VmTestKit.TryDeleteStyle(StyleName);
                conn.Disconnect();
            }
        }

        [Fact]
        public async Task Upload_MissingNameOrSld_EarlyReturn()
        {
            var conn = new GeoServerConnectionService();
            var vm = new StyleManagementViewModel(conn);
            vm.NewStyleName = "";
            vm.SldContent = "x";
            await vm.UploadStyleCommand.ExecuteAsync(null);
            Assert.Equal(vm.L.StatusStyleNameRequired, vm.StatusMessage);

            vm.NewStyleName = "whatever";
            vm.SldContent = "";
            await vm.UploadStyleCommand.ExecuteAsync(null);
            Assert.Equal(vm.L.StatusSldContentRequired, vm.StatusMessage);
        }
    }
}
