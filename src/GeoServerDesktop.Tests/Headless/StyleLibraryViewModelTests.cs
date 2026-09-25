using System;
using System.Collections.Generic;
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
    /// StyleLibraryViewModel 集成测试（M3）：样式使用总览（被引用标记与裸 REST 交叉复核）→
    /// 未引用筛选 → 删除未引用样式（落库 → VM 删除 → 独立复核）→ 引用中样式删除拦截。
    /// 独立复核走裸 REST（VmRest）；样式名前缀 gdtest_vm，测后兜底清理。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class StyleLibraryViewModelTests : GeoServerTestBase
    {
        private const string UnusedStyleName = "gdtest_vm_sld_unused";

        public StyleLibraryViewModelTests(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public async Task LoadUsages_MarksUsedAndUnused()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new StyleLibraryViewModel(conn);
            try
            {
                await vm.LoadUsagesCommand.ExecuteAsync(null);

                Assert.NotEmpty(vm.Usages);
                var total = vm.Usages.Count;
                var unused = vm.Usages.Count(u => !u.IsUsed);
                Assert.Equal(string.Format(vm.L.StyleLibStatusLoaded, total, unused), vm.StatusMessage);

                // 被引用侧：环境含示例数据图层 → 必有被引用样式；与裸 REST 交叉复核
                // （取任一被引用样式的首个引用图层，直查图层详情确认 defaultStyle 确实指向该样式，不硬编码名字）
                var used = vm.Usages.FirstOrDefault(u => u.IsUsed);
                Assert.NotNull(used);
                var layerFull = used!.Layers.First();
                var parts = layerFull.Split(':');
                Assert.True(parts.Length >= 2, "引用图层应为 qualified 名：" + layerFull);
                var (dsName, dsHref) = VmRest.LayerDefaultStyleRef(parts[0], parts[1]);
                Assert.Equal(used.StyleName, dsName);
                Assert.NotNull(dsHref);
                Assert.DoesNotContain("/workspaces/", dsHref); // 全局样式引用（工作空间样式不计入总览）

                // 未引用侧：无引用图层 + 显示为本地化"未引用"
                var unusedItem = vm.Usages.FirstOrDefault(u => !u.IsUsed);
                Assert.NotNull(unusedItem);
                Assert.Empty(unusedItem!.Layers);
                Assert.Equal(vm.L.StyleLibUnused, unusedItem.LayersDisplay);
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public async Task ShowUnusedOnly_Filters()
        {
            if (!RequireGeoServer()) return;
            VmTestKit.TryDeleteStyle(UnusedStyleName);
            var conn = VmTestKit.Connected();
            var vm = new StyleLibraryViewModel(conn);
            try
            {
                // 落库一个确定的未引用样式，保证筛选断言非空
                await conn.GetStyleService().CreateStyleAsync(UnusedStyleName, MinimalPolygonSld(UnusedStyleName));
                Assert.True(VmRest.StyleExists(UnusedStyleName));

                await vm.LoadUsagesCommand.ExecuteAsync(null);
                var total = vm.Usages.Count;
                var unusedCount = vm.Usages.Count(u => !u.IsUsed);
                Assert.Contains(vm.Usages, u => u.StyleName == UnusedStyleName && !u.IsUsed);

                vm.ShowUnusedOnly = true;
                Assert.Equal(unusedCount, vm.Usages.Count);
                Assert.All(vm.Usages, u => Assert.False(u.IsUsed));
                Assert.Contains(vm.Usages, u => u.StyleName == UnusedStyleName);

                vm.ShowUnusedOnly = false;
                Assert.Equal(total, vm.Usages.Count);
            }
            finally
            {
                VmTestKit.TryDeleteStyle(UnusedStyleName);
                conn.Disconnect();
            }
        }

        [Fact]
        public async Task DeleteUnused_Removes()
        {
            if (!RequireGeoServer()) return;
            VmTestKit.TryDeleteStyle(UnusedStyleName);
            var conn = VmTestKit.Connected();
            var vm = new StyleLibraryViewModel(conn);
            try
            {
                await conn.GetStyleService().CreateStyleAsync(UnusedStyleName, MinimalPolygonSld(UnusedStyleName));
                Assert.True(VmRest.StyleExists(UnusedStyleName));

                await vm.LoadUsagesCommand.ExecuteAsync(null);
                var item = vm.Usages.FirstOrDefault(u => u.StyleName == UnusedStyleName);
                Assert.NotNull(item);
                Assert.False(item!.IsUsed);

                vm.SelectedUsage = item;
                await vm.DeleteSelectedCommand.ExecuteAsync(null);

                Assert.Equal(string.Format(vm.L.StyleLibStatusDeleted, UnusedStyleName), vm.StatusMessage);
                Assert.False(VmRest.StyleExists(UnusedStyleName));                     // 独立复核：服务端已删除
                Assert.DoesNotContain(vm.Usages, u => u.StyleName == UnusedStyleName); // 刷新后列表不含
            }
            finally
            {
                VmTestKit.TryDeleteStyle(UnusedStyleName);
                conn.Disconnect();
            }
        }

        [Fact]
        public async Task DeleteInUse_Blocked()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new StyleLibraryViewModel(conn);
            try
            {
                await vm.LoadUsagesCommand.ExecuteAsync(null);
                var used = vm.Usages.FirstOrDefault(u => u.IsUsed);
                if (used == null)
                {
                    SkipLog.Skip("环境无被引用样式，无法验证删除拦截");
                    return;
                }

                vm.SelectedUsage = used;
                await vm.DeleteSelectedCommand.ExecuteAsync(null);

                Assert.Equal(vm.L.StyleLibStatusDeleteInUse, vm.StatusMessage);
                Assert.True(VmRest.StyleExists(used.StyleName)); // 独立复核：引用中样式未被删除
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public async Task DeleteSelected_Guards_NoSelection_And_InUse()
        {
            // 无需服务器：守卫在触达服务前返回（CI 无 GeoServer 也执行）
            var vm = new StyleLibraryViewModel(new GeoServerConnectionService());

            await vm.DeleteSelectedCommand.ExecuteAsync(null);
            Assert.Equal(vm.L.StyleLibStatusSelectFirst, vm.StatusMessage);

            vm.SelectedUsage = new StyleUsageItem("whatever", isUsed: true, new List<string> { "ws:layer" });
            await vm.DeleteSelectedCommand.ExecuteAsync(null);
            Assert.Equal(vm.L.StyleLibStatusDeleteInUse, vm.StatusMessage);
        }

        // ---- 辅助 ----

        /// <summary>最小合法面样式 SLD（经 M3 生成器产出，供"未引用样式"用例落库）。</summary>
        private static string MinimalPolygonSld(string name)
        {
            var doc = new SldDocument { LayerName = name };
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
