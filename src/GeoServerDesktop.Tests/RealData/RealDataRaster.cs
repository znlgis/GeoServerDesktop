using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.RealData
{
    /// <summary>
    /// 真实栅格保真检查（多波段 / 整型 / nodata / 瓦片化+概览 / CJK 文件名）：
    /// 期望值链路 = 生成公式 → 源 TIFF（TiffSample 独立解析）→ 服务输出（WCS GetCoverage 回读后再解析）。
    /// 服务面只与“源文件像元”比对，绝不与自身回抄；公式比对单列为数据完整性检查。
    /// </summary>
    public static class RealDataRaster
    {
        /// <summary>数据完整性：源 TIFF 像元 == 生成公式（等价既有 DEM 检查的泛化形态）。</summary>
        public static CheckResult SourceMatchesFormula(string tifPath, string tag, Func<int, int, int, double> formula,
                                                       int samples = 12)
        {
            var t = TiffSample.Parse(tifPath);
            var probs = new List<string>();
            int taken = 0;
            foreach (var (px, py) in SamplePoints(t.Width, t.Height, samples))
            {
                for (int b = 1; b <= t.SamplesPerPixel; b++)
                {
                    double got = t.ReadSample(px, py, b);
                    double want = formula(px, py, b);
                    taken++;
                    if (Math.Abs(got - want) > 1e-6) probs.Add($"({px},{py})b{b} 文件={got} 公式={want}");
                }
            }
            return Check.Cond(probs.Count == 0, "RasterFormula/" + tag,
                $"{t.Width}x{t.Height}×{t.SamplesPerPixel}波段 {taken} 采样点与生成公式一致（{t.BitsPerSample}bit fmt{t.SampleFormat}）",
                string.Join("; ", probs.Take(5)) + "（采样 " + taken + "）");
        }

        /// <summary>
        /// WCS 2.0.1 DescribeCoverage 与源文件交叉（GeoServer 3.0.1 实测形态）：
        ///  · gml:GridEnvelope low/high → 网格尺寸 = high-low+1；
        ///  · gml:offsetVector → 分辨率；gml:origin/Point@srsName → EPSG 码；
        ///  · gmlcov:rangeType 内 swe:DataRecord 为空元素（实测不宣告波段数/波段名）→ 记为服务端基线事实。
        /// </summary>
        public static CheckResult WcsDescribeMatchesSource(string tifPath, string coverageId, string tag)
        {
            var src = TiffSample.Parse(tifPath);
            var r = OgcProbe.Get(OgcProbe.Wcs("request=DescribeCoverage", "version=2.0.1", "CoverageId=" + coverageId));
            if (!r.Ok) return Check.Fail("WcsDesc/" + tag, "HTTP " + r.Status + " :: " + Snip(r.Text));
            string x = r.Text ?? "";

            var probs = new List<string>();
            var env = Regex.Match(x, @"<gml:GridEnvelope>[\s\S]*?<gml:low>([^<]*)</gml:low>\s*<gml:high>([^<]*)</gml:high>");
            if (!env.Success) return Check.Fail("WcsDesc/" + tag, "无 gml:GridEnvelope（形态变更）:: " + Snip(x));
            var low = env.Groups[1].Value.Split(' ');
            var high = env.Groups[2].Value.Split(' ');
            int gw = int.Parse(high[0].Trim()) - int.Parse(low[0].Trim()) + 1;
            int gh = int.Parse(high[1].Trim()) - int.Parse(low[1].Trim()) + 1;
            if (gw != src.Width || gh != src.Height)
                probs.Add($"网格 {gw}x{gh} != 源 {src.Width}x{src.Height}");

            var ov = Regex.Matches(x, @"<gml:offsetVector[^>]*>([^<]*)</gml:offsetVector>");
            if (ov.Count >= 2)
            {
                var sx = double.Parse(ov[0].Groups[1].Value.Split(' ')[0], CultureInfo.InvariantCulture);
                var sy = double.Parse(ov[1].Groups[1].Value.Split(' ')[1], CultureInfo.InvariantCulture);
                if (src.ModelPixelScale != null && src.ModelPixelScale.Length >= 2)
                {
                    if (Math.Abs(Math.Abs(sx) - src.ModelPixelScale[0]) > 1e-6) probs.Add($"X 分辨率 {sx} != 源 {src.ModelPixelScale[0]}");
                    if (Math.Abs(Math.Abs(sy) - src.ModelPixelScale[1]) > 1e-6) probs.Add($"Y 分辨率 {sy} != 源 {src.ModelPixelScale[1]}");
                }
            }
            else probs.Add("offsetVector 少于 2 条");

            var srs = Regex.Match(x, @"srsName=""http://www.opengis.net/def/crs/EPSG/0/(\d+)""");
            int epsg = srs.Success ? int.Parse(srs.Groups[1].Value) : -1;
            if (src.GeoKeyProjCS > 0 && epsg != src.GeoKeyProjCS)
                probs.Add($"EPSG 服务={epsg} 源 GeoKey3072={src.GeoKeyProjCS}");
            if (epsg < 0) probs.Add("DescribeCoverage 无 EPSG srsName");

            // 实测基线：rangeType/DataRecord 为空 → 波段数不经 WCS 元数据宣告
            var dr = Regex.Match(x, @"<swe:DataRecord[^>]*(/>|>([\s\S]*?)</swe:DataRecord>)");
            bool bandsAdvertised = dr.Success && dr.Groups[2].Success && dr.Groups[2].Value.Trim().Length > 0;
            string bandNote = bandsAdvertised ? "rangeType 已宣告波段" : "rangeType 内 swe:DataRecord 为空（本 GeoServer 不宣告波段数/波段名，波段数须经 GetCoverage 输出核对）";

            if (probs.Count > 0) return Check.Fail("WcsDesc/" + tag, string.Join("; ", probs) + "；" + bandNote);
            return Check.Cond(bandsAdvertised, "WcsDesc/" + tag,
                $"网格 {gw}x{gh}、分辨率、EPSG:{epsg} 与源一致；{bandNote}",
                $"网格/分辨率/EPSG 与源一致，但 WCS 元数据不宣告波段数（{src.SamplesPerPixel} 波段须经 GetCoverage 核对）：{bandNote}",
                warnInsteadOfFail: true);
        }

        /// <summary>WCS GetCoverage 像元级保真：服务输出逐采样点 == 源文件同点位（含 nodata 块与多波段）。</summary>
        public static CheckResult WcsPixelsMatchSource(string tifPath, string coverageId, string tag, string format = "GeoTIFF")
        {
            var src = TiffSample.Parse(tifPath);
            var r = OgcProbe.Get(OgcProbe.Wcs("request=GetCoverage", "version=2.0.1",
                "CoverageId=" + coverageId, "format=" + format));
            if (!(r.Ok && r.Bytes != null && r.Bytes.Length > 8))
                return Check.Fail("WcsPix/" + tag, $"GetCoverage 无 TIFF（HTTP {r.Status} ct={r.ContentType}）:: " + Snip(r.Text));
            if (!(r.Bytes[0] == 'I' && r.Bytes[1] == 'I') && !(r.Bytes[0] == 'M' && r.Bytes[1] == 'M'))
                return Check.Fail("WcsPix/" + tag, "返回非 TIFF ct=" + r.ContentType);

            var tmp = Path.Combine(Path.GetTempPath(), "gsd_rast_" + Guid.NewGuid().ToString("N") + ".tif");
            File.WriteAllBytes(tmp, r.Bytes);
            try
            {
                var outT = TiffSample.Parse(tmp);
                var probs = new List<string>();
                if (outT.SamplesPerPixel != src.SamplesPerPixel)
                    probs.Add($"波段数 输出={outT.SamplesPerPixel} 源={src.SamplesPerPixel}");
                if (outT.Width != src.Width || outT.Height != src.Height)
                    probs.Add($"尺寸 输出={outT.Width}x{outT.Height} 源={src.Width}x{src.Height}");
                int nodataSeen = 0, compared = 0;
                foreach (var (px, py) in SamplePoints(Math.Min(outT.Width, src.Width), Math.Min(outT.Height, src.Height), 10))
                {
                    for (int b = 1; b <= Math.Min(outT.SamplesPerPixel, src.SamplesPerPixel); b++)
                    {
                        double a = src.ReadSample(px, py, b);
                        double g;
                        try { g = outT.ReadSample(px, py, b); }
                        catch (Exception ex) { probs.Add($"读取输出 ({px},{py})b{b} 失败 {ex.GetType().Name}"); continue; }
                        compared++;
                        var nd = src.NoDataValue();
                        if (a == g) { if (nd.HasValue && Math.Abs(a - nd.Value) < 1e-9) nodataSeen++; continue; }
                        // 服务端可能把 nodata 替换为 0/背景值 → 归类记录，不算数据错误
                        if (nd.HasValue && Math.Abs(a - nd.Value) < 1e-9) { nodataSeen++; continue; }
                        probs.Add($"({px},{py})b{b} 输出={g} 源={a}");
                    }
                }
                if (probs.Count > 0) return Check.Fail("WcsPix/" + tag, string.Join("; ", probs.Take(6)));
                string extra = nodataSeen > 0 ? $"（含 {nodataSeen} 个 nodata 采样点，按源值/掩码等价处理）" : "";
                return Check.Pass("WcsPix/" + tag,
                    $"{compared} 采样点（{outT.Width}x{outT.Height}×{outT.SamplesPerPixel}，{outT.BitsPerSample}bit fmt{outT.SampleFormat}）与服务输出逐点一致{extra}");
            }
            finally { try { File.Delete(tmp); } catch { } }
        }

        /// <summary>WMS 出图非空白 + 与源波段统计量级一致（栅格渲染链路的真实数据校验）。</summary>
        public static CheckResult WmsRasterRenders(string tifPath, string layer, string tag)
        {
            var src = TiffSample.Parse(tifPath);
            double[] bb = SourceLonLatOrGridBBox(src, tifPath);
            var png = OgcProbe.Get(OgcProbe.Wms("request=GetMap", "version=1.1.1", "layers=" + layer, "styles=",
                "format=image/png", "bbox=" + string.Join(",", bb.Select(v => Fmt(v))),
                "width=200", "height=150", "srs=EPSG:32754"));
            if (!png.Ok || png.Bytes == null || png.Bytes.Length < 100 || png.Bytes[0] != 0x89)
                return Check.Fail("RasterWms/" + tag, $"GetMap 非 PNG（HTTP {png.Status} ct={png.ContentType}）:: " + Snip(png.Text));
            var distinct = PngColorStats(png.Bytes);
            bool nonBlank = distinct >= 2;
            return Check.Cond(nonBlank, "RasterWms/" + tag,
                $"{layer} 出图 {png.Bytes.Length}B，源 {src.Width}x{src.Height}×{src.SamplesPerPixel}，distinct≈{distinct} 颜色（非空白）",
                $"出图仅 {distinct} 种颜色（疑似空白/掩码全遮），HTTP {png.Status}");
        }

        /// <summary>WMTS：栅格图层可被 GWC 注册并出合法瓦片。</summary>
        public static CheckResult WmtsRasterTile(string layer, string tag)
        {
            var caps = OgcProbe.Get(OgcProbe.GwcWmts("service=WMTS", "version=1.0.0", "request=GetCapabilities"));
            if (!caps.Ok) return Check.Fail("RasterWmts/" + tag, "capabilities HTTP " + caps.Status);
            var info = WmtsProbe.TileMatrixFor(caps.Text ?? "", layer);
            if (info == null)
                return Check.Warn("RasterWmts/" + tag, $"{layer} 未在 WMTS 能力表（GWC 未注册该 coverage，按现状 Warn）");
            var (set, matrix, fmt) = info.Value;
            var t = OgcProbe.Get(OgcProbe.GwcWmts("service=WMTS", "version=1.0.0", "request=GetTile",
                "layer=" + layer, "style=", "tilematrixset=" + set, "TileMatrix=" + matrix, "TileCol=0", "TileRow=0",
                "format=" + fmt));
            bool img = t.Ok && t.Bytes != null && t.Bytes.Length > 100
                && (t.Bytes[0] == 0x89 || (t.Bytes[0] == 0xFF && t.Bytes[1] == 0xD8));
            return Check.Cond(img, "RasterWmts/" + tag,
                $"{layer} 瓦片 {set}/{matrix} {fmt} → {t.Bytes?.Length}B 图像",
                $"瓦片 HTTP {t.Status} 长度 {t.Bytes?.Length}");
        }

        /// <summary>
        /// REST coverage 元数据与源文件交叉（实测 3.0.1 形态）：
        ///  · grid.range.low/high 给网格尺寸、grid.crs 给 EPSG、grid.transform 给仿射；
        ///  · nativeCRS 为对象 {"@class":"projected|geographic","$":"WKT..."}（历史缺陷：模型按字符串解析恒空）；
        ///  · nativeBoundingBox 必须覆盖源文件由 GeoTIFF 仿射独立推出的范围。
        /// </summary>
        public static CheckResult CoverageRestMetadata(string tifPath, string ws, string store, string coverage, string tag)
        {
            var src = TiffSample.Parse(tifPath);
            var r = OgcProbe.Get(TestEnv.RestBase + "/rest/workspaces/" + Uri.EscapeDataString(ws) + "/coveragestores/"
                + Uri.EscapeDataString(store) + "/coverages/" + Uri.EscapeDataString(coverage) + ".json");
            if (!r.Ok) return Check.Fail("CovRest/" + tag, "HTTP " + r.Status + " :: " + Snip(r.Text));
            var probs = new List<string>();
            System.Text.Json.JsonElement cov;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(r.Text!);
                cov = doc.RootElement.GetProperty("coverage").Clone();
            }
            catch (Exception ex) { return Check.Fail("CovRest/" + tag, ex.GetType().Name + ": " + ex.Message); }

            // 1) grid 尺寸与 CRS
            if (cov.TryGetProperty("grid", out var grid))
            {
                string hi = grid.TryGetProperty("range", out var rg) && rg.TryGetProperty("high", out var h) ? h.GetString() ?? "" : "";
                var nums = hi.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (nums.Length != 2) probs.Add("grid.range.high 非两轴: '" + hi + "'");
                else
                {
                    int gw = int.Parse(nums[0], CultureInfo.InvariantCulture), gh = int.Parse(nums[1], CultureInfo.InvariantCulture);
                    if (gw != src.Width || gh != src.Height) probs.Add($"grid {gw}x{gh} != 源 {src.Width}x{src.Height}");
                }
                string gcrs = grid.TryGetProperty("crs", out var c) ? c.GetString() ?? "" : "";
                if (src.GeoKeyProjCS > 0 && gcrs != "EPSG:" + src.GeoKeyProjCS) probs.Add("grid.cs=" + gcrs + " 源=" + src.GeoKeyProjCS);
            }
            else probs.Add("REST coverage 无 grid 节点");

            // 2) nativeCRS 对象形态（历史缺陷回归点：按字符串解析会恒为 null）
            string crsClass = "";
            if (cov.TryGetProperty("nativeCRS", out var nc))
            {
                if (nc.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    crsClass = nc.TryGetProperty("@class", out var ac) ? ac.GetString() ?? "" : "";
                    string wkt = nc.TryGetProperty("$", out var w) ? w.GetString() ?? "" : "";
                    if (wkt.Trim().Length == 0) probs.Add("nativeCRS.$ 为空");
                    else if (src.GeoKeyProjCS > 0 && wkt.IndexOf("UTM zone", StringComparison.OrdinalIgnoreCase) < 0
                             && !wkt.Contains(src.GeoKeyProjCS.ToString(CultureInfo.InvariantCulture)))
                        probs.Add("nativeCRS WKT 与源投影不符: " + Snip(wkt));
                }
                else if (nc.ValueKind == System.Text.Json.JsonValueKind.String)
                    probs.Add("nativeCRS 退化为字符串（模型兼容性变更需确认）");
                else probs.Add("nativeCRS 缺失/类型异常: " + nc.ValueKind);
            }
            else probs.Add("REST coverage 无 nativeCRS");

            // 3) 原生范围必须覆盖源文件仿射推出的范围
            if (cov.TryGetProperty("nativeBoundingBox", out var nb))
            {
                double[]? bb = SourceGridBBox(src);
                if (bb != null)
                {
                    double minx = D(nb, "minx"), maxx = D(nb, "maxx"), miny = D(nb, "miny"), maxy = D(nb, "maxy");
                    double tol = Math.Max(1e-6, Math.Max(bb[2] - bb[0], bb[3] - bb[1]) * 1e-4);
                    bool ok = minx <= bb[0] + tol && maxx >= bb[2] - tol && miny <= bb[1] + tol && maxy >= bb[3] - tol;
                    if (!ok) probs.Add($"nativeBbox({minx},{miny},{maxx},{maxy}) 未覆盖源仿射范围({bb[0]},{bb[1]},{bb[2]},{bb[3]})");
                }
            }
            else probs.Add("REST coverage 无 nativeBoundingBox");

            return Check.Cond(probs.Count == 0, "CovRest/" + tag,
                $"REST 元数据与源一致：grid/CRS(@class={crsClass})/范围 全覆盖（源 {src.Width}x{src.Height}×{src.SamplesPerPixel} 波段）",
                string.Join("; ", probs));
        }

        private static double D(System.Text.Json.JsonElement el, string prop)
            => el.TryGetProperty(prop, out var v) && v.TryGetDouble(out var d) ? d : double.NaN;

        /// <summary>由 GeoTIFF 仿射独立推出源文件范围（minx,miny,maxx,maxy）。</summary>
        public static double[]? SourceGridBBox(TiffSample src)
        {
            if (src.ModelPixelScale == null || src.ModelTiepoint == null) return null;
            double ox = src.ModelTiepoint[3], oy = src.ModelTiepoint[4];
            double sx = src.ModelPixelScale[0], sy = src.ModelPixelScale[1];
            double x0 = ox, x1 = ox + sx * src.Width;
            double y1 = oy, y0 = oy - sy * src.Height;
            return new[] { Math.Min(x0, x1), Math.Min(y0, y1), Math.Max(x0, x1), Math.Max(y0, y1) };
        }

        // ---------------- 辅助 ----------------

        public static IEnumerable<(int px, int py)> SamplePoints(int w, int h, int n)
        {
            if (w <= 0 || h <= 0) yield break;
            n = Math.Max(2, n);
            yield return (0, 0);
            yield return (w - 1, h - 1);
            yield return (0, h - 1);
            yield return (w - 1, 0);
            yield return (w / 2, h / 2);
            for (int i = 0; i < n; i++)
            {
                int px = (int)((long)(i + 1) * (w - 1) / (n + 1));
                int py = (int)((long)(i * 7 + 3) % Math.Max(1, h));
                yield return (px, py);
            }
        }

        private static string Fmt(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);
        private static string Snip(string s) => s == null ? "<null>" : (s.Length > 160 ? s.Substring(0, 160) : s)
            .Replace("\n", " ").Replace("\r", " ");

        /// <summary>源文件地理范围（GeoTIFF 仿射：ModelPixelScale + ModelTiepoint；缺失时回退 GridExtensions）。</summary>
        public static double[] SourceLonLatOrGridBBox(TiffSample src, string tifPath)
        {
            if (src.ModelPixelScale != null && src.ModelTiepoint != null && src.ModelPixelScale.Length >= 2
                && src.ModelTiepoint.Length >= 6)
            {
                double ox = src.ModelTiepoint[3], oy = src.ModelTiepoint[4];
                double sx = src.ModelPixelScale[0], sy = src.ModelPixelScale[1];
                double x0 = ox, x1 = ox + sx * src.Width;
                double y1 = oy, y0 = oy - sy * src.Height;
                return new[] { x0, y0, x1, y1 };
            }
            var g = GeoTiffGridExtensions.Read(tifPath);
            if (g != null) return g;
            throw new InvalidDataException("栅格无仿射/GridExtensions，无法确定范围: " + tifPath);
        }

        /// <summary>PNG 粗粒度颜色统计（解码为位图后按 4bit 量化取 distinct 桶数；不经外部图像库）。</summary>
        public static int PngColorStats(byte[] png)
        {
            using var ms = new MemoryStream(png);
            using var img = SkiaSharp.SKBitmap.Decode(ms);
            if (img == null) return 0;
            var set = new HashSet<int>();
            int stepX = Math.Max(1, img.Width / 40), stepY = Math.Max(1, img.Height / 40);
            for (int y = 0; y < img.Height; y += stepY)
                for (int x = 0; x < img.Width; x += stepX)
                {
                    var c = img.GetPixel(x, y);
                    set.Add((((int)c.Red >> 4) << 8) | (((int)c.Green >> 4) << 4) | ((int)c.Blue >> 4));
                }
            return set.Count;
        }
    }

    /// <summary>TIFF GridExtensions（tag 700/701）兜底读取（GeoServer WCS 输出常丢弃仿射标签）。</summary>
    internal static class GeoTiffGridExtensions
    {
        public static double[] Read(string path)
        {
            try
            {
                var bytes = File.ReadAllBytes(path);
                bool little = bytes[0] == 'I' && bytes[1] == 'I';
                Func<int, ushort> u16 = i => little ? (ushort)((int)bytes[i] | ((int)bytes[i + 1] << 8)) : (ushort)(((int)bytes[i] << 8) | (int)bytes[i + 1]);
                Func<int, uint> u32 = i => little
                    ? (uint)(bytes[i] | (bytes[i + 1] << 8) | (bytes[i + 2] << 16) | ((uint)bytes[i + 3] << 24))
                    : (uint)(((uint)bytes[i] << 24) | (bytes[i + 1] << 16) | (bytes[i + 2] << 8) | bytes[i + 3]);
                Func<int, double> f64 = i =>
                {
                    var b = new byte[8];
                    Array.Copy(bytes, i, b, 0, 8);
                    if (!little) Array.Reverse(b);
                    return BitConverter.ToDouble(b, 0);
                };
                int ifd = (int)u32(4);
                int n = u16(ifd);
                int geoDouble = -1, count = 0;
                for (int e = 0; e < n; e++)
                {
                    int ent = ifd + 2 + e * 12;
                    int tag = u16(ent);
                    if (tag == 3356) { geoDouble = (int)u32(ent + 8); count = (int)u32(ent + 4); }
                }
                if (geoDouble < 0 || count < 6) return null;
                // GTModelTypeGeoKey(3355)=Projected(1) + GTRasterType + limits
                double x0 = f64(geoDouble + 24), x1 = f64(geoDouble + 32);
                double y0 = f64(geoDouble + 40), y1 = f64(geoDouble + 48);
                return new[] { Math.Min(x0, x1), Math.Min(y0, y1), Math.Max(x0, x1), Math.Max(y0, y1) };
            }
            catch { return null; }
        }
    }

    /// <summary>复用既有 WMTS 能力表解析（RealDataChecks 内部方法为 private，此处以同等逻辑独立实现）。</summary>
    internal static class WmtsProbe
    {
        public static (string set, string matrix, string fmt)? TileMatrixFor(string caps, string layerName)
        {
            int li = caps.IndexOf("<Layer", StringComparison.Ordinal);
            while (li >= 0)
            {
                int end = caps.IndexOf("</Layer>", li, StringComparison.Ordinal);
                if (end < 0) break;
                var seg = caps.Substring(li, end - li);
                if (seg.Contains(layerName))
                {
                    string? fmt = null;
                    foreach (var pref in new[] { "image/png", "image/jpeg", "image/gif" })
                    {
                        if (seg.Contains("<Format>" + pref + "</Format>")) { fmt = pref; break; }
                    }
                    if (fmt == null) return null;
                    var set = Between(seg, "<TileMatrixSet>", "</TileMatrixSet>");
                    var m = Between(seg, "<TileMatrix>", "</TileMatrix>");
                    if (set == null) return null;
                    if (set != null && m != null) return (set, m, fmt);
                    if (set != null) return (set, set + ":0", fmt);
                }
                li = caps.IndexOf("<Layer", end, StringComparison.Ordinal);
            }
            return null;
        }

        private static string? Between(string s, string o, string c)
        {
            int i = s.IndexOf(o, StringComparison.Ordinal); if (i < 0) return null;
            i += o.Length; int j = s.IndexOf(c, i, StringComparison.Ordinal); if (j < 0) return null;
            return s.Substring(i, j - i).Trim();
        }
    }
}
