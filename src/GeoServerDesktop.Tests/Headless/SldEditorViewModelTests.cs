using System;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.GeoServerClient.Sld;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// SldEditorViewModel 集成测试（M3）：样式列表加载 → 新建/保存（创建+更新）→
    /// 加载解析（名字信任边界）→ 双模式切换（含失败路径）→ 应用前校验拦截 → 预览 URL。
    /// 独立复核走裸 REST（VmRest）；样式名前缀 gdtest_vm，测后兜底清理。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class SldEditorViewModelTests : GeoServerTestBase
    {
        private const string StyleName = "gdtest_vm_sld_flow";
        private const string LoadStyleName = "gdtest_vm_sld_load";

        public SldEditorViewModelTests(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public async Task LoadStyles_PopulatesList()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new SldEditorViewModel(conn);
            try
            {
                await vm.LoadStylesCommand.ExecuteAsync(null);
                Assert.NotEmpty(vm.Styles);
                Assert.Contains("polygon", vm.Styles); // 示例数据内置样式
                Assert.Equal(string.Format(vm.L.SldEditorStatusStylesLoaded, vm.Styles.Count), vm.StatusMessage);
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public async Task NewStyle_Save_CreateThenUpdate()
        {
            if (!RequireGeoServer()) return;
            VmTestKit.TryDeleteStyle(StyleName);
            var conn = VmTestKit.Connected();
            var vm = new SldEditorViewModel(conn);
            try
            {
                vm.NewStyleCommand.Execute(null);
                Assert.Single(vm.Rules);
                Assert.Equal("Polygon", vm.Rules[0].SymbolizerKind);
                Assert.Equal("#FF0000", vm.Rules[0].FillColor);

                vm.StyleName = StyleName;
                await vm.SaveStyleCommand.ExecuteAsync(null);
                Assert.True(VmRest.StyleExists(StyleName), vm.StatusMessage);
                Assert.Equal(string.Format(vm.L.SldEditorStatusSaved, StyleName), vm.StatusMessage);

                // 独立复核内容（裸 REST 读 .sld）
                var sld = VmRest.Get("/rest/styles/" + StyleName + ".sld");
                Assert.NotNull(sld);
                Assert.Contains("#FF0000", sld);

                // 更新：改蓝色 → 再保存
                vm.Rules[0].FillColor = "#0000FF";
                await vm.SaveStyleCommand.ExecuteAsync(null);
                var sld2 = VmRest.Get("/rest/styles/" + StyleName + ".sld");
                Assert.NotNull(sld2);
                Assert.Contains("#0000FF", sld2);
                Assert.DoesNotContain("#FF0000", sld2);
            }
            finally
            {
                VmTestKit.TryDeleteStyle(StyleName);
                conn.Disconnect();
            }
        }

        [Fact]
        public async Task LoadSelectedStyle_ParsesRules_AndUsesRestName()
        {
            if (!RequireGeoServer()) return;
            VmTestKit.TryDeleteStyle(LoadStyleName);
            var conn = VmTestKit.Connected();
            var vm = new SldEditorViewModel(conn);
            try
            {
                // 先落库一个已知内容的红面样式
                await conn.GetStyleService().CreateStyleAsync(LoadStyleName, RedPolygonSld(LoadStyleName));

                await vm.LoadStylesCommand.ExecuteAsync(null);
                vm.SelectedStyle = LoadStyleName;
                await vm.LoadSelectedStyleCommand.ExecuteAsync(null);

                // 名字信任边界：样式名来自 REST 资源名（SLD 内名字会被服务端规范化，不可依赖）
                Assert.Equal(LoadStyleName, vm.StyleName);
                Assert.Single(vm.Rules);
                Assert.Equal("Polygon", vm.Rules[0].SymbolizerKind);
                Assert.Equal("#FF0000", vm.Rules[0].FillColor);
                Assert.False(vm.IsSourceMode);
                Assert.Equal(string.Format(vm.L.SldEditorStatusStyleLoaded, LoadStyleName), vm.StatusMessage);
            }
            finally
            {
                VmTestKit.TryDeleteStyle(LoadStyleName);
                conn.Disconnect();
            }
        }

        [Fact]
        public void SwitchModes_RoundTrip()
        {
            var vm = new SldEditorViewModel(new GeoServerConnectionService());
            vm.NewStyleCommand.Execute(null);
            vm.StyleName = "whatever";
            vm.Rules[0].FillColor = "#00FF00";
            vm.Rules[0].FilterEnabled = true;
            vm.Rules[0].FilterProperty = "pop";
            vm.Rules[0].FilterOperator = ">";
            vm.Rules[0].FilterValue = "1000";

            vm.SwitchToSourceCommand.Execute(null);
            Assert.True(vm.IsSourceMode);
            Assert.Contains("PolygonSymbolizer", vm.SourceXml);
            Assert.Contains("#00FF00", vm.SourceXml);
            Assert.Contains("PropertyIsGreaterThan", vm.SourceXml);
            Assert.Equal(vm.L.SldEditorStatusSwitchedToSource, vm.StatusMessage);

            vm.SwitchToStructuredCommand.Execute(null);
            Assert.False(vm.IsSourceMode);
            Assert.Single(vm.Rules);
            Assert.Equal("#00FF00", vm.Rules[0].FillColor);
            Assert.True(vm.Rules[0].FilterEnabled);
            Assert.Equal("pop", vm.Rules[0].FilterProperty);
            Assert.Equal(">", vm.Rules[0].FilterOperator);
            Assert.Equal(vm.L.SldEditorStatusSwitchedToStructured, vm.StatusMessage);
        }

        [Fact]
        public void SwitchToStructured_InvalidXml_StaysInSource()
        {
            var vm = new SldEditorViewModel(new GeoServerConnectionService());
            vm.IsSourceMode = true;
            vm.SourceXml = "<broken";

            vm.SwitchToStructuredCommand.Execute(null);

            Assert.True(vm.IsSourceMode);
            Assert.StartsWith(Prefix(vm.L.SldEditorStatusParseFailed), vm.StatusMessage);
        }

        [Fact]
        public async Task Save_InvalidSource_Rejected()
        {
            if (!RequireGeoServer()) return;
            VmTestKit.TryDeleteStyle(StyleName);
            var conn = VmTestKit.Connected();
            var vm = new SldEditorViewModel(conn);
            try
            {
                vm.IsSourceMode = true;
                vm.StyleName = StyleName;
                vm.SourceXml = "<?xml version=\"1.0\"?><StyledLayerDescriptor version=\"1.0.0\"><NotValid/></StyledLayerDescriptor>";

                await vm.SaveStyleCommand.ExecuteAsync(null);

                Assert.False(VmRest.StyleExists(StyleName)); // 本地校验拦截，未创建
                Assert.StartsWith(Prefix(vm.L.SldEditorStatusInvalid), vm.StatusMessage);
            }
            finally
            {
                VmTestKit.TryDeleteStyle(StyleName);
                conn.Disconnect();
            }
        }

        [Fact]
        public async Task Preview_GeneratesWmsUrl_AfterSave()
        {
            if (!RequireGeoServer()) return;
            VmTestKit.TryDeleteStyle(StyleName);
            var conn = VmTestKit.Connected();
            var vm = new SldEditorViewModel(conn);
            try
            {
                vm.NewStyleCommand.Execute(null);
                vm.StyleName = StyleName;
                vm.SelectedPreviewLayer = "gdtest_ws_e2e:gdtest_poly";

                // 未保存 → 提示先保存
                vm.PreviewStyleCommand.Execute(null);
                Assert.Equal(vm.L.SldEditorStatusNeedSaveForPreview, vm.StatusMessage);

                // 保存后 → 生成 WMS GetMap 地址
                await vm.SaveStyleCommand.ExecuteAsync(null);
                vm.PreviewStyleCommand.Execute(null);
                Assert.NotNull(vm.PreviewUrl);
                Assert.Contains("request=GetMap", vm.PreviewUrl);
                Assert.Contains("styles=" + StyleName, vm.PreviewUrl);
                Assert.Contains("layers=" + Uri.EscapeDataString("gdtest_ws_e2e:gdtest_poly"), vm.PreviewUrl);
                Assert.StartsWith(Prefix(vm.L.SldEditorStatusPreviewReady), vm.StatusMessage);
            }
            finally
            {
                VmTestKit.TryDeleteStyle(StyleName);
                conn.Disconnect();
            }
        }

        [Fact]
        public void AddRemoveRule_SelectionManagement()
        {
            var vm = new SldEditorViewModel(new GeoServerConnectionService());
            vm.NewStyleCommand.Execute(null);
            Assert.Single(vm.Rules);

            vm.AddRuleCommand.Execute(null);
            Assert.Equal(2, vm.Rules.Count);
            Assert.Same(vm.Rules[1], vm.SelectedRule);

            vm.RemoveRuleCommand.Execute(null);
            Assert.Single(vm.Rules);
            Assert.Same(vm.Rules[0], vm.SelectedRule);

            vm.RemoveRuleCommand.Execute(null);
            Assert.Empty(vm.Rules);
            Assert.Null(vm.SelectedRule);

            vm.RemoveRuleCommand.Execute(null); // 无选中 → 提示
            Assert.Equal(vm.L.SldEditorStatusNoRuleSelected, vm.StatusMessage);
        }

        // ---- 辅助 ----

        /// <summary>本地化模板取参数前缀（语言无关的消息断言）。</summary>
        private static string Prefix(string template) => template.Replace("{0}", string.Empty);

        /// <summary>纯红面 SLD（经 M3 生成器产出）。</summary>
        private static string RedPolygonSld(string layerName)
        {
            var doc = new SldDocument { LayerName = layerName };
            doc.Rules.Add(new SldRule
            {
                Symbolizer = new SldPolygonSymbolizer
                {
                    FillColor = "#FF0000",
                    FillOpacity = 1,
                    StrokeColor = "#FF0000",
                    StrokeWidth = 1,
                },
            });
            return SldBuilder.Build(doc);
        }
    }
}
