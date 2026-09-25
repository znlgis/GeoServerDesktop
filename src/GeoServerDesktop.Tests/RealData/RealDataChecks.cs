using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using GeoServerDesktop.Tests.Infrastructure;
using GeoServerDesktop.Tests.RealData;
using SkiaSharp;

namespace GeoServerDesktop.Tests.RealData
{
    /// <summary>
    /// Layer 3 服务面“纯检查逻辑”：每个方法返回 <see cref="CheckResult"/>（内部经 Check.* 记入汇总，
    /// 供 xunit ThrowOnFail 与控制台 harness 双形态复用）。期望值一律从数据文件头/DBF/SHP/manifest 或
    /// 确定性公式推导，绝不从被测 GeoServer 返回值回抄。
    /// </summary>
    public static class RealDataChecks
    {
        // 显式容差常量（依据见注释）
        public const double ToleranceGeometry = 1e-9;     // 经纬度几何锚定：浮点/序列化误差上界（度）
        public const double ToleranceAreaRel = 1e-6;      // 跨服务面面积：GeoTools 序列化 + 环顶点浮点累积相对误差
        public const double TolerancePixel = 1e-3;        // Float32 像元：源与 WCS 输出同为 float32，编码往返误差远小于此
        public const double ToleranceGeoRel = 1e-9;       // GeoTIFF 仿射标签相对误差

        private static readonly string Ws = E2ePublishHelper.Ws;

        // ============ 数据侧：期望值推导 ============
        public static List<Dictionary<string, string>> PolyExpected(string dataDir)
            => DbfRecords.Read(Path.Combine(dataDir, "gdtest_poly.dbf"));
        public static List<Dictionary<string, string>> LinesExpected(string dataDir)
            => DbfRecords.Read(Path.Combine(dataDir, "gdtest_lines.dbf"));

        private static string Col(List<Dictionary<string, string>> rows, int i, string c)
            => rows[i].TryGetValue(c, out var v) ? v.Trim() : null;

        // ============ WFS ============
        public static CheckResult WfsCapabilitiesHasTypes(string polyLayer, string linesLayer)
        {
            var r = OgcProbe.Get(OgcProbe.Wfs("request=GetCapabilities", "service=WFS", "version=2.0.0"));
            var t = r.Text ?? "";
            bool hasP = t.Contains(polyLayer) && t.Contains(Ws);
            bool hasL = t.Contains(linesLayer);
            return Check.Cond(hasP && hasL, "WfsGetCapabilities/typenames",
                $"含 {polyLayer}+{linesLayer}",
                $"缺 typename：poly={hasP} lines={hasL}（HTTP {r.Status}）");
        }

        public static CheckResult WfsHitsCount(string layer, int expected, string tag)
        {
            var url = OgcProbe.Wfs("request=GetFeature", "version=2.0.0", "resultType=hits",
                "outputFormat=application/json", "typeName=" + layer, "count=1");
            var r = OgcProbe.Get(url);
            var matched = ExtractNumberMatched(r.Text);
            return Check.Cond(matched == expected, "WfsHits/" + tag,
                $"numberMatched={matched}=={expected}",
                $"numberMatched={matched} 期望 {expected}（HTTP {r.Status}）");
        }

        private static int ExtractNumberMatched(string text)
        {
            if (text == null) return -1;
            var m = System.Text.RegularExpressions.Regex.Match(text, "numberMatched[\"':=\\s]+(\\d+)");
            return m.Success && int.TryParse(m.Groups[1].Value, out var v) ? v : -1;
        }

        public static CheckResult WfsAttributesMatchFileHeader(string dataDir, string layer, string tag)
        {
            var url = OgcProbe.Wfs("request=GetFeature", "version=2.0.0",
                "outputFormat=application/json", "typeName=" + layer);
            var r = OgcProbe.Get(url);
            if (!r.Ok) return Check.Fail("WfsAttrs/" + tag, "HTTP " + r.Status);
            if (!LooksJson(r.Text)) return Check.Fail("WfsAttrs/" + tag,
                "WFS 非 JSON（可能命名空间/存储不可用）：" + Snip(r.Text));

            string stem = tag.Contains("line") ? "lines" : "poly";
            var dbf = DbfHeader.Parse(Path.Combine(dataDir, "gdtest_" + stem + ".dbf"));
            var shpCount = ShapefileHeader.CountRecords(Path.Combine(dataDir, "gdtest_" + stem + ".shp"));
            var rows = DbfRecords.Read(Path.Combine(dataDir, "gdtest_" + stem + ".dbf"));
            bool hasId = dbf.Fields.Any(f => f.Name.Equals("ID", StringComparison.OrdinalIgnoreCase));
            bool hasVal = dbf.Fields.Any(f => f.Name.Equals("VALUE", StringComparison.OrdinalIgnoreCase));
            var expected = new Dictionary<string, (int id, double val, bool hasId, bool hasVal)>(StringComparer.Ordinal);
            for (int i = 0; i < rows.Count; i++)
            {
                var nm = Col(rows, i, "NAME");
                expected[nm] = (hasId ? DbfRecords.GetInt(rows, "ID", i) : 0,
                                hasVal ? DbfRecords.GetDouble(rows, "VALUE", i) : 0, hasId, hasVal);
            }

            var (featCount, byName, _) = ParseWfsFeatures(r.Text);
            var problems = new List<string>();
            if (featCount != expected.Count) problems.Add($"要素数 {featCount} != DBF {expected.Count}");
            if (shpCount != dbf.RecordCount) problems.Add($"SHP 记录 {shpCount} != DBF {dbf.RecordCount}");
            if (featCount != shpCount) problems.Add($"服务要素 {featCount} != SHP {shpCount}");
            foreach (var kv in expected)
            {
                if (!byName.TryGetValue(kv.Key, out var got)) { problems.Add("缺要素 " + kv.Key); continue; }
                if (kv.Value.hasId && got.id != kv.Value.id) problems.Add($"{kv.Key} ID {got.id}!={kv.Value.id}");
                if (kv.Value.hasVal && Math.Abs(got.val - kv.Value.val) > 1e-9) problems.Add($"{kv.Key} VALUE {got.val}!={kv.Value.val}");
            }
            foreach (var k in byName.Keys) if (!expected.ContainsKey(k)) problems.Add("服务多出 " + k);

            return Check.Cond(problems.Count == 0, "WfsAttrs/" + tag,
                $"{featCount} 要素 NAME/ID/VALUE 与 DBF 一致；SHP=DBF={shpCount}",
                string.Join("; ", problems.Take(6)) + "  (count=" + featCount + ")");
        }

