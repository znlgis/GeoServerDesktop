using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GeoServerDesktop.Tests.Infrastructure;
using GeoServerDesktop.Tests.RealData;
using Xunit;

namespace GeoServerDesktop.Tests.RealDataTests;

/// <summary>
/// 扩展真实数据集的 L3 服务面保真（与控制台 harness 同一套检查函数，双形态复用）：
/// 字段类型全覆盖、DBF 编码三变体、几何形态（NULL 几何 / Z / 自相交 / 0 记录 / 超高顶点）、
/// CJK 图层名全链路、大表分页、脏数据负路径、栅格像元与元数据。
/// 期望值一律从 .shp/.dbf/.cpg/.tif 文件本体独立推导；数据或服务器缺失时按 SkipLog 显式登记。
/// </summary>
[Collection("GeoServerSerial")]
public class ExtendedFidelityIT : GeoServerTestBase
{
    public ExtendedFidelityIT(GeoServerFixture fx) : base(fx) { }

    private static bool Skip(string why) => SkipLog.Skip(why);

    private bool Ready()
    {
        if (!RequireGeoServer()) return false;
        if (!ExtendedFixture.DataPresent)
            return Skip("扩展数据集缺失（请先运行 tests/testdata/generate_testdata.py）");
        return true;
    }

    [Fact]
    public void Types_FieldTypeFidelity_AllKinds()
    {
        if (!Ready()) return;
        using var fx = new ExtendedFixture();
        try
        {
            var s = Ext("gdtest_x_types");
            var r = fx.EnsurePublished(s);
            Check.ThrowOnFail(Check.Cond(r.Success, "IT/publish:types", "ok", r.Message));
            Check.ThrowOnFail(RealDataFidelity.AttributesAllFields(ExtendedFixture.VecDir, "gdtest_types",
                s.Qualified, "types"));
            Check.ThrowOnFail(RealDataFidelity.NativeBboxMatchesShpHeader(ExtendedFixture.VecDir, "gdtest_types",
                ExtendedFixture.Ws, ExtendedFixture.StoreFor("vec"), s.LayerName, "types"));
        }
        finally { if (!ExtendedFixture.Keep) ExtendedFixture.Wipe(fx.Factory); }
    }

    [Fact]
    public void DbfEncoding_ThreeVariants_ClientMustFlagUndeclaredGbk()
    {
        if (!Ready()) return;
        using var fx = new ExtendedFixture();
        try
        {
            var utf8 = Ext("gdtest_x_enc_utf8");
            var gbkCpg = Ext("gdtest_x_enc_gbk_cpg");
            var gbkNo = Ext("gdtest_x_enc_gbk_nocpg");
            foreach (var s in new[] { utf8, gbkCpg, gbkNo })
                Check.ThrowOnFail(Check.Cond(fx.EnsurePublished(s).Success, "IT/publish:" + s.LayerName, "ok", "发布失败"));
            Check.ThrowOnFail(RealDataFidelity.EncodingRiskBaseline(ExtendedFixture.VecDir, "enc_utf8", utf8.Qualified, "utf8-cpg", false));
            Check.ThrowOnFail(RealDataFidelity.EncodingRiskBaseline(ExtendedFixture.VecDir, "enc_gbk_cpg", gbkCpg.Qualified, "gbk-cpg", false));
            Check.ThrowOnFail(RealDataFidelity.EncodingRiskBaseline(ExtendedFixture.VecDir, "enc_gbk_nocpg", gbkNo.Qualified, "gbk-nocpg", true));
        }
        finally { if (!ExtendedFixture.Keep) ExtendedFixture.Wipe(fx.Factory); }
    }

    [Fact]
    public void Points_NullGeometry_AttributesCompleteGeometryOmitted()
    {
        if (!Ready()) return;
        using var fx = new ExtendedFixture();
        try
        {
            var s = Ext("gdtest_x_points");
            Check.ThrowOnFail(Check.Cond(fx.EnsurePublished(s).Success, "IT/publish:points", "ok", "发布失败"));
            Check.ThrowOnFail(RealDataFidelity.NullGeometryHandling(ExtendedFixture.VecDir, "gdtest_points", s.Qualified, "points"));
        }
        finally { if (!ExtendedFixture.Keep) ExtendedFixture.Wipe(fx.Factory); }
    }

