using GeoServerDesktop.Tests.Infrastructure;
using GeoServerDesktop.Tests.RealData;

namespace GeoServerDesktop.Tests.RealDataTests
{
    /// <summary>
    /// 任务2：WFS 数据面。GetCapabilities/typenames、hits 计数、逐属性对 DBF 推导期望、
    /// 几何并集 bbox 对 SHP 头、几何结构（双部件/洞）、CQL、BBOX、重投影、PostGIS↔shapefile 交叉比对。
    /// 全部走裸 HttpClient（OgcProbe），期望值来自数据文件，不回抄被测系统。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class WfsDataPlaneTests : RealDataTestBase
    {
        private static string Poly => E2ePublishHelper.Ws + ":" + E2ePublishHelper.PolyLayer;
        private static string Lines => E2ePublishHelper.Ws + ":" + E2ePublishHelper.LinesLayer;
        private static string Pg => E2ePublishHelper.Ws + ":" + E2ePublishHelper.PgPolyLayer;

        public WfsDataPlaneTests(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public void Capabilities_ListsBothTypenames()
        {
            if (!RequireReady()) return;
            Check.ThrowOnFail(RealDataChecks.WfsCapabilitiesHasTypes(E2ePublishHelper.PolyLayer, E2ePublishHelper.LinesLayer));
        }

        [Fact]
        public void Hits_Counts()
        {
            if (!RequireReady()) return;
            Check.ThrowOnFail(
                RealDataChecks.WfsHitsCount(Poly, 12, "poly"),
                RealDataChecks.WfsHitsCount(Lines, 8, "lines"));
        }

        [Fact]
        public void Attributes_MatchDbfHeader()
        {
            if (!RequireReady()) return;
            Check.ThrowOnFail(
                RealDataChecks.WfsAttributesMatchFileHeader(DataDir, Poly, "poly"),
                RealDataChecks.WfsAttributesMatchFileHeader(DataDir, Lines, "lines"));
        }

        [Fact]
        public void Geometry_BboxMatchesShpHeader()
        {
            if (!RequireReady()) return;
            Check.ThrowOnFail(
                RealDataChecks.WfsGeometryBboxMatchesShpHeader(DataDir, Poly, "gdtest_poly.shp", "poly"),
                RealDataChecks.WfsGeometryBboxMatchesShpHeader(DataDir, Lines, "gdtest_lines.shp", "lines"));
        }

        [Fact]
        public void Geometry_StructureMultipartAndHole()
        {
            if (!RequireReady()) return;
            Check.ThrowOnFail(RealDataChecks.WfsGeometryStructure(DataDir, Poly, "poly"));
        }

        [Fact]
        public void Cql_IdGreaterThan100_PicksPoly10Poly11()
        {
            if (!RequireReady()) return;
            Check.ThrowOnFail(RealDataChecks.WfsCqlIdGreaterThan(DataDir, Poly, 100, "ID>100"));
        }

        [Fact]
        public void Bbox_FilterHitsHoledRecord()
        {
            if (!RequireReady()) return;
            Check.ThrowOnFail(RealDataChecks.WfsBboxFilter(DataDir, Poly, "bbox(0,5,1,6)"));
        }

        [Fact]
        public void Reprojection_TransformsGeometry()
        {
            if (!RequireReady()) return;
            Check.ThrowOnFail(RealDataChecks.WfsReprojectionTransforms(Poly, "poly"));
        }

        [Fact]
        public void PostGIS_MatchesShapefile()
        {
            if (!RequireReady()) return;
            Check.ThrowOnFail(RealDataChecks.WfsPostgisMatchesShapefile(DataDir, Pg, Poly, "pg-vs-shp"));
        }
    }
}