        public static CheckResult WfsGeometryBboxMatchesShpHeader(string dataDir, string layer, string shapefile, string tag)
        {
            var hdr = ShapefileHeader.Parse(Path.Combine(dataDir, shapefile));
            var url = OgcProbe.Wfs("request=GetFeature", "version=2.0.0",
                "outputFormat=application/json", "typeName=" + layer);
            var r = OgcProbe.Get(url);
            var (_, _, bb) = ParseWfsFeatures(r.Text);
            bool ok = Math.Abs(bb[0] - hdr.XMin) <= ToleranceGeometry && Math.Abs(bb[1] - hdr.YMin) <= ToleranceGeometry
                   && Math.Abs(bb[2] - hdr.XMax) <= ToleranceGeometry && Math.Abs(bb[3] - hdr.YMax) <= ToleranceGeometry;
            return Check.Cond(ok, "WfsGeomBbox/" + tag,
                $"服务并集bbox≈SHP头 ({hdr.XMin},{hdr.YMin},{hdr.XMax},{hdr.YMax})",
                $"服务并集bbox=({bb[0]:F4},{bb[1]:F4},{bb[2]:F4},{bb[3]:F4}) vs SHP头 ({hdr.XMin},{hdr.YMin},{hdr.XMax},{hdr.YMax})");
        }

        /// <summary>几何结构锚定：poly_10 双部件、poly_11 带洞——用 SHP 环推导期望，比对服务侧 MultiPolygon 结构。</summary>
        public static CheckResult WfsGeometryStructure(string dataDir, string layer, string tag)
        {
            var shpPath = Path.Combine(dataDir, "gdtest_poly.shp");
            var recs = ShapefileRecords.ReadAll(shpPath);
            var names = DbfRecords.Read(Path.Combine(dataDir, "gdtest_poly.dbf"));
            var expParts = new Dictionary<string, (int parts, int holes)>();
            for (int i = 0; i < recs.Count && i < names.Count; i++)
            {
                var (p, h, _) = ShapefileRecords.Classify(recs[i]);
                expParts[Col(names, i, "NAME")] = (p, h);
            }
            var url = OgcProbe.Wfs("request=GetFeature", "version=2.0.0",
                "outputFormat=application/json", "typeName=" + layer);
            var r = OgcProbe.Get(url);
            var (_, byName, _) = ParseWfsFeatures(r.Text);
            var probs = new List<string>();
            foreach (var nm in new[] { "poly_10", "poly_11" })
            {
                if (!byName.TryGetValue(nm, out var g)) { probs.Add("缺 " + nm); continue; }
                var (srvParts, srvHoles) = ClassifyServerGeometry(g.geom);
                var e = expParts[nm];
                if (srvParts != e.parts) probs.Add($"{nm} 部件 {srvParts}!={e.parts}");
                if (srvHoles != e.holes) probs.Add($"{nm} 洞 {srvHoles}!={e.holes}");
            }
            return Check.Cond(probs.Count == 0, "WfsGeomStructure/" + tag,
                "poly_10 双部件 / poly_11 带洞 结构与服务一致",
                string.Join("; ", probs));
        }

        public static CheckResult WfsCqlIdGreaterThan(string dataDir, string layer, int threshold, string tag)
        {
            var rows = DbfRecords.Read(Path.Combine(dataDir, "gdtest_poly.dbf"));
            var expected = new SortedSet<string>();
            for (int i = 0; i < rows.Count; i++)
                if (DbfRecords.GetInt(rows, "ID", i) > threshold) expected.Add(Col(rows, i, "NAME"));
            var url = OgcProbe.Wfs("request=GetFeature", "version=2.0.0",
                "outputFormat=application/json", "typeName=" + layer,
                "CQL_FILTER=ID > " + threshold);
            var r = OgcProbe.Get(url);
            var (_, byName, _) = ParseWfsFeatures(r.Text);
            var got = new SortedSet<string>(byName.Keys);
            bool ok = got.SetEquals(expected);
            return Check.Cond(ok, "WfsCql/" + tag,
                "ID>" + threshold + " → " + string.Join(",", got) + "（恰为期望集）",
                $"服务={string.Join(",", got)} 期望={string.Join(",", expected)}");
        }