    [Fact]
    public void PolygonZ_ZDimensionPreserved()
    {
        if (!Ready()) return;
        using var fx = new ExtendedFixture();
        try
        {
            var s = Ext("gdtest_x_polyz");
            Check.ThrowOnFail(Check.Cond(fx.EnsurePublished(s).Success, "IT/publish:polyz", "ok", "发布失败"));
            Check.ThrowOnFail(RealDataFidelity.ZDimensionPreserved(ExtendedFixture.VecDir, "gdtest_polyz", s.Qualified, "polyz"));
        }
        finally { if (!ExtendedFixture.Keep) ExtendedFixture.Wipe(fx.Factory); }
    }

    [Fact]
    public void SelfIntersectingGeometry_NotSilentlyRewritten()
    {
        if (!Ready()) return;
        using var fx = new ExtendedFixture();
        try
        {
            var s = Ext("gdtest_x_selfint");
            Check.ThrowOnFail(Check.Cond(fx.EnsurePublished(s).Success, "IT/publish:selfint", "ok", "发布失败"));
            Check.ThrowOnFail(RealDataFidelity.SelfIntersectionPreserved(ExtendedFixture.VecDir, "gdtest_selfint", s.Qualified, "selfint"));
        }
        finally { if (!ExtendedFixture.Keep) ExtendedFixture.Wipe(fx.Factory); }
    }

    [Fact]
    public void ZeroRecordLayer_PublishableAndServesEmpty()
    {
        if (!Ready()) return;
        using var fx = new ExtendedFixture();
        try
        {
            var s = Ext("gdtest_x_empty");
            Check.ThrowOnFail(Check.Cond(fx.EnsurePublished(s).Success, "IT/publish:empty", "ok", "发布失败"));
            Check.ThrowOnFail(RealDataFidelity.EmptyLayerBehavior(ExtendedFixture.VecDir, "gdtest_empty", s.Qualified, "empty"));
        }
        finally { if (!ExtendedFixture.Keep) ExtendedFixture.Wipe(fx.Factory); }
    }

    [Fact]
    public void HighVertexPolygons_VertexCountAndSpatialPredicateExact()
    {
        if (!Ready()) return;
        using var fx = new ExtendedFixture();
        try
        {
            var s = Ext("gdtest_x_huge");
            Check.ThrowOnFail(Check.Cond(fx.EnsurePublished(s).Success, "IT/publish:huge", "ok", "发布失败"));
            Check.ThrowOnFail(RealDataFidelity.VertexCountParity(ExtendedFixture.VecDir, "gdtest_huge", s.Qualified, "huge"));
            Check.ThrowOnFail(RealDataFidelity.SpatialPredicatesMatch(ExtendedFixture.VecDir, "gdtest_huge", s.Qualified, "huge"));
            Check.ThrowOnFail(RealDataFidelity.ReprojectionConsistency(ExtendedFixture.VecDir, "gdtest_huge", s.Qualified, "huge"));
        }
        finally { if (!ExtendedFixture.Keep) ExtendedFixture.Wipe(fx.Factory); }
    }

    [Fact]
    public void SpatialLiteralAxisOrder_PinnedAsServerContract()
    {
        if (!Ready()) return;
        using var fx = new ExtendedFixture();
        try
        {
            var s = Ext("gdtest_x_huge");
            Check.ThrowOnFail(Check.Cond(fx.EnsurePublished(s).Success, "IT/publish:huge", "ok", "发布失败"));
            var r = RealDataFidelity.SpatialLiteralAxisOrderContract(ExtendedFixture.VecDir, "gdtest_huge", s.Qualified, "huge");
            // 契约项本身允许 Pass/Warn（服务端轴序行为），但不得是 Fail
            Assert.NotEqual(CheckStatus.Fail, r.Status);
        }
        finally { if (!ExtendedFixture.Keep) ExtendedFixture.Wipe(fx.Factory); }
    }

    [Fact]
    public void CjkLayerName_FullChainEscaping()
    {
        if (!Ready()) return;
        using var fx = new ExtendedFixture();
        try
        {
            var s = Ext("湖泊 与 水库");
            Check.ThrowOnFail(Check.Cond(fx.EnsurePublished(s).Success, "IT/publish:cjk", "ok", "发布失败"));
            Check.ThrowOnFail(RealDataFidelity.CjkNameChain(ExtendedFixture.VecDir, "湖泊 与 水库", s.Qualified, "cjk-layer"));
        }
        finally { if (!ExtendedFixture.Keep) ExtendedFixture.Wipe(fx.Factory); }
    }

