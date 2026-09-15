using GeoServerDesktop.Tests.Infrastructure;
using GeoServerDesktop.Tests.RealData;

namespace GeoServerDesktop.Tests.RealDataTests
{
    /// <summary>
    /// 任务4：WCS 数据面。GetCapabilities 含 coverage；2.0.1 GetCoverage GeoTIFF 解析尺寸/位深/ProjCS、
    /// 仿射标签（源）、5+ 像元对确定式；1.0.0 兼容形态（本 GeoServer 不受理则 Warn）。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class WcsDataPlaneTests : RealDataTestBase
    {
        private static string Dem => E2ePublishHelper.Ws + ":" + E2ePublishHelper.DemLayer;

        public WcsDataPlaneTests(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public void Capabilities_HasCoverage()
        {
            if (!RequireReady()) return;
            Check.ThrowOnFail(RealDataChecks.WcsCapabilitiesHasCoverage(E2ePublishHelper.DemLayer, "dem"));
        }

        [Fact]
        public void GetCoverage_201_GeoTiff_HeadersAndPixels()
        {
            if (!RequireReady()) return;
            Check.ThrowOnFail(RealDataChecks.WcsCoverage201(DataDir, Dem, "gdtest_dem.tif", "dem"));
        }

        [Fact]
        public void GetCoverage_100_Compat()
        {
            if (!RequireReady()) return;
            Check.ThrowOnFail(RealDataChecks.WcsCoverage100Compat(Dem, "dem"));
        }
    }
}