        public static CheckResult WfsBboxFilter(string dataDir, string layer, string tag)
        {
            // 期望：几何完全落在查询框 (0,5,1,6) 的记录（由 SHP 逐记录 bbox 推导，应为带洞的 poly_11）
            var shpPath = Path.Combine(dataDir, "gdtest_poly.shp");
            var recs = ShapefileRecords.ReadAll(shpPath);
            var names = DbfRecords.Read(Path.Combine(dataDir, "gdtest_poly.dbf"));
            double qx0 = 0, qy0 = 5, qx1 = 1, qy1 = 6;
            var expected = new SortedSet<string>();
            for (int i = 0; i < recs.Count && i < names.Count; i++)
            {
                var rec = recs[i];
                bool inside = rec.MinX >= qx0 - 1e-9 && rec.MaxX <= qx1 + 1e-9 && rec.MinY >= qy0 - 1e-9 && rec.MaxY <= qy1 + 1e-9;
                if (inside) expected.Add(Col(names, i, "NAME"));
            }
            // 关键基线：WFS 2.0 BBOX 不带 CRS 后缀时按 EPSG:4326 官方轴序(lat,lon)解释（实测），
            // 故显式追加 ",EPSG:4326" 强制经纬度(lon,lat) 轴序，方能命中框内记录。
            var url = OgcProbe.Wfs("request=GetFeature", "version=2.0.0",
                "outputFormat=application/json", "typeName=" + layer,
                "BBOX=0,5,1,6,EPSG:4326");
            var r = OgcProbe.Get(url);
            var (cnt, byName, _) = ParseWfsFeatures(r.Text);
            var got = new SortedSet<string>(byName.Keys);
            bool ok = got.SetEquals(expected) && cnt == expected.Count;
            return Check.Cond(ok, "WfsBbox/" + tag,
                $"BBOX(0,5,1,6,EPSG:4326) → {string.Join(",", got)} count={cnt}",
                $"服务={string.Join(",", got)}({cnt}) 期望={string.Join(",", expected)}({expected.Count})");
        }

        /// <summary>重投影正确性：请求 EPSG:3857 时坐标应发生量级变化（>1e-9），且回显 CRS。</summary>
        public static CheckResult WfsReprojectionTransforms(string layer, string tag)
        {
            var u4326 = OgcProbe.Wfs("request=GetFeature", "version=2.0.0", "outputFormat=application/json",
                "typeName=" + layer, "CQL_FILTER=ID=51", "count=1");
            var u3857 = OgcProbe.Wfs("request=GetFeature", "version=2.0.0", "outputFormat=application/json",
                "typeName=" + layer, "CQL_FILTER=ID=51", "count=1", "srsName=EPSG:3857");
            var a = FirstCoord(OgcProbe.Get(u4326).Text);
            var b = FirstCoord(OgcProbe.Get(u3857).Text);
            // 硬性前置：两侧都必须先取到坐标（此前 fixture 缺失/端点错导致 FirstCoord=null → 假“无差异”）。
            if (!a.HasValue || !b.HasValue)
                return Check.Fail("WfsReproj/" + tag,
                    $"未取到首个坐标：4326={a.HasValue} 3857={b.HasValue}（fixture/端点异常，须先排除）");
            bool transformed = Math.Abs(a.Value.x - b.Value.x) > 1e-9 || Math.Abs(a.Value.y - b.Value.y) > 1e-9;
            // 附：本 GeoServer EPSG:4490(CGCS2000 地理坐标系) 与 4326(WGS84) 数值等价（椭球近乎一致、无 datum 平移），
            // 故不用于“发生转换”的断言——记录为事实说明。
            var u4490 = OgcProbe.Wfs("request=GetFeature", "version=2.0.0", "outputFormat=application/json",
                "typeName=" + layer, "CQL_FILTER=ID=51", "count=1", "srsName=EPSG:4490");
            var c = FirstCoord(OgcProbe.Get(u4490).Text);
            bool eq4490 = a.HasValue && c.HasValue && Math.Abs(a.Value.x - c.Value.x) < 1e-6 && Math.Abs(a.Value.y - c.Value.y) < 1e-6;
            var msg = $"3857 变换 4326({a?.x:F3},{a?.y:F3})→3857({b?.x:F1},{b?.y:F1}); 4490 与 4326 数值等价={eq4490}";
            return Check.Cond(transformed, "WfsReproj/" + tag, msg,
                transformed ? msg : "3857 输出与 4326 无差异（" + msg + "）");
        }

        public static CheckResult WfsPostgisMatchesShapefile(string dataDir, string pgLayer, string shpLayer, string tag)
        {
            var rows = DbfRecords.Read(Path.Combine(dataDir, "gdtest_poly.dbf"));
            var exp = new Dictionary<string, (int, double)>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rows.Count; i++)
                exp[Col(rows, i, "NAME")] = (DbfRecords.GetInt(rows, "ID", i), DbfRecords.GetDouble(rows, "VALUE", i));

            var pg = ParseWfsFeatures(OgcProbe.Get(OgcProbe.Wfs("request=GetFeature", "version=2.0.0",
                "outputFormat=application/json", "typeName=" + pgLayer)).Text);
            var sh = ParseWfsFeatures(OgcProbe.Get(OgcProbe.Wfs("request=GetFeature", "version=2.0.0",
                "outputFormat=application/json", "typeName=" + shpLayer)).Text);
            var probs = new List<string>();
            if (pg.count != 12) probs.Add("PG count=" + pg.count);
            if (pg.count != sh.count) probs.Add($"PG {pg.count}!=SHP {sh.count}");
            foreach (var kv in exp)
            {
                if (!TryName(pg.byName, kv.Key, out var p) || !TryName(sh.byName, kv.Key, out var s))
                { probs.Add("缺 " + kv.Key); continue; }
                if (p.id != kv.Value.Item1 || Math.Abs(p.val - kv.Value.Item2) > 1e-9) probs.Add(kv.Key + " PG属性");
                if (p.id != s.id || Math.Abs(p.val - s.val) > 1e-9) probs.Add(kv.Key + " PG!=SHP属性");
                double areaPg = AreaFromGeometry(p.geom), areaShp = AreaFromGeometry(s.geom);
                double denom = Math.Max(1e-12, Math.Abs(areaShp));
                if (Math.Abs(areaPg - areaShp) / denom > ToleranceAreaRel) probs.Add($"{kv.Key} 面积相对差 {Math.Abs(areaPg - areaShp) / denom:E2}");
            }
            return Check.Cond(probs.Count == 0, "WfsPgVsShp/" + tag,
                $"PG 12 要素属性与 shapefile/DBF 一致；同名面积相对差 <{ToleranceAreaRel}",
                string.Join("; ", probs.Take(6)));
        }