    [Fact]
    public void LargeTable_PagingCompletenessAndIdempotency()
    {
        if (!Ready()) return;
        using var fx = new ExtendedFixture();
        try
        {
            var s = Ext("gdtest_x_pts20k");
            Check.ThrowOnFail(Check.Cond(fx.EnsurePublished(s).Success, "IT/publish:pts20k", "ok", "发布失败"));
            Check.ThrowOnFail(RealDataFidelity.PagingConsistency(ExtendedFixture.VolDir, "gdtest_pts20k", s.Qualified, "pts20k"));
            int total = DbfHeader.Parse(Path.Combine(ExtendedFixture.VolDir, "gdtest_pts20k.dbf")).RecordCount;
            var timing = RealDataFidelity.TimingBaseline(s.Qualified, total, "pts20k");
            Check.ThrowOnFail(timing);

            // 幂等复核：重复发布不得新增资源、结果必须一致
            var again = fx.EnsurePublished(s);
            Check.ThrowOnFail(Check.Cond(again.Success, "IT/idempotent:pts20k", "重复发布仍成功", again.Message));
            var fts = OgcProbe.Get(TestEnv.RestBase + "/rest/workspaces/" + ExtendedFixture.Ws
                + "/datastores/" + ExtendedFixture.StoreFor("vol") + "/featuretypes.json");
            int n = System.Text.RegularExpressions.Regex.Matches(fts.Text ?? "", "\"name\"").Count;
            Check.ThrowOnFail(Check.Cond(n == 1, "IT/idempotent:single-ft", "存储内仍只有 1 个要素类型", "要素类型数=" + n));
        }
        finally { if (!ExtendedFixture.Keep) ExtendedFixture.Wipe(fx.Factory); }
    }

    [Fact]
    public void CorruptedShapefiles_AreRejectedWithDiagnosableReason()
    {
        if (!Ready()) return;
        using var fx = new ExtendedFixture();
        try
        {
            foreach (var s in ExtendedFixture.Sets().Where(x => x.ExpectFailure))
                Check.ThrowOnFail(RealDataFidelity.DiagnosableFailure(fx, s, s.BaseName));
            foreach (var s in ExtendedFixture.Sets().Where(x => x.BaseName == "bd_valid"))
                Check.ThrowOnFail(RealDataFidelity.PositiveControlInDirtyDir(fx, s, "bd_valid"));
            Check.ThrowOnFail(RealDataFidelity.MissingPrjSurfaced(ExtendedFixture.BadDir, "bd_noprj", "noprj"));
        }
        finally { if (!ExtendedFixture.Keep) ExtendedFixture.Wipe(fx.Factory); }
    }

    [Fact]
    public void RasterDatasets_PixelsBandsAndNodataFidelity()
    {
        if (!Ready()) return;
        using var fx = new ExtendedFixture();
        try
        {
            foreach (var s in ExtendedFixture.Sets().Where(x => x.IsCoverage))
            {
                var r = fx.EnsurePublished(s);
                Check.ThrowOnFail(Check.Cond(r.Success, "IT/publish:" + s.LayerName, "ok", r.Message));
                if (!r.Success) continue;
                var tif = Path.Combine(s.HostDir, s.BaseName);
                Check.ThrowOnFail(RealDataRaster.SourceMatchesFormula(tif, s.LayerName,
                    (px, py, b) => RasterFormula(s.BaseName, px, py, b)));
                Check.ThrowOnFail(RealDataRaster.CoverageRestMetadata(tif, ExtendedFixture.Ws,
                    ExtendedFixture.CoverageStoreFor(s), s.LayerName, s.LayerName));
                Check.ThrowOnFail(RealDataRaster.WcsDescribeMatchesSource(tif, s.Qualified, s.LayerName));
                Check.ThrowOnFail(RealDataRaster.WcsPixelsMatchSource(tif, s.Qualified, s.LayerName));
            }
        }
        finally { if (!ExtendedFixture.Keep) ExtendedFixture.Wipe(fx.Factory); }
    }

    private static ExtSet Ext(string layerName) =>
        ExtendedFixture.Sets().First(s => s.LayerName == layerName);

    private static double RasterFormula(string tifName, int px, int py, int band)
    {
        if (tifName == "gdtest_rgb.tif")
        {
            if (band == 1) return (px * 3 + py) % 251 + 1;
            if (band == 2) return (px + py * 5) % 241 + 10;
            if (px < 8 && py < 6) return 0;
            return (px * py) % 200 + 50;
        }
        if (tifName == "gdtest_int16.tif") return px < 10 && py < 10 ? -9999 : (px * py) % 3000 - 500;
        return (px * 2 + py * 3) % 256;
    }
}
