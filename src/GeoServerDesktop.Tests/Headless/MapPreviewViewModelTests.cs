using System;
using GeoServerDesktop.App.ViewModels;
using Xunit;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// MapPreviewViewModel 无头测试。其 LoadWmsLayerAsync 仅拼接 WMS 预览 URL（不做 Mapsui 渲染），
    /// 故可离线断言 BaseUrl/PreviewUrl 拼接与命令。E36 修复后状态串走本地化单例，
    /// 故并入 GeoServerSerial 串行集合（与一切换语言的用例互斥）并按 vm.L.* 断言。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class MapPreviewViewModelTests
    {
        [Fact]
        public void Ctor_InitializesMap_AndMessages()
        {
            var vm = new MapPreviewViewModel();
            Assert.NotNull(vm.Map); // Mapsui.Map 在无头下可构造（仅数据模型，不依赖窗口）
            Assert.Equal(vm.L.StatusMapInitialized, vm.StatusMessage);
            Assert.Null(vm.BaseUrl);
            Assert.Null(vm.PreviewUrl);
        }

        [Fact]
        public async Task LoadWmsLayer_BuildsEscapedPreviewUrl()
        {
            var vm = new MapPreviewViewModel();
            await vm.LoadWmsLayerAsync("http://localhost:8765/geoserver", "sf", "states");

            Assert.Equal("http://localhost:8765/geoserver", vm.BaseUrl);
            Assert.Equal("sf", vm.CurrentWorkspace);
            Assert.NotNull(vm.PreviewUrl);
            Assert.Contains("/wms?", vm.PreviewUrl);
            Assert.Contains("request=GetMap", vm.PreviewUrl);
            Assert.Contains("layers=" + Uri.EscapeDataString("sf:states"), vm.PreviewUrl);
            Assert.Contains("format=image/png", vm.PreviewUrl);
            Assert.False(vm.IsLoading);
        }

        [Fact]
        public async Task LoadWmsLayer_EmptyWorkspace_NoLayerPrefix()
        {
            var vm = new MapPreviewViewModel();
            await vm.LoadWmsLayerAsync("http://host/geoserver", "", "solo");
            Assert.Contains("layers=solo", vm.PreviewUrl);
            Assert.DoesNotContain("solo%3A", vm.PreviewUrl); // 无 workspace 前缀
        }

        [Fact]
        public void ClearAndZoom_States()
        {
            var vm = new MapPreviewViewModel();
            vm.LoadWmsLayerAsync("http://h/geoserver", "w", "l").GetAwaiter().GetResult();
            Assert.NotNull(vm.PreviewUrl);

            vm.ClearLayersCommand.Execute(null);
            Assert.Null(vm.PreviewUrl);
            Assert.Null(vm.BaseUrl);
            Assert.Equal(vm.L.StatusPreviewCleared, vm.StatusMessage);

            vm.ZoomToExtentCommand.Execute(null);
            Assert.Equal(vm.L.StatusMapReadyForPreview, vm.StatusMessage);
        }
    }
}