        // ============ WMS ============
        public static CheckResult WmsCapabilitiesHasLayer(string layer, string version)
        {
            var r = OgcProbe.Get(OgcProbe.Wms("request=GetCapabilities", "version=" + version));
            var t = r.Text ?? "";
            bool ok = r.Ok && t.Contains(layer) && t.Contains(Ws);
            return Check.Cond(ok, "WmsCaps/" + version, $"含 {layer}",
                $"{layer} 未在 {version} 能力表中（HTTP {r.Status}）");
        }

        public static CheckResult WmsGetMapPngDims(string layer, int w, int h)
        {
            var r = OgcProbe.Get(OgcProbe.Wms("request=GetMap", "version=1.3.0", "layers=" + layer,
                "bbox=0,0,11,6", "width=" + w, "height=" + h, "crs=EPSG:4326", "format=image/png"));
            if (!(r.Ok && IsPng(r.Bytes))) return Check.Fail("WmsGetMapDims", $"非 PNG 或 HTTP {r.Status} ct={r.ContentType}");
            using var bmp = SKBitmap.Decode(r.Bytes);
            bool dims = bmp != null && bmp.Width == w && bmp.Height == h;
            return Check.Cond(dims, "WmsGetMapDims", $"PNG {bmp?.Width}x{bmp?.Height} 尺寸正确",
                $"解码尺寸 {bmp?.Width}x{bmp?.Height} 期望 {w}x{h}");
        }

