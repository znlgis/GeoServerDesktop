using GeoServerDesktop.Tests.Infrastructure;
using GeoServerDesktop.Tests.RealData;

namespace GeoServerDesktop.Tests.RealDataTests
{
    /// <summary>
    /// 任务3：WMS 数据面。GetCapabilities(1.1.1/1.3.0) 列出图层；GetMap PNG 魔数与尺寸；
    /// 红色 SLD 像素真值（存在红像素 + 非白屏）；既有演示数据只读出图非纯色；GetFeatureInfo 命中 poly_00。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class WmsDataPlaneTests : RealDataTestBase
    {
        private static string Poly => E2ePublishHelper.Ws + ":" + E2ePublishHelper.PolyLayer;

        public WmsDataPlaneTests(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public void Capabilities_ListsLayer_111_and_130()
        {
            if (!RequireReady()) return;
            Check.ThrowOnFail(
                RealDataChecks.WmsCapabilitiesHasLayer(Poly, "1.1.1"),
                RealDataChecks.WmsCapabilitiesHasLayer(Poly, "1.3.0"));
        }

        [Fact]
        public void GetMap_PngMagic_And_Dims()
        {
            if (!RequireReady()) return;
            Check.ThrowOnFail(RealDataChecks.WmsGetMapPngDims(Poly, 220, 120));
        }

        [Fact]
        public void GetMap_RedStyle_HasRedPixelsAndNotBlank()
        {
            if (!RequireReady()) return;
            Check.ThrowOnFail(RealDataChecks.WmsRedPixels(Poly, E2ePublishHelper.RedStyle));
        }

        [Fact]
        public void GetMap_ExistingDemoData_NonMonochrome()
        {
            if (!RequireReady()) return;
            // 注：本机演示数据无 sf:states，实际状态面是 topp:states；只读交叉验证数据→像素链路。
            Check.ThrowOnFail(RealDataChecks.WmsDemoLayerFromCaps("topp:states", "topp-states"));
        }

        [Fact]
        public void GetFeatureInfo_HitsPoly00()
        {
            if (!RequireReady()) return;
            Check.ThrowOnFail(RealDataChecks.WmsFeatureInfoPoly00(DataDir, Poly, "poly00"));
        }
    }
}