        public static CheckResult WmsRedPixels(string layer, string styleName)
        {
            var r = OgcProbe.Get(OgcProbe.Wms("request=GetMap", "version=1.1.1", "layers=" + layer,
                "styles=" + styleName, "bbox=0,0,11,6", "width=220", "height=120", "srs=EPSG:4326", "format=image/png"));
            if (!(r.Ok && IsPng(r.Bytes))) return Check.Fail("WmsRedPixels", $"GetMap 非 PNG（HTTP {r.Status}）");
            using var bmp = SKBitmap.Decode(r.Bytes);
            if (bmp == null) return Check.Fail("WmsRedPixels", "PNG 解码失败");
            int red = 0; var distinct = new HashSet<uint>(); int w = bmp.Width, h = bmp.Height;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var p = bmp.GetPixel(x, y);
                    distinct.Add(((uint)p.Red << 16) | ((uint)p.Green << 8) | p.Blue);
                    if (p.Red > 150 && p.Green < 80 && p.Blue < 80) red++;
                }
            }
            bool ok = red > 0 && distinct.Count > 1;   // 有红色像素 + 非白屏（多色）
            return Check.Cond(ok, "WmsRedPixels",
                $"红色像素={red}，distinct={distinct.Count}",
                $"red={red} distinct={distinct.Count}（应>0且>1，防白屏）");
        }

        /// <summary>WMS GetMap 目标色像素检查（color: red/blue）：命中像素&gt;0 且非白屏（distinct&gt;1）。</summary>
        public static CheckResult WmsColorPixels(string layer, string styleName, string color, string tag)
        {
            var r = OgcProbe.Get(OgcProbe.Wms("request=GetMap", "version=1.1.1", "layers=" + layer,
                "styles=" + styleName, "bbox=0,0,11,6", "width=220", "height=120", "srs=EPSG:4326", "format=image/png"));
            if (!(r.Ok && IsPng(r.Bytes))) return Check.Fail("WmsPixels/" + tag, $"GetMap 非 PNG（HTTP {r.Status}）");
            using var bmp = SKBitmap.Decode(r.Bytes);
            if (bmp == null) return Check.Fail("WmsPixels/" + tag, "PNG 解码失败");
            int hit = 0; var distinct = new HashSet<uint>(); int w = bmp.Width, h = bmp.Height;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var p = bmp.GetPixel(x, y);
                    distinct.Add(((uint)p.Red << 16) | ((uint)p.Green << 8) | p.Blue);
                    if (color == "red" && p.Red > 150 && p.Green < 80 && p.Blue < 80) hit++;
                    if (color == "blue" && p.Blue > 150 && p.Red < 80 && p.Green < 80) hit++;
                }
            }
            bool ok = hit > 0 && distinct.Count > 1;   // 有目标色像素 + 非白屏（多色）
            return Check.Cond(ok, "WmsPixels/" + tag,
                $"{color} 像素={hit}，distinct={distinct.Count}",
                $"{color}={hit} distinct={distinct.Count}（应>0且>1，防白屏）");
        }

        public static CheckResult WmsDemoLayerNonMonochrome(string layer, double[] bbox, string tag)
        {
            var r = OgcProbe.Get(OgcProbe.Wms("request=GetMap", "version=1.3.0", "layers=" + layer,
                $"bbox={bbox[0]},{bbox[1]},{bbox[2]},{bbox[3]}", "width=220", "height=120", "crs=EPSG:4326", "format=image/png"));
            if (!(r.Ok && IsPng(r.Bytes)))
                return Check.Warn("WmsDemo/" + tag, $"{layer} GetMap 非 PNG（HTTP {r.Status}）——只读交叉验证，降级为 Warn");
            using var bmp = SKBitmap.Decode(r.Bytes);
            if (bmp == null) return Check.Warn("WmsDemo/" + tag, "解码失败");
            int w = bmp.Width, h = bmp.Height; var distinct = new HashSet<uint>();
            int samples = 0;
            for (int y = 0; y < h; y += 3)
            {
                for (int x = 0; x < w; x += 3)
                {
                    var p = bmp.GetPixel(x, y);
                    distinct.Add(((uint)p.Red << 16) | ((uint)p.Green << 8) | p.Blue);
                    samples++;
                }
            }
            return Check.Cond(distinct.Count > 1, "WmsDemo/" + tag,
                $"{layer} 采样 {samples} 点 distinct={distinct.Count}（数据→像素链路成立）",
                $"仅 {distinct.Count} 色（疑似空白）");
        }

        public static CheckResult WmsDemoLayerFromCaps(string layer, string tag)
        {
            var caps = OgcProbe.Get(OgcProbe.Wms("request=GetCapabilities", "version=1.1.1"));
            var bbox = ParseLayerLatLonBbox(caps.Text ?? "", layer);
            if (bbox == null)
                return Check.Warn("WmsDemoCaps/" + tag, $"无法从 1.1.1 能力表解析 {layer} 的 LatLonBoundingBox（可能不存在），Warn");
            var r = OgcProbe.Get(OgcProbe.Wms("request=GetMap", "version=1.1.1", "layers=" + layer,
                $"bbox={bbox[0]},{bbox[1]},{bbox[2]},{bbox[3]}", "width=220", "height=120", "srs=EPSG:4326", "format=image/png"));
            if (!(r.Ok && IsPng(r.Bytes)))
                return Check.Warn("WmsDemoCaps/" + tag, $"{layer} GetMap 非 PNG（HTTP {r.Status}）——只读交叉验证，Warn");
            using var bmp = SKBitmap.Decode(r.Bytes);
            if (bmp == null) return Check.Warn("WmsDemoCaps/" + tag, "PNG 解码失败");
            int w = bmp.Width, h = bmp.Height; var distinct = new HashSet<uint>();
            for (int y = 0; y < h; y += 3)
            {
                for (int x = 0; x < w; x += 3)
                {
                    var p = bmp.GetPixel(x, y);
                    distinct.Add(((uint)p.Red << 16) | ((uint)p.Green << 8) | p.Blue);
                }
            }
            return Check.Cond(distinct.Count > 1, "WmsDemoCaps/" + tag,
                $"{layer} bbox=({bbox[0]:F1},{bbox[1]:F1},{bbox[2]:F1},{bbox[3]:F1}) distinct={distinct.Count}（数据→像素链路在既有真实数据成立）",
                $"仅 {distinct.Count} 色（疑似空白）");
        }

        private static double[] ParseLayerLatLonBbox(string caps, string layer)
        {
            int li = caps.IndexOf("<Layer", StringComparison.Ordinal);
            while (li >= 0)
            {
                int end = caps.IndexOf("</Layer>", li, StringComparison.Ordinal);
                if (end < 0) break;
                var seg = caps.Substring(li, end - li);
                if (seg.Contains("<Name>" + layer + "</Name>"))
                {
                    int bi = seg.IndexOf("<LatLonBoundingBox", StringComparison.Ordinal);
                    if (bi >= 0)
                    {
                        int be = seg.IndexOf("/>", bi, StringComparison.Ordinal);
                        var at = seg.Substring(bi, be - bi);
                        double? mnX = GrabNum(at, "minx"), mnY = GrabNum(at, "miny"), mxX = GrabNum(at, "maxx"), mxY = GrabNum(at, "maxy");
                        if (mnX != null && mnY != null && mxX != null && mxY != null)
                            return new[] { mnX.Value, mnY.Value, mxX.Value, mxY.Value };
                    }
                }
                li = caps.IndexOf("<Layer", end, StringComparison.Ordinal);
            }
            return null;
        }

        private static double? GrabNum(string s, string attr)
        {
            int i = s.IndexOf(attr + "=\"", StringComparison.Ordinal); if (i < 0) return null;
            i += attr.Length + 2; int j = s.IndexOf('"', i); if (j < 0) return null;
            return double.TryParse(s.Substring(i, j - i), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : (double?)null;
        }

        public static CheckResult WmsFeatureInfoPoly00(string dataDir, string layer, string tag)
        {
            // bbox(0,0,11,6) 尺寸 220x120 → 每像素 0.05°x / 0.05°y。poly_00 中心 (0.5,0.5)：
            //   x = (0.5-0)/11*220 = 10；WMS1.1.1 y 自顶向下：y = (6-0.5)/6*120 = 110。
            var r = OgcProbe.Get(OgcProbe.Wms("request=GetFeatureInfo", "version=1.1.1", "layers=" + layer,
                "query_layers=" + layer, "bbox=0,0,11,6", "width=220", "height=120", "srs=EPSG:4326",
                "x=10", "y=110", "info_format=application/json", "feature_count=1"));
            var name = FirstProp(r.Text, "NAME");
            bool ok = r.Ok && name == "poly_00";
            return Check.Cond(ok, "WmsFeatureInfo/" + tag,
                "(0.5,0.5) 命中 NAME=" + name,
                $"NAME={name ?? "<null>"}（期望 poly_00，HTTP {r.Status}）");
        }

        // ============ WCS ============
        public static CheckResult WcsCapabilitiesHasCoverage(string covName, string tag)
        {
            var r = OgcProbe.Get(OgcProbe.Wcs("request=GetCapabilities", "version=2.0.1"));
            var t = r.Text ?? "";
            bool ok = r.Ok && t.Contains(covName);
            return Check.Cond(ok, "WcsCaps/" + tag, $"含 {covName}", $"{covName} 未在 WCS 能力表（HTTP {r.Status}）");
        }

        /// <summary>WCS 2.0.1 GetCoverage GeoTIFF：宽81高41位32、ProjCS32754、像元=公式值、仿射标签与源一致。</summary>
        public static CheckResult WcsCoverage201(string dataDir, string covId, string tifSourceName, string tag)
        {
            var src = Path.Combine(dataDir, tifSourceName);
            var r = OgcProbe.Get(OgcProbe.Wcs("request=GetCoverage", "version=2.0.1",
                "CoverageId=" + covId, "format=GeoTIFF"));
            if (!(r.Ok && IsTiff(r.Bytes))) return Check.Fail("Wcs201/" + tag, $"非 TIFF（HTTP {r.Status} ct={r.ContentType}）");
            var tmp = Path.Combine(Path.GetTempPath(), "gsd_wcs_" + Guid.NewGuid().ToString("N") + ".tif");
            File.WriteAllBytes(tmp, r.Bytes);
            try
            {
                var h = TiffHeader.Parse(tmp);
                var sh = TiffHeader.Parse(src);
                var probs = new List<string>();
                if (h.Width != 81 || h.Height != 41) probs.Add($"尺寸 {h.Width}x{h.Height}");
                if (h.BitsPerSample != 32) probs.Add("bits=" + h.BitsPerSample);
                if (h.GeoTiffProjCS != 32754) probs.Add("ProjCS=" + h.GeoTiffProjCS);
                // 仿射：源文件必含 ModelPixelScale/Tiepoint；断言与源一致（服务端若丢弃则如实 Warn 记录）
                bool srcScale = sh.ModelPixelScale != null && sh.ModelPixelScale.Length >= 3
                    && Approx(sh.ModelPixelScale[0], 100) && Approx(sh.ModelPixelScale[1], 100) && Approx(sh.ModelPixelScale[2], 0);
                bool srcTie = sh.ModelTiepoint != null && sh.ModelTiepoint.Length >= 6
                    && Approx(sh.ModelTiepoint[3], 500000) && Approx(sh.ModelTiepoint[4], 2200060);
                if (!srcScale) probs.Add("源 ModelPixelScale≠(100,100,0)");
                if (!srcTie) probs.Add("源 ModelTiepoint≠(…,500000,2200060,…)");
                // 像元真值：读 WCS 输出（tiled/大端），与公式比对
                foreach (var (px, py) in new[] { (0, 0), (80, 0), (0, 40), (80, 40), (40, 20), (7, 13) })
                {
                    float actual = TiffPixels.ReadFloat32(tmp, px, py);
                    double expected = DemFormula(px, py);
                    if (Math.Abs(actual - expected) > TolerancePixel)
                        probs.Add($"({px},{py}) 实际{actual} 期望{expected}");
                }
                // WCS 输出是否保留仿射标签（实测 GeoServer 2.0.1 GeoTIFF 丢弃 33550/33922）——信息性 Warn
                bool wcsHasAffine = h.ModelPixelScale != null && h.ModelTiepoint != null;
                var extra = wcsHasAffine ? "" : "（注：WCS 输出丢弃 ModelPixelScale/Tiepoint，仅保留 GeoKey ProjCS，已如实记录）";
                if (probs.Count > 0) return Check.Fail("Wcs201/" + tag, string.Join("; ", probs));
                return wcsHasAffine
                    ? Check.Pass("Wcs201/" + tag, "81x41 bits32 ProjCS32754 仿射(源)一致 像元=公式")
                    : Check.Warn("Wcs201/" + tag, "尺寸/位深/ProjCS/像元/源仿射均通过，" + extra);
            }
            finally { try { File.Delete(tmp); } catch { } }
        }

        public static CheckResult WcsCoverage100Compat(string covId, string tag)
        {
            var r = OgcProbe.Get(OgcProbe.Wcs("request=GetCoverage", "version=1.0.0",
                "Coverage=" + covId, "Format=GeoTIFF"));
            if (r.Ok && IsTiff(r.Bytes)) return Check.Pass("Wcs100/" + tag, "1.0.0 GeoTIFF 兼容形态出图 OK");
            string why = r.Text != null && r.Text.Contains("Could not understand version") ? "本 GeoServer 3.0.1 WCS 不受理 1.0.0" : ("HTTP " + r.Status);
            return Check.Warn("Wcs100/" + tag, "WCS 1.0.0 GetCoverage 未返回合法 TIFF：" + why + "（按现状 Warn）");
        }

        /// <summary>DEM 像元值确定式：v=(x-500000)/100 + 2*(y-2200060)/100 + (px*py)%7。</summary>
        public static double DemFormula(int px, int py)
        {
            double x = 500000.0 + (px + 0.5) * 100.0;
            double y = 2200060.0 - (py + 0.5) * 100.0;
            return (x - 500000) / 100 + 2 * (y - 2200060) / 100 + (px * py) % 7;
        }

        // ============ WMTS / GWC ============
        public static CheckResult WmtsHasLayer(string layerName)
        {
            var r = OgcProbe.Get(OgcProbe.GwcWmts("service=WMTS", "version=1.0.0", "request=GetCapabilities"));
            var t = r.Text ?? "";
            bool listed = r.Ok && t.Contains(layerName);
            if (listed) return Check.Pass("WmtsCaps/layer", "能力表含 " + layerName);
            return Check.Warn("WmtsCaps/layer", $"{layerName} 未在 WMTS 能力表（HTTP {r.Status}）——默认未启用则属正常，降级 Warn");
        }

        public static CheckResult WmtsTile(string layerName)
        {
            var caps = OgcProbe.Get(OgcProbe.GwcWmts("service=WMTS", "version=1.0.0", "request=GetCapabilities"));
            var info = ParseTileMatrixFor(caps.Text ?? "", layerName);
            if (info == null)
                return Check.Warn("WmtsTile", $"{layerName} 无可解析的 TileMatrixSetLink/format（GWC 未注册或未启用），Warn");
            var (set, matrix, fmt) = info.Value;
            var r = OgcProbe.Get(OgcProbe.GwcWmts("service=WMTS", "version=1.0.0", "request=GetTile",
                "layer=" + layerName, "style=", "tilematrixset=" + set, "TileMatrix=" + matrix,
                "TileCol=0", "TileRow=0", "format=" + fmt));
            if (!r.Ok || r.Bytes == null || r.Bytes.Length == 0)
                return Check.Warn("WmtsTile", $"{layerName} GetTile({set}/{matrix}) HTTP {r.Status}——扩展/矩阵缺失按现状 Warn");
            bool img = IsPng(r.Bytes) || IsJpeg(r.Bytes) || IsGif(r.Bytes);
            return Check.Cond(img, "WmtsTile",
                $"{layerName} tile {set}/{matrix} {fmt} → {r.Bytes.Length}B 图像",
                $"tile 返回 {r.Bytes.Length}B 但非已知图像魔数 ct={r.ContentType}");
        }

        private static (string, string, string)? ParseTileMatrixFor(string caps, string layerName)
        {
            // 在含 layerName 的 <Layer>…</Layer> 片段里取 Format 与 TileMatrixSetLink/TileMatrix（去依赖字符串扫描）。
            int li = caps.IndexOf("<Layer", StringComparison.Ordinal);
            while (li >= 0)
            {
                int end = caps.IndexOf("</Layer>", li, StringComparison.Ordinal);
                if (end < 0) break;
                var seg = caps.Substring(li, end - li);
                if (seg.Contains(layerName))
                {
                    // 该 GeoServer 矢量图层默认 format 是 pbf(Mapbox vector tile)，能力表里 <Format> 首个即 mvt。
                    // 瓦片像素真值校验须显式选 image/png（其次 jpeg/gif），否则 GetTile 回 mvt 无图像魔数。
                    var fmt = ChooseRasterFormat(seg);
                    var set = Grab(seg, "<TileMatrixSet>", "</TileMatrixSet>");
                    // TileMatrix 在 TileMatrixSetLink/<TileMatrixSet> 之后；退化取任意 <TileMatrix>
                    var m = Grab(seg, "<TileMatrix>", "</TileMatrix>");
                    if (fmt == null) return null;                 // 无栅格 format（仅矢量）→ 上层按现状 Warn
                    if (set != null && m != null) return (set, m, fmt);
                    if (set != null) return (set, set + ":0", fmt);
                }
                li = caps.IndexOf("<Layer", end, StringComparison.Ordinal);
            }
            return null;
        }

        private static string Grab(string s, string open, string close)
        {
            int i = s.IndexOf(open, StringComparison.Ordinal); if (i < 0) return null;
            i += open.Length; int j = s.IndexOf(close, i, StringComparison.Ordinal); if (j < 0) return null;
            return s.Substring(i, j - i).Trim();
        }

        /// <summary>从图层 <Format> 枚举中优先挑栅格图像 format（png>jpeg>gif），无则 null（纯矢量图层）。</summary>
        private static string ChooseRasterFormat(string seg)
        {
            var fmts = new List<string>();
            int idx = 0;
            while ((idx = seg.IndexOf("<Format>", idx, StringComparison.Ordinal)) >= 0)
            {
                int end = seg.IndexOf("</Format>", idx, StringComparison.Ordinal);
                if (end < 0) break;
                fmts.Add(seg.Substring(idx + 8, end - idx - 8).Trim());
                idx = end;
            }
            foreach (var pref in new[] { "image/png", "image/jpeg", "image/gif" })
                if (fmts.Any(f => string.Equals(f, pref, StringComparison.OrdinalIgnoreCase))) return pref;
            return null;
        }

        private static bool IsJpeg(byte[] b) => b != null && b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF;
        private static bool IsGif(byte[] b) => b != null && b.Length > 3 && b[0] == 'G' && b[1] == 'I' && b[2] == 'F';

        // ============ 解析辅助 ============
        private static bool LooksJson(string t) => t != null && t.TrimStart().StartsWith("{", StringComparison.Ordinal);
        private static string Snip(string t) => t == null ? "<null>" : (t.Length > 120 ? t.Substring(0, 120) : t).Replace("\n", " ").Replace("\r", " ");
        private static bool Approx(double a, double b) => Math.Abs(a - b) <= Math.Abs(b) * ToleranceGeoRel + 1e-9;
        private static bool IsPng(byte[] b) => b != null && b.Length > 8 && b[0] == 0x89 && b[1] == 'P' && b[2] == 'N' && b[3] == 'G';
        private static bool IsTiff(byte[] b) => b != null && b.Length > 4 && ((b[0] == 'I' && b[1] == 'I') || (b[0] == 'M' && b[1] == 'M'));

        private static int JsonInt(string text, string key)
        {
            if (text == null) return -1;
            try
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.TryGetProperty(key, out var v) && v.TryGetInt32(out var i)) return i;
                var s = doc.RootElement.GetProperty(key).ToString();
                return int.TryParse(s, out var j) ? j : -1;
            }
            catch { return -1; }
        }

        private static string FirstProp(string text, string prop)
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.TryGetProperty("features", out var fs) && fs.GetArrayLength() > 0)
                {
                    var f0 = fs[0];
                    if (f0.TryGetProperty("properties", out var p) && p.TryGetProperty(prop, out var pv))
                        return pv.ToString();
                }
            }
            catch { }
            return null;
        }

        private struct Feat { public int id; public double val; public JsonElement geom; }
        private static bool TryName(Dictionary<string, Feat> d, string name, out Feat f)
            => d.TryGetValue(name, out f) || d.TryGetValue(name.ToUpperInvariant(), out f);

        private static (int count, Dictionary<string, Feat> byName, double[] bbox) ParseWfsFeatures(string json)
        {
            var byName = new Dictionary<string, Feat>(StringComparer.Ordinal);
            double[] bb = { double.MaxValue, double.MaxValue, double.MinValue, double.MinValue };
            int count = 0;
            if (!LooksJson(json)) return (0, byName, new[] { 0d, 0d, 0d, 0d });
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("features", out var feats)) return (0, byName, new[] { 0d, 0d, 0d, 0d });
            foreach (var f in feats.EnumerateArray())
            {
                count++;
                var props = f.GetProperty("properties");
                // 大小写无关：shapefile WFS 属性名为大写(NAME/ID/VALUE)，PostGIS 表列被 PG 折叠为小写(name/id/value)。
                string name = GetPropCI(props, "NAME").ToString();
                int id = GetIntCI(props, "ID");
                double val = GetDoubleCI(props, "VALUE");
                var coordsEl = default(JsonElement);
                if (f.TryGetProperty("geometry", out var g) && g.ValueKind == JsonValueKind.Object
                    && g.TryGetProperty("coordinates", out var ce) && ce.ValueKind != JsonValueKind.Null)
                    coordsEl = ce.Clone();
                byName[name] = new Feat { id = id, val = val, geom = coordsEl };
                if (coordsEl.ValueKind == JsonValueKind.Undefined) continue;
                foreach (var (x, y) in EnumerateCoords(coordsEl))
                {
                    if (x < bb[0]) bb[0] = x; if (y < bb[1]) bb[1] = y;
                    if (x > bb[2]) bb[2] = x; if (y > bb[3]) bb[3] = y;
                }
            }
            if (count == 0) bb = new[] { 0d, 0d, 0d, 0d };
            return (count, byName, bb);
        }

        private static IEnumerable<(double x, double y)> EnumerateCoords(JsonElement coords)
        {
            foreach (var ring in Rings(coords))
                for (int i = 0; i < ring.xs.Length; i++)
                    yield return (ring.xs[i], ring.ys[i]);
        }

        private static (double[] xs, double[] ys)[] Rings(JsonElement coords)
        {
            var list = new List<(double[], double[])>();
            CollectRings(coords, list);
            return list.ToArray();
        }

        private static void CollectRings(JsonElement el, List<(double[], double[])> outRings)
        {
            if (el.ValueKind != JsonValueKind.Array) return;
            var arr = el.EnumerateArray().ToArray();
            if (arr.Length == 0) return;
            // 若首元素是数字对 → 当前数组即一个环
            if (arr[0].ValueKind == JsonValueKind.Array)
            {
                var first = arr[0].EnumerateArray().ToArray();
                if (first.Length >= 2 && first[0].ValueKind == JsonValueKind.Number)
                {
                    var xs = new double[arr.Length]; var ys = new double[arr.Length];
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var pt = arr[i].EnumerateArray().ToArray();
                        xs[i] = pt[0].GetDouble(); ys[i] = pt[1].GetDouble();
                    }
                    outRings.Add((xs, ys));
                    return;
                }
            }
            foreach (var c in arr) CollectRings(c, outRings);
        }

        /// <summary>由服务侧 geometry 求净面积：对每个环取有向面积，按“同属一多边形的外环+洞符号相反”累加后取绝对值。</summary>
        public static double AreaFromGeometry(JsonElement geom)
        {
            if (geom.ValueKind == JsonValueKind.Undefined) return 0;
            double total = 0;
            foreach (var ring in Rings(geom))
                total += Math.Abs(Shoelace(ring.xs, ring.ys));
            return total;
        }

        private static double Shoelace(double[] xs, double[] ys)
        {
            int n = xs.Length; if (n < 3) return 0; double s = 0;
            for (int i = 0, j = n - 1; i < n; j = i++) s += (xs[j] + xs[i]) * (ys[j] - ys[i]);
            return s / 2.0;
        }

        private static (int parts, int holes) ClassifyServerGeometry(JsonElement geom)
        {
            if (geom.ValueKind == JsonValueKind.Undefined) return (0, 0);
            return ComputePartsHoles(Rings(geom).ToList());
        }

        private static (int, int) ComputePartsHoles(List<(double[] xs, double[] ys)> rings)
        {
            var rec = new ShapefileRecords.Record();
            foreach (var (xs, ys) in rings)
            {
                var rg = new ShapefileRecords.Ring { Xs = xs, Ys = ys };
                double mnX = double.MaxValue, mnY = double.MaxValue, mxX = double.MinValue, mxY = double.MinValue;
                for (int i = 0; i < xs.Length; i++) { if (xs[i] < mnX) mnX = xs[i]; if (xs[i] > mxX) mxX = xs[i]; if (ys[i] < mnY) mnY = ys[i]; if (ys[i] > mxY) mxY = ys[i]; }
                rg.MinX = mnX; rg.MaxX = mxX; rg.MinY = mnY; rg.MaxY = mxY; rg.SignedArea = Shoelace(xs, ys);
                rec.Rings.Add(rg);
            }
            var (p, h, _) = ShapefileRecords.Classify(rec);
            return (p, h);
        }

        private static (double x, double y)? FirstCoord(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var g = doc.RootElement.GetProperty("features")[0].GetProperty("geometry");
                if (g.ValueKind != JsonValueKind.Object || !g.TryGetProperty("coordinates", out var c)
                    || c.ValueKind == JsonValueKind.Null) return null;
                foreach (var (x, y) in EnumerateCoords(c.Clone())) return (x, y);
            }
            catch { }
            return null;
        }

        // 大小写无关属性读取：GeoServer 对 shapefile 回大写 NAME/ID/VALUE，对 PostGIS 回 PG 折叠后的小写。
        private static JsonElement GetPropCI(JsonElement props, string name)
        {
            if (props.ValueKind != JsonValueKind.Object) return default;
            foreach (var p in props.EnumerateObject())
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) return p.Value;
            return default;
        }
        private static int GetIntCI(JsonElement props, string name)
        {
            var e = GetPropCI(props, name);
            return e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var v) ? v
                 : (e.ValueKind == JsonValueKind.String && int.TryParse(e.GetString(), out var s) ? s : 0);
        }
        private static double GetDoubleCI(JsonElement props, string name)
        {
            var e = GetPropCI(props, name);
            return e.ValueKind == JsonValueKind.Number && e.TryGetDouble(out var v) ? v : double.NaN;
        }
    }
}
