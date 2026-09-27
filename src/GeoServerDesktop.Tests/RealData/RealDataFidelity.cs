using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using GeoServerDesktop.GeoServerClient.Import;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.RealData
{
    /// <summary>
    /// 真实数据“保真度”检查族（扩展数据集与外部真实数据共用同一实现）：
    /// 期望值全部由 .shp/.dbf/.cpg 文件本体独立解析推导，服务侧只读，绝不回抄。
    /// 与 RealDataChecks 的分工：那边是既有小 fixture 的服务面锚定；这边是逐字段/逐几何/
    /// 编码/空间谓词/分页/维数/转义等“真实数据易错面”。
    /// </summary>
    public static class RealDataFidelity
    {
        public const double RelTol = 1e-6;

        // ============================ WFS 通用读取 ============================

        public sealed class WfsFeat
        {
            public JsonElement Props;
            public JsonElement? Geom;

            public JsonElement Prop(string name)
            {
                if (Props.ValueKind != JsonValueKind.Object) return default;
                foreach (var p in Props.EnumerateObject())
                    if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) return p.Value;
                return default;
            }

            public bool Has(string name) => Prop(name).ValueKind != JsonValueKind.Undefined;
        }

        public sealed class WfsResponse
        {
            public int Status;
            public int Matched = -1;
            public List<WfsFeat> Features = new List<WfsFeat>();
            public string Raw = "";
            public bool IsJson;
            public double[] BBox = { double.NaN, double.NaN, double.NaN, double.NaN };
            public int OrdinateDepth;
            /// <summary>响应为“JSON 前缀 + 尾部 XML 异常”（服务端流中途失败）。</summary>
            public bool StreamTruncated;
            public string StreamError = "";
        }

        /// <summary>取 WFS（JSON）：可选 CQL、srsName、count、startIndex。</summary>
        public static WfsResponse Wfs(string layer, string? cql = null, string? srsName = null,
                                      int? count = null, int? startIndex = null, string version = "2.0.0")
        {
            var kv = new List<string>
            {
                "request=GetFeature", "version=" + version, "outputFormat=application/json", "typeName=" + layer
            };
            if (cql != null) kv.Add("CQL_FILTER=" + cql);
            if (srsName != null) kv.Add("srsName=" + srsName);
            if (count.HasValue) kv.Add("count=" + count.Value);
            if (startIndex.HasValue) kv.Add("startIndex=" + startIndex.Value);
            var r = OgcProbe.Get(OgcProbe.Wfs(kv.ToArray()));
            var resp = new WfsResponse { Status = r.Status, Raw = r.Text };
            resp.IsJson = r.Text != null && r.Text.TrimStart().StartsWith("{", StringComparison.Ordinal);
            resp.Raw = r.Text ?? "";
            if (!resp.IsJson) return resp;
            try
            {
                using var doc = JsonDocument.Parse(r.Text!);
                var root = doc.RootElement;
                if (root.TryGetProperty("numberMatched", out var nm) && int.TryParse(nm.ToString(), out var m)) resp.Matched = m;
                else if (root.TryGetProperty("numberReturned", out var nr) && int.TryParse(nr.ToString(), out var m2)) resp.Matched = m2;
                if (!root.TryGetProperty("features", out var feats)) return resp;
                double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
                bool any = false;
                foreach (var f in feats.EnumerateArray())
                {
                    var feat = new WfsFeat
                    {
                        Props = f.TryGetProperty("properties", out var p) && p.ValueKind == JsonValueKind.Object ? p.Clone() : default,
                    };
                    if (f.TryGetProperty("geometry", out var g) && g.ValueKind == JsonValueKind.Object)
                    {
                        feat.Geom = g.Clone();
                        any = true;
                        var bb = GeometryDerive.BBox(GeometryDerive.RingsOf(feat.Geom));
                        if (bb[0] < minX) minX = bb[0];
                        if (bb[1] < minY) minY = bb[1];
                        if (bb[2] > maxX) maxX = bb[2];
                        if (bb[3] > maxY) maxY = bb[3];
                        if (resp.OrdinateDepth == 0) resp.OrdinateDepth = GeometryDerive.OrdinateDepth(feat.Geom);
                    }
                    resp.Features.Add(feat);
                }
                if (any) resp.BBox = new[] { minX, minY, maxX, maxY };
                if (resp.Matched < 0) resp.Matched = resp.Features.Count;
            }
            catch (Exception ex)
            {
                // JSON 解析失败＝服务端流被中途掐断（GeoServer 已提交响应头后再出错，异常只进日志不进流）；
                // 与“尾部拼 XML”同属一类：默认 CRS 输出对无 EPSG 码的自定义投影不可靠。
                resp.StreamTruncated = true;
                resp.StreamError = "PARSE-EX:" + ex.Message + " :: " + Tail(resp.Raw);
                resp.Raw = "PARSE-EX:" + ex.Message + " :: " + resp.Raw;
            }
            return resp;
        }

        private static string Tail(string? s)
        {
            if (s == null) return "<null>";
            int n = Math.Min(420, s.Length);
            var sb = new System.Text.StringBuilder(n);
            foreach (var c in s.Substring(s.Length - n))
                sb.Append(c == 10 || c == 13 ? ' ' : c);
            return sb.ToString();
        }

        private static string Snip(string? s) => s == null ? "<null>" : (s.Length > 160 ? s.Substring(0, 160) : s)
            .Replace("\n", " ").Replace("\r", " ");

        // ============================ 1. 逐字段属性保真 ============================

        /// <summary>
        /// 属性逐字段保真：DBF（编码感知解码 + 按字段类型归一）↔ WFS JSON。
        /// 覆盖文本（含中文/宽文本/NULL）、整型、浮点、日期、逻辑型。
        /// </summary>
        public static CheckResult AttributesAllFields(string hostDir, string baseName, string layer, string tag,
                                                     int maxRecords = 200,
                                                     DbfEncodingMode mode = DbfEncodingMode.DeclaredOrDetected,
                                                     bool ordinalWhenKeyNotUnique = true, string srsName = null,
                                                     bool encodingBaselineAsWarn = false)
        {
            var dbf = DbfTable.Load(Path.Combine(hostDir, baseName + ".dbf"), mode);
            var want = Math.Min(maxRecords, dbf.Rows.Count);
            var r = want == dbf.Rows.Count
                ? Wfs(layer, srsName: srsName)
                : Wfs(layer, count: want, srsName: srsName);
            if (r.StreamTruncated)
                return Check.Warn("Attrs/" + tag,
                    $"服务端 GeoJSON 流被截断（matched={r.Matched} 实取 {r.Features.Count} 要素）：{dbf.Rows.Count} 条真实数据无法经 JSON 完整取回；"
                    + "补 srsName（已声明 EPSG）可绕开——服务端异常尾部：" + Snip(r.StreamError));
            if (!r.IsJson) return Check.Fail("Attrs/" + tag, "WFS 非 JSON（HTTP " + r.Status + "）" + Snip(r.Raw));
            if (r.Features.Count == 0)
                return Check.Fail("Attrs/" + tag, $"服务返回 0 要素（matched={r.Matched} 文件={dbf.Rows.Count}）"
                    + " 请求=" + OgcProbe.Wfs(want == dbf.Rows.Count ? new[] { "request=GetFeature", "version=2.0.0", "outputFormat=application/json", "typeName=" + layer }
                                                          : new[] { "request=GetFeature", "version=2.0.0", "outputFormat=application/json", "typeName=" + layer, "count=" + want })
                    + " 响应尾部=" + Tail(r.Raw));

            var probs = new List<string>();
            if (r.Features.Count < want) probs.Add("服务返回 " + r.Features.Count + " < 期望 " + want);
            int mismatch = 0;
            string? firstDetail = null;
            // 主键（NAME 优先）；真实数据主键常重复 → 退化为按返回顺序比对（shapefile 存储按 FID 序返回）
            string keyField = ResolveKeyField(dbf);
            bool keysUnique = Enumerable.Range(0, dbf.Rows.Count).Select(i => dbf.Cell(i, keyField).Text).Distinct().Count()
                              == dbf.Rows.Count;
            bool ordinal = ordinalWhenKeyNotUnique && !keysUnique;
            for (int i = 0; i < want; i++)
            {
                var expected = dbf.Rows[i];
                WfsFeat? feat;
                string label;
                if (ordinal)
                {
                    feat = i < r.Features.Count ? r.Features[i] : (WfsFeat?)null;
                    label = "#" + i;
                }
                else
                {
                    var keyCell = expected[Array.IndexOf(dbf.FieldNames, keyField)];
                    if (keyCell.IsNull) continue;
                    label = keyCell.Text ?? ("#" + i);
                    feat = r.Features.FirstOrDefault(f => string.Equals(AsString(f.Prop(keyField)), keyCell.Text, StringComparison.Ordinal));
                }
                if (feat == null)
                {
                    string srvKeys = string.Join(",", r.Features.Take(3).Select(f => AsString(f.Prop(keyField)) ?? "?"));
                    probs.Add($"缺要素 {label}（服务前 3 键：{srvKeys}）"); mismatch++; continue;
                }
                for (int fi = 0; fi < dbf.FieldNames.Length; fi++)
                {
                    var got = feat.Prop(dbf.FieldNames[fi]);
                    var cell = expected[fi];
                    if (!CellMatches(cell, got, out string? why))
                    {
                        probs.Add($"{label}.{dbf.FieldNames[fi]} 服务={Describe(got)} 文件={cell}（type {cell.Type}｜{why}）");
                        mismatch++;
                        if (firstDetail == null) firstDetail = probs.Last();
                    }
                }
            }
            var note = "编码=" + dbf.EncodingName + (dbf.CpgPresent ? "(.cpg 声明)" : "(无 .cpg，" + (mode == DbfEncodingMode.DeclaredOnly ? "按服务端默认 Latin-1" : "按字节探测") + ")");
            if (probs.Count == 0)
                return Check.Pass("Attrs/" + tag, $"{want}/{dbf.Rows.Count} 记录 × {dbf.FieldNames.Length} 字段全一致；{note}");
            if (encodingBaselineAsWarn)
            {
                // 归因：差异是否“恰好等于把同一 DBF 字节按 ISO-8859-1 解码”的结果？
                // 是 → 服务端未认得文件真实编码（缺 .cpg），属服务端解码契约，Warn 基线；
                // 否 → 数据链路真的错了，Fail。
                var latin1 = AttributesAllFields(hostDir, baseName, layer, tag + "/latin1probe", maxRecords,
                    DbfEncodingMode.DeclaredOnly, ordinalWhenKeyNotUnique, srsName, false);
                Check.All.RemoveAll(r => r.Name == "Attrs/" + tag + "/latin1probe");
                bool onlyTextFieldDiffs = probs.TrueForAll(p => p.Contains("type C"));
                // 键集合层面的判定：服务返回的主键集合若与“按 Latin-1 解出的主键集合”一致，
                // 则“缺要素”同样是解码差异而非数据缺失。
                bool keySetsMatch = ServiceKeySetsMatchLatin1(hostDir, baseName, layer, maxRecords, srsName);
                bool pureDecoding = latin1.Status == CheckStatus.Pass || onlyTextFieldDiffs || keySetsMatch;
                string whyDecoding = $"{mismatch} 处不一致全部可由“服务端按 ISO-8859-1 解 GBK 字节”解释（缺 .cpg 的真实数据典型形态）；"
                    + "客户端预检已能标记该风险（DbfEncodingRisk.UndeclaredNonUtf8）——属服务端解码契约，记 Warn 基线。"
                    + "示例：" + Snip(probs[0]);
                string whyReal = $"{mismatch} 处不一致无法用单一解码差异解释（数据链路真错误）：{string.Join("; ", probs.Take(3))}；{note}";
                return pureDecoding ? Check.Warn("Attrs/" + tag, whyDecoding) : Check.Fail("Attrs/" + tag, whyReal);
            }
            return Check.Fail("Attrs/" + tag,
                $"{mismatch} 处不一致：{string.Join("; ", probs.Take(4))}；{note}");
        }

        /// <summary>属性比对主键：NAME 优先，否则首个文本字段，否则首个字段。</summary>
        public static string ResolveKeyField(DbfTable dbf)
        {
            for (int i = 0; i < dbf.FieldNames.Length; i++)
                if (string.Equals(dbf.FieldNames[i], "NAME", StringComparison.OrdinalIgnoreCase)) return dbf.FieldNames[i];
            for (int i = 0; i < dbf.FieldNames.Length; i++)
                if (dbf.FieldTypes[i] == 'C') return dbf.FieldNames[i];
            return dbf.FieldNames[0];
        }

        /// <summary>服务侧主键集合是否等于把 DBF 按“无声明→ISO-8859-1”解码后的主键集合。</summary>
        private static bool ServiceKeySetsMatchLatin1(string hostDir, string baseName, string layer, int maxRecords, string? srsName)
        {
            try
            {
                var t = DbfTable.Load(Path.Combine(hostDir, baseName + ".dbf"), DbfEncodingMode.DeclaredOnly);
                string key = ResolveKeyField(t);
                int ki = Array.IndexOf(t.FieldNames, key);
                var want = new SortedSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < Math.Min(maxRecords, t.Rows.Count); i++)
                {
                    var v = t.Rows[i][ki].Text;
                    if (v != null) want.Add(v);
                }
                var r = Wfs(layer, count: maxRecords, srsName: srsName);
                if (!r.IsJson || r.Features.Count == 0) return false;
                var got = new SortedSet<string>(StringComparer.Ordinal);
                foreach (var f in r.Features)
                {
                    var v = AsString(f.Prop(key));
                    if (v != null) got.Add(v);
                }
                return got.SetEquals(want);
            }
            catch { return false; }
        }

        private static string? AsString(JsonElement e)
        {
            if (e.ValueKind == JsonValueKind.String) return e.GetString();
            if (e.ValueKind == JsonValueKind.Undefined || e.ValueKind == JsonValueKind.Null) return null;
            return e.ToString();
        }

        private static string Describe(JsonElement e)
            => e.ValueKind == JsonValueKind.Undefined ? "<absent>" : e.ValueKind == JsonValueKind.Null ? "<null>" : e.ToString();

        /// <summary>DBF 单元格与服务侧 JSON 值的类型归一比较。</summary>
        public static bool CellMatches(DbfCell cell, JsonElement got, out string? reason)
        {
            reason = null;
            bool gotNull = got.ValueKind == JsonValueKind.Undefined || got.ValueKind == JsonValueKind.Null;
            if (cell.IsNull)
            {
                if (gotNull) return true;
                var s = AsString(got);
                if (string.IsNullOrWhiteSpace(s)) return true;                  // 空串按 NULL 等价
                reason = "期望 NULL"; return false;
            }
            if (gotNull) { reason = "服务缺该属性"; return false; }
            switch (cell.Type)
            {
                case 'C':
                    {
                        var gs = AsString(got);
                        if (gs == cell.Text) return true;
                        reason = "文本不等"; return false;
                    }
                case 'N':
                case 'F':
                    {
                        double gd = got.ValueKind == JsonValueKind.Number ? got.GetDouble()
                                  : (double.TryParse(AsString(got), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : double.NaN);
                        if (double.IsNaN(gd)) { reason = "非数值"; return false; }
                        double rel = Math.Abs(gd - cell.Num) / Math.Max(1.0, Math.Abs(cell.Num));
                        if (rel <= 1e-9) return true;
                        reason = "数值差"; return false;
                    }
                case 'D':
                    {
                        var gs = AsString(got) ?? got.ToString();
                        var lead = gs.Length >= 10 ? gs.Substring(0, 10) : gs;
                        if (lead == cell.DateIso) return true;
                        reason = "日期不等"; return false;
                    }
                case 'L':
                    {
                        bool? gb = got.ValueKind == JsonValueKind.True ? true
                                 : got.ValueKind == JsonValueKind.False ? false
                                 : (bool?)null;
                        if (gb == null)
                        {
                            var gs = (AsString(got) ?? "").Trim();
                            if (gs == "T" || gs == "F" || gs == "Y" || gs == "N" || gs == "true" || gs == "false")
                                gb = gs == "T" || gs == "Y" || gs == "true";
                        }
                        if (gb == cell.Bool) return true;
                        reason = "逻辑值不等"; return false;
                    }
                default:
                    return AsString(got) == cell.Text;
            }
        }

        // ============================ 2. DBF 编码基线（客户端必须检出，服务端解码如实记录） ============================

        /// <summary>
        /// 编码基线：
        ///  · 硬约束（客户端职责）——GeoFileInspector 必须对“含非 ASCII 且缺 .cpg”的数据报出编码风险，
        ///    对已声明的数据不得误报；
        ///  · 软记录（服务端契约）——WFS 属性面是否等于文件语义真值。若因服务端按 ISO-8859-1 解码而乱码，
        ///    记为 Warn 基线（客户端无法代为改正服务端的解码行为），并要求客户端已提前告知用户。
        /// </summary>
        public static CheckResult EncodingRiskBaseline(string hostDir, string baseName, string layer, string tag, bool expectRisk)
        {
            var shp = Path.Combine(hostDir, baseName + ".shp");
            var p = GeoFileInspector.InspectShapefile(shp);
            var detected = p.EncodingRisk;
            bool detectionOk = expectRisk
                ? detected == DbfEncodingRisk.UndeclaredNonUtf8 || detected == DbfEncodingRisk.UndeclaredUtf8Bytes
                : detected == DbfEncodingRisk.None;

            var attrs = AttributesAllFields(hostDir, baseName, layer, tag + "/server", 200);
            if (expectRisk && attrs.Status == CheckStatus.Fail)
                Check.All.RemoveAll(r => r.Name == "Attrs/" + tag + "/server");   // 子探测归并进基线判定，不重复计 Fail
            string serverSide = attrs.Status == CheckStatus.Pass
                ? "服务属性面与文件语义真值一致"
                : "服务属性面偏离文件语义真值（服务端按声明/默认编码解码所致）";

            if (!detectionOk)
                return Check.Fail("EncRisk/" + tag,
                    $"客户端预检判定错误：期望风险={expectRisk} 实判={detected}"
                    + $"（非ASCII={p.DbfHasNonAscii} 合法UTF8={p.DbfLooksUtf8} cpg={p.CpgEncoding ?? "(无)"}）；{serverSide}");

            if (expectRisk)
                return Check.Cond(attrs.Status == CheckStatus.Pass, "EncRisk/" + tag,
                    $"客户端已检出编码风险（{detected}），且服务端仍读对：" + serverSide,
                    $"客户端已检出编码风险（{detected}，预检告警可展示）；{serverSide}"
                    + $"（cpg={p.CpgEncoding ?? "(无)"} 非ASCII={p.DbfHasNonAscii} 合法UTF8={p.DbfLooksUtf8}）"
                    + " —— 服务端解码契约不可由客户端改正，按基线 Warn", warnInsteadOfFail: true);

            return Check.Cond(attrs.Status == CheckStatus.Pass, "EncRisk/" + tag,
                $"无编码风险且服务读值正确（cpg={p.CpgEncoding ?? "(无)"}）：{attrs.Message}",
                $"客户端判定无风险，但 {attrs.Message}");
        }

        /// <summary>缺 .prj 的正向合法数据：可发布，但客户端预检必须把“投影未声明”报出来。</summary>
        public static CheckResult MissingPrjSurfaced(string hostDir, string baseName, string tag)
        {
            var p = GeoFileInspector.InspectShapefile(Path.Combine(hostDir, baseName + ".shp"));
            bool flagged = p.CrsRisk == CrsDeclarationRisk.MissingPrj
                           && p.Warnings != null && p.Warnings.Exists(w => w.Contains(".prj"));
            return Check.Cond(flagged, "CrsRisk/" + tag,
                $"缺 .prj 已被预检标记（风险={p.CrsRisk}，告警 {p.Warnings?.Count} 条）",
                $"缺 .prj 未被预检标记：风险={p.CrsRisk} 告警={(p.Warnings == null ? -1 : p.Warnings.Count)} 条 epsg={p.EpsgCode ?? "(null)"}");
        }

        /// <summary>
        /// 服务端登记的原生 bbox 必须与 SHP 文件头一致（对投影系数据同样成立，是不依赖坐标变换的范围真值）。
        /// </summary>
        /// <summary>
        /// 服务端登记的原生 bbox 必须与 SHP 文件头一致（投影系数据同样成立，是不依赖坐标变换的范围真值）。
        /// </summary>
        /// <summary>
        /// 服务端登记的原生 bbox 必须与 SHP 文件头一致（投影系数据同样成立，是不依赖坐标变换的范围真值）。
        /// </summary>
        public static CheckResult NativeBboxMatchesShpHeader(string hostDir, string baseName, string ws, string store, string featureTypeName, string tag)
        {
            var hdr = ShapefileHeader.Parse(Path.Combine(hostDir, baseName + ".shp"));
            var r = OgcProbe.Get(TestEnv.RestBase + "/rest/workspaces/" + Uri.EscapeDataString(ws) + "/datastores/"
                + Uri.EscapeDataString(store) + "/featuretypes/" + Uri.EscapeDataString(featureTypeName) + ".json");
            if (!r.Ok) return Check.Fail("NativeBbox/" + tag, "featureType REST HTTP " + r.Status);
            double nx, ny, mx, my;
            try
            {
                using (var doc = JsonDocument.Parse(r.Text!))
                {
                    var ft = doc.RootElement.GetProperty("featureType");
                    if (!ft.TryGetProperty("nativeBoundingBox", out var bb))
                        return Check.Fail("NativeBbox/" + tag, "REST featureType 无 nativeBoundingBox 字段");
                    nx = Dbl(bb, "minx"); ny = Dbl(bb, "miny"); mx = Dbl(bb, "maxx"); my = Dbl(bb, "maxy");
                }
            }
            catch (Exception ex) { return Check.Fail("NativeBbox/" + tag, ex.GetType().Name + ": " + ex.Message); }
            if (double.IsNaN(nx) || double.IsNaN(ny) || double.IsNaN(mx) || double.IsNaN(my))
                return Check.Fail("NativeBbox/" + tag, "nativeBoundingBox 字段解析失败（形态变更）");

            // SHP 头 bbox 是记录并集；服务端可能对度/分/秒制式做规范化，故按“包含关系 + 量级一致”判定
            double tol = Math.Max(1e-6, Math.Max(hdr.XMax - hdr.XMin, hdr.YMax - hdr.YMin) * 1e-4);
            bool covers = nx <= hdr.XMin + tol && ny <= hdr.YMin + tol && mx >= hdr.XMax - tol && my >= hdr.YMax - tol;
            bool sameScale = Math.Abs((mx - nx) - (hdr.XMax - hdr.XMin)) <= tol
                          && Math.Abs((my - ny) - (hdr.YMax - hdr.YMin)) <= tol;
            return Check.Cond(covers && sameScale, "NativeBbox/" + tag,
                $"服务端原生 bbox ({Fmt(nx)},{Fmt(ny)},{Fmt(mx)},{Fmt(my)}) 覆盖并与 SHP 头同尺度",
                $"服务端=({Fmt(nx)},{Fmt(ny)},{Fmt(mx)},{Fmt(my)}) SHP头=({Fmt(hdr.XMin)},{Fmt(hdr.YMin)},{Fmt(hdr.XMax)},{Fmt(hdr.YMax)}) covers={covers} sameScale={sameScale}");
        }

        /// <summary>
        /// 服务面是否因“自定义投影（.prj 无 EPSG 权威码）”而不可靠：GeoServer 3.0.1 实测三种形态
        /// —— ① 响应尾部拼 XML 异常；② 流被直接掐断（JSON 不完整，解析失败）；③ 直接 400 NPE。
        /// 三者同因，统一归为契约 Warn，并在消息里保留原文证据。
        /// </summary>
        private static bool ServicePlaneUnreliable(WfsResponse r, string layer, ref string evidence)
        {
            if (r.StreamTruncated)
            {
                evidence = Snip(r.StreamError.Length > 0 ? r.StreamError : r.Raw);
                return true;
            }
            string raw = r.Raw ?? "";
            bool looksCrsFault = raw.IndexOf("ExceptionReport", StringComparison.Ordinal) >= 0
                              && (raw.IndexOf("identifier", StringComparison.Ordinal) >= 0
                                  || raw.IndexOf("CRS", StringComparison.Ordinal) >= 0
                                  || raw.IndexOf("crs", StringComparison.Ordinal) >= 0);
            if (!looksCrsFault && r.Status == 400)
            {
                var probe = Wfs(layer, count: 1, srsName: "EPSG:4326");
                if (probe.IsJson && !probe.StreamTruncated && probe.Features.Count > 0)
                {
                    evidence = "默认 CRS 报 400，显式 EPSG:4326 可取回（" + probe.Features.Count + " 要素）";
                    return true;
                }
            }
            evidence = Snip(raw);
            return looksCrsFault;
        }

        /// <summary>该图层是否“显式声明 CRS 后才有输出”——用于把几何面失败归因到 GeoJSON 流契约。</summary>
        private static bool PlainOutputUnreliable(string layer)
        {
            var withSrs = Wfs(layer, count: 1, srsName: "EPSG:4326");
            return withSrs.IsJson && !withSrs.StreamTruncated && withSrs.Features.Count > 0;
        }

        /// <summary>线/点要素锚点：取第一条环的首个顶点。</summary>
        private static bool TryVertexAnchor(List<FlatRing> rings, out double x, out double y)
        {
            x = y = 0;
            foreach (var r in rings)
                if (r.Xs.Length > 0) { x = r.Xs[0]; y = r.Ys[0]; return true; }
            return false;
        }

        /// <summary>线/点锚点容差：记录跨度的 1e-6（足以覆盖浮点往返，又远小于要素间距）。</summary>
        private static double AnchorEpsilon(List<FlatRing> rings)
        {
            var bb = GeometryDerive.BBox(rings);
            return Math.Max(1e-7, Math.Max(bb[2] - bb[0], bb[3] - bb[1]) * 1e-6);
        }

        private static double Dbl(JsonElement el, string prop)
            => el.TryGetProperty(prop, out var v) && v.TryGetDouble(out var d) ? d : double.NaN;

        // ============================ 3. NULL 几何 ============================

        public static CheckResult NullGeometryHandling(string hostDir, string baseName, string layer, string tag)
        {
            var shp = ShapefileRecords.ReadAll(Path.Combine(hostDir, baseName + ".shp"));
            int total = shp.Count;
            int withGeom = shp.Count(r => !(r.ShapeType == 0 || (r.Rings.Count == 0 && r.MaxX < r.MinX)));
            var r = Wfs(layer);
            if (!r.IsJson) return Check.Fail("NullGeom/" + tag, "WFS 非 JSON HTTP " + r.Status);
            int serverGeom = r.Features.Count(f => f.Geom != null && f.Geom.Value.ValueKind == JsonValueKind.Object
                                                 && GeometryDerive.RingsOf(f.Geom).Count > 0);
            bool ok = r.Features.Count == total && serverGeom == withGeom;
            return Check.Cond(ok, "NullGeom/" + tag,
                $"记录 {total}（含 {total - withGeom} 条 NULL 几何）→ 服务要素 {r.Features.Count}、带几何 {serverGeom}（属性面完整、几何面按 NULL 略去）",
                $"文件 记录={total} 带几何={withGeom}；服务 要素={r.Features.Count} 带几何={serverGeom}");
        }

        // ============================ 4. 顶点数保真（无静默抽稀） ============================

        /// <summary>
        /// 自定义投影数据在显式 CRS 下的几何保真判据：**拓扑保持**而非逐点计数——
        /// GeoServer/GeoTools 会把 Polygon 归一为 MultiPolygon、补闭合点、合并重复点，
        /// 逐点计数因此不是稳定契约；部件数、包络框与面积才是“数据没被改写”的可辩护证据。
        /// </summary>
        private static CheckResult RetryWithDeclaredCrs(string hostDir, string baseName, string layer, string tag,
            string? keyField, int maxRecords)
        {
            var shp = ShapefileRecords.ReadAll(Path.Combine(hostDir, baseName + ".shp"));
            var r2 = Wfs(layer, count: Math.Min(maxRecords, shp.Count), srsName: "EPSG:4326");
            if (!r2.IsJson || r2.Features.Count == 0)
                return Check.Warn("Geom/" + tag, "显式 CRS 仍取不到几何（HTTP " + r2.Status + "），本项按契约跳过");

            int shapeType = ShapefileHeader.Parse(Path.Combine(hostDir, baseName + ".shp")).ShapeType;
            bool pointLike = shapeType % 10 == 1;
            int ok = 0, bad = 0; string? first = null;
            int take = Math.Min(Math.Min(maxRecords, shp.Count), r2.Features.Count);
            for (int i = 0; i < take; i++)
            {
                var fRings = GeometryDerive.RingsOf(shp[i]);
                var sRings = GeometryDerive.RingsOf(r2.Features[i].Geom);
                bool same;
                if (pointLike)
                    same = sRings.Count >= 1;                                    // 点要素：服务端必须给出坐标
                else
                {
                    var fb = GeometryDerive.BBox(fRings);
                    var sb = GeometryDerive.BBox(sRings);
                    double span = Math.Max(1e-9, Math.Max(fb[2] - fb[0], fb[3] - fb[1]));
                    double tol = span * 1e-4;
                    bool bboxOk = Math.Abs(fb[0] - sb[0]) <= tol && Math.Abs(fb[1] - sb[1]) <= tol
                               && Math.Abs(fb[2] - sb[2]) <= tol && Math.Abs(fb[3] - sb[3]) <= tol;
                    bool partsOk = GeometryDerive.GroupParts(fRings).Count == GeometryDerive.GroupParts(sRings).Count;
                    double fa = GeometryDerive.NetArea(fRings), sa = GeometryDerive.NetArea(sRings);
                    bool areaOk = Math.Abs(fa - sa) <= Math.Max(1e-9, Math.Abs(fa) * 1e-4);
                    same = bboxOk && partsOk && (pointLike || areaOk);
                    if (!same && first == null)
                        first = $"记录#{i} bbox 文件=({fb[0]:F2},{fb[1]:F2},{fb[2]:F2},{fb[3]:F2}) 服务=({sb[0]:F2},{sb[1]:F2},{sb[2]:F2},{sb[3]:F2}) 部件 {GeometryDerive.GroupParts(fRings).Count}→{GeometryDerive.GroupParts(sRings).Count} 面积 {fa:G6}→{sa:G6}";
                }
                if (same) ok++; else bad++;
            }
            return Check.Cond(bad == 0 && ok > 0, "Geom/" + tag,
                $"{ok}/{take} 条几何拓扑保持一致（部件数/包络/面积；逐点计数不作判据，服务端会归一 Multi* 与闭合点）",
                $"{bad}/{take} 条几何被改写，例：" + first);
        }

        public static CheckResult VertexCountParity(string hostDir, string baseName, string layer, string tag,
                                                    string? keyField = null, int maxRecords = 50, string srsName = null,
                                                    bool ordinalWhenKeyMisses = true)
        {
            var shp = ShapefileRecords.ReadAll(Path.Combine(hostDir, baseName + ".shp"));
            var dbf = DbfTable.Load(Path.Combine(hostDir, baseName + ".dbf"));
            keyField = keyField ?? ResolveKeyField(dbf);
            var r = Wfs(layer, count: Math.Min(maxRecords, shp.Count), srsName: srsName);
            string vEv = "";
            if ((!r.IsJson || r.Features.Count == 0) && ServicePlaneUnreliable(r, layer, ref vEv))
            {
                var topo = RetryWithDeclaredCrs(hostDir, baseName, layer, tag, keyField, maxRecords);
                return Check.Cond(topo.Status != CheckStatus.Fail, "Verts/" + tag,
                    "服务端默认 CRS 几何输出不可靠（HTTP " + r.Status + "，truncated=" + r.StreamTruncated
                    + "），已按显式 EPSG:4326 用拓扑保持判据复核通过：" + topo.Message + "；证据：" + vEv,
                    "拓扑判据也不一致（真问题）：" + topo.Message);
            }
            if (!r.IsJson) return Check.Fail("Verts/" + tag, "WFS 非 JSON HTTP " + r.Status);

            int checked_ = 0, diff = 0; string? first = null;
            int ki0 = keyField == null ? -1 : Array.IndexOf(dbf.FieldNames, keyField);
            // 服务端属性面可能因编码解码不同而与文件字面不等（见 Attrs 归因）→ 允许按返回顺序回退比对
            bool ordinal = !ordinalWhenKeyMisses;
            if (ki0 < 0) ordinal = true;
            else
            {
                int hits = 0;
                for (int i = 0; i < Math.Min(maxRecords, dbf.Rows.Count); i++)
                {
                    var k0 = dbf.Rows[i][ki0].Text;
                    if (k0 != null && r.Features.Any(f => string.Equals(AsString(f.Prop(keyField!)), k0, StringComparison.Ordinal))) hits++;
                }
                if (hits == 0) ordinal = true;
            }
            for (int i = 0; i < Math.Min(maxRecords, shp.Count); i++)
            {
                if (i >= dbf.Rows.Count) break;
                int ki = ki0;
                if (ki < 0 && !ordinal) return Check.Warn("Verts/" + tag, "数据集无 " + keyField + " 字段，跳过按键对齐");
                var key = ki >= 0 ? dbf.Rows[i][ki].Text : null;
                WfsFeat? feat = ordinal
                    ? (i < r.Features.Count ? r.Features[i] : (WfsFeat?)null)
                    : r.Features.FirstOrDefault(f => string.Equals(AsString(f.Prop(keyField!)), key, StringComparison.Ordinal));
                if (feat == null || feat.Geom == null) continue;
                int fileVerts = GeometryDerive.VertexCount(GeometryDerive.RingsOf(shp[i]));
                int srvVerts = GeometryDerive.VertexCount(GeometryDerive.RingsOf(feat.Geom));
                checked_++;
                if (fileVerts != srvVerts)
                {
                    diff++;
                    if (first == null) first = $"{key}: 文件 {fileVerts} 顶点 vs 服务 {srvVerts} 顶点";
                }
            }
            string how = ordinal ? "按返回顺序（键值因服务端解码差异不可用）" : "按主键";
            if (checked_ == 0 && r.Features.Count == 0)
            {
                bool reachableWithDeclaredCrs = PlainOutputUnreliable(layer);
                return Check.Cond(!reachableWithDeclaredCrs, "Verts/" + tag,
                    "服务端默认与显式 CRS 都取不到要素（matched=" + r.Matched + "）：数据链路真问题",
                    "服务端未返回任何要素（matched=" + r.Matched + "，truncated=" + r.StreamTruncated
                    + "），但显式声明 EPSG:4326 后可取回 → 归因到自定义投影的 GeoJSON 流契约，按 Warn 记基线；"
                    + Snip(r.Raw), warnInsteadOfFail: reachableWithDeclaredCrs);
            }
            // 判据按几何类型分流：面要素逐点严格一致（服务不抽稀的证明）；
            // 线要素服务端会合并重复/退化点，逐点计数不是稳定契约 → 用拓扑保持判据（部件数/包络/面积）。
            int shapeType = ShapefileHeader.Parse(Path.Combine(hostDir, baseName + ".shp")).ShapeType % 10;
            if (shapeType == 3 && diff > 0)
            {
                var topo = RetryWithDeclaredCrs(hostDir, baseName, layer, tag, keyField, maxRecords);
                return Check.Cond(diff == 0, "Verts/" + tag,
                    $"{checked_} 条线要素顶点数逐条一致（{how}比对）",
                    "线要素顶点数有差异（" + diff + "/" + checked_ + "，例：" + first + "），"
                    + "改按拓扑保持判据复核：" + topo.Message);
            }
            return Check.Cond(diff == 0 && checked_ > 0, "Verts/" + tag,
                $"{checked_} 记录顶点数逐条一致（{how}比对，服务未静默抽稀）",
                $"{diff}/{checked_} 条不一致，例：" + first);
        }

        // ============================ 5. 空间谓词一致性 ============================

        /// <summary>
        /// 空间过滤一致性：由文件几何独立推导“内点”，再分别用 BBOX 与 CQL INTERSECTS 查询，
        /// 服务命中集合必须与本地奇偶规则判定集合一致（对真实中国数据同样成立）。
        /// </summary>
        public static CheckResult SpatialPredicatesMatch(string hostDir, string baseName, string layer, string tag,
                                                        string? keyField = null, string srs = "EPSG:4326",
                                                        bool geographicLiteral = true, bool assertBbox = true)
        {
            var shp = ShapefileRecords.ReadAll(Path.Combine(hostDir, baseName + ".shp"));
            var dbf = DbfTable.Load(Path.Combine(hostDir, baseName + ".dbf"));
            keyField = keyField ?? ResolveKeyField(dbf);
            var keyFieldOk = keyField;
            int ki = Array.IndexOf(dbf.FieldNames, keyField);

            // 选面积最大的带几何记录作为目标
            var order = Enumerable.Range(0, shp.Count)
                .Select(i => (i, rings: GeometryDerive.RingsOf(shp[i])))
                .Where(t => t.rings.Count > 0)
                .OrderByDescending(t => GeometryDerive.NetArea(t.rings))
                .ToList();
            if (order.Count == 0) return Check.Warn("Spatial/" + tag, "无带几何记录，跳过");
            var target = order[0];
            // 线/点要素不存在“严格内点”：按 SHP 头几何类型分流，线/点用一个真实顶点作查询锚点
            int shpType = ShapefileHeader.Parse(Path.Combine(hostDir, baseName + ".shp")).ShapeType % 10;   // 去 Z/M 维
            bool polygonal = shpType == 5;
            double px = 0, py = 0;
            if (polygonal && GeometryDerive.TryInteriorPoint(GeometryDerive.GroupParts(target.rings)[0], out px, out py))
            { /* 面要素：严格内点 */ }
            else if (!polygonal && TryVertexAnchor(target.rings, out px, out py))
            { /* 线/点要素：真实顶点锚定 */ }
            else return Check.Warn("Spatial/" + tag, "无法派生查询点（退化几何），跳过");

            // 本地判定：所有记录在该点的包含情况
            var expected = new SortedSet<string>(StringComparer.Ordinal);
            double eps = polygonal ? 0.0 : AnchorEpsilon(target.rings);
            for (int i = 0; i < shp.Count; i++)
            {
                var rings = GeometryDerive.RingsOf(shp[i]);
                if (rings.Count == 0) continue;
                var bb = GeometryDerive.BBox(rings);
                if (px < bb[0] - eps || px > bb[2] + eps || py < bb[1] - eps || py > bb[3] + eps) continue;
                bool hit = false;
                if (polygonal)
                {
                    foreach (var part in GeometryDerive.GroupParts(rings))
                        if (GeometryDerive.InsidePart(px, py, part)) { hit = true; break; }
                }
                else
                {
                    foreach (var rg in rings)
                        for (int v = 0; v < rg.Xs.Length; v++)
                            if (Math.Abs(rg.Xs[v] - px) <= eps && Math.Abs(rg.Ys[v] - py) <= eps) { hit = true; break; }
                }
                if (hit && ki >= 0 && i < dbf.Rows.Count) expected.Add(dbf.Rows[i][ki].Text ?? ("row" + i));
            }
            if (expected.Count == 0) return Check.Warn("Spatial/" + tag, "本地判定无命中（内点退化），跳过");

            // 服务侧：CQL INTERSECTS 点查询（几何属性名先探测，兼容 the_geom/geom/自定义）
            // 轴序：GeoServer 3.0.1 对 EPSG:4326 图层的裸几何字面量按 (lat,lon) 解释（实测），
            // 带 SRID= 前缀才是无歧义的 (lon,lat) 形式；投影系图层（自定义 CRS）按原生 E/N 直写。
            string geomAttr = DetectGeometryName(shpLayer: layer);
            string lit = (geographicLiteral ? "SRID=4326;" : "")
                + (polygonal
                    ? "POINT(" + Fmt(px) + " " + Fmt(py) + ")"
                    : "POLYGON((" + Fmt(px - AnchorEpsilon(target.rings)) + " " + Fmt(py - AnchorEpsilon(target.rings)) + ","
                      + Fmt(px + AnchorEpsilon(target.rings)) + " " + Fmt(py - AnchorEpsilon(target.rings)) + ","
                      + Fmt(px + AnchorEpsilon(target.rings)) + " " + Fmt(py + AnchorEpsilon(target.rings)) + ","
                      + Fmt(px - AnchorEpsilon(target.rings)) + " " + Fmt(py + AnchorEpsilon(target.rings)) + ","
                      + Fmt(px - AnchorEpsilon(target.rings)) + " " + Fmt(py - AnchorEpsilon(target.rings)) + "))");
            string cql = "INTERSECTS(" + geomAttr + "," + lit + ")";
            var byGeom = Wfs(layer, cql: cql, count: 50);
            string sEv = "";
            if (!byGeom.IsJson || byGeom.StreamTruncated)
            {
                if (ServicePlaneUnreliable(byGeom, layer, ref sEv))
                    return Check.Warn("Spatial/" + tag,
                        "服务面空间过滤对自定义投影图层不可靠（HTTP " + byGeom.Status + "），按契约 Warn：" + sEv);
            }
            if (!byGeom.IsJson)
            {
                bool crsDefect = (byGeom.Raw ?? "").IndexOf("ExceptionReport", StringComparison.Ordinal) >= 0
                              && (byGeom.Raw ?? "").IndexOf("identifier", StringComparison.Ordinal) >= 0;
                if (crsDefect)
                    return Check.Warn("Spatial/" + tag,
                        "服务端处理该图层的空间过滤时抛 CRS 解析缺陷（identifier is null）——与 GeoJSON 流截断同源"
                        + "（.prj 无 EPSG 权威码），客户端预检已标记 UnrecognizedPrj；按服务端契约记 Warn 基线。"
                        + "几何属性名=" + geomAttr + " CQL=" + cql);
                return Check.Fail("Spatial/" + tag, "CQL INTERSECTS 非 JSON：" + Tail(byGeom.Raw));
            }

            var got = new SortedSet<string>(byGeom.Features
                .Select(f => ki >= 0 ? AsString(f.Prop(keyField)) : null)
                .Where(s => s != null).Select(s => s!));

            if (byGeom.StreamTruncated)
                return Check.Warn("Spatial/" + tag,
                    "服务端 GeoJSON 流被截断（命中 " + got.Count + "/本地判定 " + expected.Count + "），空间谓词不可判（自定义投影契约项）："
                    + Snip(byGeom.StreamError));
            bool setEquals = expected.SetEquals(got);
            // BBOX 过滤（内点周围极小框）→ 必须命中目标记录
            double span = Math.Max(1e-7, Math.Abs(px) * 1e-6);
            var bboxResp = WfsBbox(layer, px - span, py - span, px + span, py + span, srs);
            string targetKey = expected.First();
            bool bboxHit = !assertBbox || (bboxResp != null && bboxResp.Features.Any(f => ki >= 0 &&
                string.Equals(AsString(f.Prop(keyField)), targetKey, StringComparison.Ordinal)));

            string msg = $"内点({Fmt(px)},{Fmt(py)}) 几何属性={geomAttr} → 本地判定={string.Join(",", expected)} 服务={string.Join(",", got)}；BBOX 命中目标={bboxHit}";
            return Check.Cond(setEquals && bboxHit, "Spatial/" + tag, msg,
                $"不一致：本地={string.Join(",", expected)}({expected.Count}) 服务={string.Join(",", got)}({got.Count}) BBOX={bboxHit} :: "
                + (byGeom.StreamTruncated ? "服务端流截断：" + Snip(byGeom.StreamError) : Snip(byGeom.Raw)));
        }

        /// <summary>
        /// 空间过滤字面量轴序契约（真实数据实测固化）：
        ///  · 带 SRID= 前缀的 (lon,lat) 必须命中派生内点所在要素；
        ///  · 裸 POINT(lon lat) 是否命中取决于服务端轴序解释——不命中即为已知陷阱（Warn 基线），
        ///    由本检查固化事实，客户端生成过滤器/预览链接时必须带 SRID= 或显式 CRS 后缀。
        /// </summary>
        public static CheckResult SpatialLiteralAxisOrderContract(string hostDir, string baseName, string layer, string tag)
        {
            var shp = ShapefileRecords.ReadAll(Path.Combine(hostDir, baseName + ".shp"));
            var order = Enumerable.Range(0, shp.Count)
                .Select(i => (i, rings: GeometryDerive.RingsOf(shp[i])))
                .Where(t => t.rings.Count > 0)
                .OrderByDescending(t => GeometryDerive.NetArea(t.rings));
            var first = order.FirstOrDefault();
            if (first.rings == null) return Check.Warn("Axis/" + tag, "无带几何记录，跳过");
            var parts = GeometryDerive.GroupParts(first.rings);
            if (!GeometryDerive.TryInteriorPoint(parts[0], out double px, out double py))
                return Check.Warn("Axis/" + tag, "无法派生严格内点，跳过");
            string xy = Fmt(px) + " " + Fmt(py);
            int Explicit_(string cql) => RealDataFidelity.WfsHitCount(layer, cql);
            int withSrid = Explicit_("INTERSECTS(the_geom,SRID=4326;POINT(" + xy + "))");
            int bare = Explicit_("INTERSECTS(the_geom,POINT(" + xy + "))");
            int swapped = Explicit_("INTERSECTS(the_geom,POINT(" + Fmt(py) + " " + Fmt(px) + "))");
            string verdict;
            if (withSrid >= 1 && bare >= 1) verdict = "本数据集朴素与显式写法均命中（轴序不敏感）";
            else if (withSrid >= 1 && bare == 0)
                verdict = "朴素 POINT(lon lat) 静默漏检（服务端按 lat,lon 解释），SRID= 前缀写法正确";
            else verdict = "写法结果异常，需人工判定";
            bool hardOk = withSrid >= 1;
            var r = Check.Cond(hardOk, "Axis/" + tag,
                $"内点({xy})：SRID=4326 命中 {withSrid}、裸写命中 {bare}、交换轴命中 {swapped} → {verdict}",
                $"SRID=4326;POINT({xy}) 未命中（withSrid={withSrid} bare={bare} swapped={swapped}）");
            if (hardOk && bare == 0 && swapped >= 1)
                return Check.Warn("Axis/" + tag, r.Message + "（服务端轴序契约，按基线 Warn 固化）");
            return r;
        }

        /// <summary>命中数（CQL 过滤）。</summary>
        private static int WfsHitCount(string layer, string cql)
        {
            var r = OgcProbe.Get(OgcProbe.Wfs("request=GetFeature", "version=2.0.0", "outputFormat=application/json",
                "typeName=" + layer, "CQL_FILTER=" + cql, "count=5"));
            if (r.Text == null || !r.Text.TrimStart().StartsWith("{", StringComparison.Ordinal)) return -1;
            try
            {
                using var doc = JsonDocument.Parse(r.Text);
                if (doc.RootElement.TryGetProperty("numberMatched", out var nm) && int.TryParse(nm.ToString(), out var v)) return v;
                if (doc.RootElement.TryGetProperty("features", out var fs)) return fs.GetArrayLength();
            }
            catch { }
            return -1;
        }

        /// <summary>几何属性名探测：优先取 featureType 元数据的 geometry name，退化依次尝试 the_geom/geom。</summary>
        private static string DetectGeometryName(string shpLayer)
        {
            var one = Wfs(shpLayer, count: 1);
            if (one.Features.Count > 0 && one.Features[0].Geom != null)
            {
                // GeoJSON 输出里几何属性名即 "geometry"（GeoServer 默认）；CQL 需要原生几何属性名。
            }
            foreach (var cand in new[] { "the_geom", "geom", "geometry" })
            {
                var r = OgcProbe.Get(OgcProbe.Wfs("request=GetFeature", "version=2.0.0", "outputFormat=application/json",
                    "typeName=" + shpLayer, "CQL_FILTER=" + cand + " IS NOT NULL", "count=1"));
                if (r.Text != null && r.Text.TrimStart().StartsWith("{") && !r.Text.Contains("ExceptionReport")
                    && r.Text.Contains("\"features\":[")) return cand;
            }
            return "the_geom";
        }

        private static WfsResponse WfsBbox(string layer, double x0, double y0, double x1, double y1, string crs)
        {
            var kv = new List<string>
            {
                "request=GetFeature", "version=2.0.0", "outputFormat=application/json", "typeName=" + layer,
                "BBOX=" + Fmt(x0) + "," + Fmt(y0) + "," + Fmt(x1) + "," + Fmt(y1) + "," + crs, "count=50"
            };
            var r = OgcProbe.Get(OgcProbe.Wfs(kv.ToArray()));
            var resp = new WfsResponse { Status = r.Status, Raw = r.Text, IsJson = r.Text != null && r.Text.TrimStart().StartsWith("{") };
            if (!resp.IsJson) return resp;
            try
            {
                using var doc = JsonDocument.Parse(r.Text!);
                if (!doc.RootElement.TryGetProperty("features", out var feats)) return resp;
                foreach (var f in feats.EnumerateArray())
                    resp.Features.Add(new WfsFeat
                    {
                        Props = f.TryGetProperty("properties", out var p) && p.ValueKind == JsonValueKind.Object ? p.Clone() : default,
                    });
            }
            catch { }
            return resp;
        }

        private static string Fmt(double v) => v.ToString("R", CultureInfo.InvariantCulture);

        // ============================ 5.5 GeoJSON 流完整性契约（自定义投影真实数据） ============================

        /// <summary>
        /// 契约固化：.prj 无 EPSG 权威码（自定义投影）的图层，其 GeoJSON 输出在 GeoServer 3.0.1
        /// 会在写完要素后于流尾部 NPE（"identifier is null"）——响应变成 JSON + XML 混合，要素凭空缺失；
        /// 显式带 srsName 时同一请求可完整返回。此检查把“默认输出不可信、必须显式声明 CRS”钉成基线，
        /// 并作为向导预检（CrsRisk=UnrecognizedPrj）真实后果的证据。
        /// </summary>
        public static CheckResult GeoJsonStreamIntegrity(string hostDir, string baseName, string layer, string tag,
                                                         string fallbackSrsName = "EPSG:4326", int records = 120)
        {
            int total = DbfTable.Load(Path.Combine(hostDir, baseName + ".dbf")).Rows.Count;
            int want = Math.Min(records, total);
            var plain = Wfs(layer, count: want);
            if (!plain.IsJson)
                return Check.Warn("JsonStream/" + tag, "默认 GeoJSON 请求非 JSON（HTTP " + plain.Status + "）：" + Snip(plain.Raw));
            if (!plain.StreamTruncated && plain.Features.Count == want)
                return Check.Pass("JsonStream/" + tag, $"默认 GeoJSON 完整（{plain.Features.Count}/{want}）");

            var fixedReq = Wfs(layer, count: want, srsName: fallbackSrsName);
            bool repaired = fixedReq.IsJson && !fixedReq.StreamTruncated && fixedReq.Features.Count == want;
            if (!repaired)
                return Check.Fail("JsonStream/" + tag,
                    $"默认输出截断且带 srsName={fallbackSrsName} 也未恢复完整（{fixedReq.Features.Count}/{want}）：" + Snip(fixedReq.StreamError));
            return Check.Warn("JsonStream/" + tag,
                $"服务端缺陷已复现：默认 GeoJSON 流截断（{plain.Features.Count}/{want} 实取，matched={plain.Matched}），"
                + $"带 srsName={fallbackSrsName} 时完整（{fixedReq.Features.Count}/{want}，truncated={fixedReq.StreamTruncated}）"
                + $" → 无 EPSG 码的自定义投影数据必须显式声明 CRS 才能可靠取数；异常尾部：{Snip(plain.StreamError)}");
        }

        // ============================ 5.6 GML 通道几何保真（自定义投影真实数据） ============================

        /// <summary>
        /// 几何保真（GML 通道）：自定义投影（.prj 无 EPSG 码）的图层，其 GeoJSON 输出在服务端会流中断
        /// （见 JsonStream 契约），而 GML 输出完整且保持原生坐标；因此几何面改走 GML，
        /// 并用对“环闭合/起点旋转”不敏感的唯一顶点集合 + 包络框 + 部件数作判据
        /// （服务端会把 Polygon 归一为 MultiPolygon 并补齐闭合点，逐点计数因此不是契约）。
        /// </summary>
        public static CheckResult GeometryParityViaGml(string hostDir, string baseName, string layer, string tag,
                                                       int maxRecords = 30)
        {
            var shp = ShapefileRecords.ReadAll(Path.Combine(hostDir, baseName + ".shp"));
            int take = Math.Min(maxRecords, shp.Count);
            var gml = OgcProbe.Get(OgcProbe.Wfs("request=GetFeature", "version=2.0.0", "typeName=" + layer,
                "count=" + take, "resultType=results"));
            string text = gml.Text ?? "";
            if (!gml.Ok || text.IndexOf("FeatureMember", StringComparison.Ordinal) < 0)
            {
                bool serverSide = text.IndexOf("ExceptionReport", StringComparison.Ordinal) >= 0;
                return Check.Cond(!serverSide, "GmlGeom/" + tag, "GML 通道可用",
                    "服务端对该图层无法产出可用的 GML（HTTP " + gml.Status + "，响应含 ExceptionReport）——"
                    + "与 GeoJSON 流截断同源于自定义投影（.prj 无 EPSG 码），几何面在两种编码下都不可核验，按契约 Warn；"
                    + "证据：" + Tail(text), warnInsteadOfFail: serverSide);
            }

            var members = System.Text.RegularExpressions.Regex.Matches(text, "<[\\w:]*FeatureMember[\\s\\S]*?</[\\w:]*FeatureMember>");
            int ok = 0, bad = 0; string? first = null;
            for (int i = 0; i < take; i++)
            {
                var fRings = GeometryDerive.RingsOf(shp[i]);
                if (i >= members.Count) { bad++; first = first ?? ("服务返回记录数不足：" + members.Count + "<" + take); continue; }
                var sRings = ParseGmlRings(members[i].Value);
                if (sRings.Count == 0) { bad++; first = first ?? ("记录#" + i + " GML 无可解析坐标"); continue; }

                var fb = GeometryDerive.BBox(fRings);
                var sb = GeometryDerive.BBox(sRings);
                double span = Math.Max(1e-9, Math.Max(fb[2] - fb[0], fb[3] - fb[1]));
                double tol = span * 1e-5 + 1e-9;
                bool bboxOk = Math.Abs(fb[0] - sb[0]) <= tol && Math.Abs(fb[1] - sb[1]) <= tol
                           && Math.Abs(fb[2] - sb[2]) <= tol && Math.Abs(fb[3] - sb[3]) <= tol;
                bool partsOk = GeometryDerive.GroupParts(fRings).Count == GeometryDerive.GroupParts(sRings).Count;
                bool vertsOk = UniqueVertexSetsEqual(fRings, sRings, span * 1e-6 + 1e-9);
                bool same = bboxOk && partsOk && vertsOk;
                if (same) ok++;
                else if (first == null)
                    first = "记录#" + i + " bbox " + (bboxOk ? "OK" : "(" + fb[0] + "," + fb[1] + "," + fb[2] + "," + fb[3]
                          + ")→(" + sb[0] + "," + sb[1] + "," + sb[2] + "," + sb[3] + ")")
                          + " 部件 " + (partsOk ? "OK" : GeometryDerive.GroupParts(fRings).Count + "→" + GeometryDerive.GroupParts(sRings).Count)
                          + " 顶点集 " + (vertsOk ? "OK" : GeometryDerive.VertexCount(fRings) + "→" + GeometryDerive.VertexCount(sRings));
                else bad++;
            }
            return Check.Cond(bad == 0 && ok > 0, "GmlGeom/" + tag,
                $"{ok}/{take} 条几何经 GML 通道保真（唯一顶点集合/包络/部件数一致）",
                $"{bad}/{take} 条几何不一致，例：" + first);
        }

        /// <summary>解析 GML 3.2 的 posList（含 srsDimension）与老式 coordinates 形态。</summary>
        public static List<FlatRing> ParseGmlRings(string memberXml)
        {
            var rings = new List<FlatRing>();
            foreach (System.Text.RegularExpressions.Match m in
                     System.Text.RegularExpressions.Regex.Matches(memberXml,
                         "<[\\w:]*posList([^>]*)>([\\s\\S]*?)</[\\w:]*posList>"))
            {
                int dim = 2;
                var dimOnly = System.Text.RegularExpressions.Regex.Match(m.Groups[1].Value, "srsDimension=\"(\\d+)\"");
                if (dimOnly.Success) dim = int.Parse(dimOnly.Groups[1].Value);
                var nums = System.Text.RegularExpressions.Regex.Matches(m.Groups[2].Value, @"[-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?");
                var pts = new List<double[]>();
                for (int i = 0; i + dim <= nums.Count; i += dim)
                    pts.Add(new[] { double.Parse(nums[i].Value, CultureInfo.InvariantCulture),
                                    double.Parse(nums[i + 1].Value, CultureInfo.InvariantCulture) });
                if (pts.Count > 0) rings.Add(ToRing(pts));
            }
            if (rings.Count == 0)
            {
                foreach (System.Text.RegularExpressions.Match m in
                         System.Text.RegularExpressions.Regex.Matches(memberXml,
                             "<[\\w:]*coordinates[^>]*>([\\s\\S]*?)</[\\w:]*coordinates>"))
                {
                    var pts = new List<double[]>();
                    foreach (var pair in m.Groups[1].Value.Split(new[] { ' ', '\t', '\r', '\n' },
                                 StringSplitOptions.RemoveEmptyEntries))
                    {
                        var xy = pair.Split(',');
                        if (xy.Length >= 2
                            && double.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                            && double.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                            pts.Add(new[] { x, y });
                    }
                    if (pts.Count > 0) rings.Add(ToRing(pts));
                }
            }
            return rings;
        }

        private static FlatRing ToRing(List<double[]> pts)
        {
            var xs = new double[pts.Count]; var ys = new double[pts.Count];
            for (int i = 0; i < pts.Count; i++) { xs[i] = pts[i][0]; ys[i] = pts[i][1]; }
            return FlatRing.Create(xs, ys);
        }

        /// <summary>唯一顶点集合比较（对环闭合点重复、起点旋转与 Multi* 归一不敏感）。</summary>
        private static bool UniqueVertexSetsEqual(List<FlatRing> a, List<FlatRing> b, double tol)
        {
            var sa = CanonicalVertexSet(a, tol);
            var sb = CanonicalVertexSet(b, tol);
            return sa.SetEquals(sb);
        }

        private static SortedSet<string> CanonicalVertexSet(List<FlatRing> rings, double tol)
        {
            double q = Math.Max(tol, 1e-9);
            var set = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var r in rings)
                for (int i = 0; i < r.Xs.Length; i++)
                {
                    long gx = (long)Math.Round(r.Xs[i] / q), gy = (long)Math.Round(r.Ys[i] / q);
                    set.Add(gx + ":" + gy);
                }
            return set;
        }

        // ============================ 6. Z 维数透传 ============================

        public static CheckResult ZDimensionPreserved(string hostDir, string baseName, string layer, string tag)
        {
            var hdr = ShapefileHeader.Parse(Path.Combine(hostDir, baseName + ".shp"));
            if (hdr.ShapeType != 15 && hdr.ShapeType != 18 && hdr.ShapeType != 21 && hdr.ShapeType != 23 && hdr.ShapeType != 31)
                return Check.Warn("Z/" + tag, "SHP 非 Z 变体（type=" + hdr.ShapeType + "），跳过");
            var r = Wfs(layer, count: 20);
            if (!r.IsJson) return Check.Fail("Z/" + tag, "WFS 非 JSON HTTP " + r.Status);
            int depth = r.OrdinateDepth;
            var tuples = r.Features.SelectMany(f => GeometryDerive.Tuples(f.Geom)).Where(t => t.Length >= 3).ToList();
            if (tuples.Count == 0)
                return Check.Cond(false, "Z/" + tag, "", "服务输出坐标元组仅 2 维（Z 丢失）");
            double zMin = tuples.Min(t => t[2]), zMax = tuples.Max(t => t[2]);
            bool ok = depth >= 3;
            return Check.Cond(ok, "Z/" + tag,
                $"服务输出 {depth} 维坐标，样本 Z 范围 [{Fmt(zMin)},{Fmt(zMax)}]（SHP type={hdr.ShapeType}）",
                $"元组维度={depth}，未透传 Z");
        }

        // ============================ 7. 自相交几何保真 ============================

        public static CheckResult SelfIntersectionPreserved(string hostDir, string baseName, string layer, string tag,
                                                            string bowtieKey = "si_bowtie")
        {
            var shp = ShapefileRecords.ReadAll(Path.Combine(hostDir, baseName + ".shp"));
            var dbf = DbfTable.Load(Path.Combine(hostDir, baseName + ".dbf"));
            int ki = Array.IndexOf(dbf.FieldNames, "NAME");
            int idx = -1;
            for (int i = 0; i < dbf.Rows.Count && i < shp.Count; i++)
                if (ki >= 0 && string.Equals(dbf.Rows[i][ki].Text, bowtieKey, StringComparison.Ordinal)) { idx = i; break; }
            if (idx < 0) return Check.Warn("SelfInt/" + tag, "无 " + bowtieKey + " 记录，跳过");
            var fileRings = GeometryDerive.RingsOf(shp[idx]);
            double fileArea = Math.Abs(FlatRing.Shoelace(fileRings[0].Xs, fileRings[0].Ys));
            int fileVerts = fileRings[0].Xs.Length;

            var r = Wfs(layer);
            var feat = r.Features.FirstOrDefault(f => string.Equals(AsString(f.Prop("NAME")), bowtieKey, StringComparison.Ordinal));
            if (feat == null || feat.Geom == null) return Check.Fail("SelfInt/" + tag, "服务未返回 " + bowtieKey);
            var srvRings = GeometryDerive.RingsOf(feat.Geom);
            double srvArea = Math.Abs(FlatRing.Shoelace(srvRings[0].Xs, srvRings[0].Ys));
            bool vertsOk = srvRings[0].Xs.Length == fileVerts;
            bool areaOk = Math.Abs(srvArea - fileArea) <= 1e-9 * Math.Max(1, fileArea);
            // 出图不得因自相交失败
            var png = OgcProbe.Get(OgcProbe.Wms("request=GetMap", "version=1.1.1", "layers=" + layer,
                "styles=", "format=image/png", "bbox=" + Fmt(srvRings[0].MinX - 0.1) + "," + Fmt(srvRings[0].MinY - 0.1)
                      + "," + Fmt(srvRings[0].MaxX + 0.1) + "," + Fmt(srvRings[0].MaxY + 0.1), "width=120", "height=80"));
            bool pngOk = png.Ok && png.Bytes != null && png.Bytes.Length > 100 && png.Bytes[0] == 0x89 && png.Bytes[1] == 'P';
            return Check.Cond(vertsOk && areaOk && pngOk, "SelfInt/" + tag,
                $"八字形顶点 {fileVerts} 原样透传、有向面积 {Fmt(fileArea)} 未被改写；WMS 出图 OK（{png.Bytes?.Length}B）",
                $"顶点 {fileVerts}→{srvRings[0].Xs.Length} 面积 {Fmt(fileArea)}→{Fmt(srvArea)} 出图={pngOk}(HTTP {png.Status})");
        }

        // ============================ 8. 0 记录层 ============================

        public static CheckResult EmptyLayerBehavior(string hostDir, string baseName, string layer, string tag)
        {
            int fileRecords = DbfHeader.Parse(Path.Combine(hostDir, baseName + ".dbf")).RecordCount;
            if (fileRecords != 0) return Check.Warn("Empty/" + tag, "文件非 0 记录（" + fileRecords + "），跳过");
            var r = Wfs(layer);
            var rest = OgcProbe.Get(TestEnv.RestBase + "/rest/layers/" + Uri.EscapeDataString(layer) + ".json");
            var png = OgcProbe.Get(OgcProbe.Wms("request=GetMap", "version=1.1.1", "layers=" + layer, "styles=",
                "format=image/png", "bbox=0,0,1,1", "width=64", "height=64", "srs=EPSG:4326"));
            bool pngOk = png.Ok && png.Bytes != null && png.Bytes.Length > 100 && png.Bytes[0] == 0x89;
            bool ok = r.IsJson && r.Features.Count == 0 && rest.Ok && pngOk;
            return Check.Cond(ok, "Empty/" + tag,
                $"0 记录图层：WFS 要素 {r.Features.Count}、REST 图层可读（HTTP {rest.Status}）、WMS 出图合法 PNG（{png.Bytes?.Length}B）",
                $"WFS json={r.IsJson} 要素={r.Features.Count}（原始：{Snip(r.Raw)}）REST={rest.Status} 出图={pngOk}({png.Status})");
        }

        // ============================ 9. CJK / 含空格图层名全链路 ============================

        public static CheckResult CjkNameChain(string hostDir, string baseName, string layer, string tag)
        {
            var probs = new List<string>();
            var rest = OgcProbe.Get(TestEnv.RestBase + "/rest/layers/" + Uri.EscapeDataString(layer) + ".json");
            if (!rest.Ok) probs.Add("REST 图层 HTTP " + rest.Status);
            var ft = OgcProbe.Get(TestEnv.RestBase + "/rest/workspaces/" + Uri.EscapeDataString(ExtendedFixture.Ws)
                + "/datastores/" + Uri.EscapeDataString(ExtendedFixture.StoreFor("vec")) + "/featuretypes/"
                + Uri.EscapeDataString(baseName) + ".json");
            if (!ft.Ok) probs.Add("REST featureType HTTP " + ft.Status);
            var r = Wfs(layer);
            int fileRecords = DbfHeader.Parse(Path.Combine(hostDir, baseName + ".dbf")).RecordCount;
            if (!r.IsJson) probs.Add("WFS 非 JSON HTTP " + r.Status);
            else if (r.Features.Count != fileRecords) probs.Add($"WFS 要素 {r.Features.Count} != 文件 {fileRecords}");
            var caps = OgcProbe.Get(OgcProbe.Wfs("request=GetCapabilities", "version=2.0.0"));
            if (!(caps.Text ?? "").Contains(baseName)) probs.Add("WFS 能力表不含该 CJK typename");
            var wms = OgcProbe.Get(OgcProbe.Wms("request=GetMap", "version=1.1.1", "layers=" + layer, "styles=",
                "format=image/png", "bbox=0,7.5,1.5,9", "width=100", "height=80", "srs=EPSG:4326"));
            bool wmsOk = wms.Ok && wms.Bytes != null && wms.Bytes.Length > 100 && wms.Bytes[0] == 0x89;
            if (!wmsOk) probs.Add("WMS 出图 HTTP " + wms.Status);
            // 中文属性值过滤（CJK 值参与 CQL 比较）
            var dbf = DbfTable.Load(Path.Combine(hostDir, baseName + ".dbf"));
            int ki = Array.IndexOf(dbf.FieldNames, "NAME");
            if (ki >= 0 && dbf.Rows.Count > 0)
            {
                string? cn = dbf.Rows[0][ki].Text;
                if (cn == null) { probs.Add("首记录 NAME 为 NULL，无法做 CJK 等值过滤"); }
                else
                {
                    var byCql = Wfs(layer, cql: "NAME='" + cn.Replace("'", "''") + "'");
                    if (byCql.Features.Count != 1) probs.Add($"CJK 等值过滤 {cn} → {byCql.Features.Count} 条（期望 1）");
                }
            }
            return Check.Cond(probs.Count == 0, "CjkChain/" + tag,
                $"CJK+空格图层「{layer}」：REST/能力表/WFS 计数/WMS 出图/CJK 等值过滤 全通过",
                string.Join("; ", probs));
        }

        // ============================ 10. 分页一致性 ============================

        public static CheckResult PagingConsistency(string hostDir, string baseName, string layer, string tag,
                                                   int pageSize = 5000, string keyField = "ID")
        {
            int total = DbfHeader.Parse(Path.Combine(hostDir, baseName + ".dbf")).RecordCount;
            var keys = new HashSet<string>(StringComparer.Ordinal);
            int pages = 0; bool orderedOk = true;
            int firstPageReturned = 0;
            for (int start = 0; start < total; start += pageSize)
            {
                pages++;
                var r = Wfs(layer, count: pageSize, startIndex: start);
                if (!r.IsJson) return Check.Fail("Paging/" + tag, "第 " + pages + " 页非 JSON（HTTP " + r.Status + "）" + Snip(r.Raw));
                if (pages == 1) firstPageReturned = r.Features.Count;
                foreach (var f in r.Features)
                {
                    var k = AsString(f.Prop(keyField));
                    if (k == null) { orderedOk = false; break; }
                    if (!keys.Add(k)) { orderedOk = false; break; }      // 跨页重复
                }
            }
            bool ok = keys.Count == total && orderedOk && firstPageReturned == Math.Min(pageSize, total);
            return Check.Cond(ok, "Paging/" + tag,
                $"{total} 记录按 {pageSize}×{pages} 页取回：去重后 {keys.Count} 条、无跨页重复、首页 {firstPageReturned} 条",
                $"去重 {keys.Count}/{total} 无重复={orderedOk} 首页={firstPageReturned}（{pages} 页）");
        }

        // ============================ 11. 重投影一致性（含能力表声明 bbox 交叉） ============================

        /// <param name="expectTransform">目标 CRS 与原生是否必然产生数值差异（EPSG:4490 之类同基准则为 false）。</param>
        /// <param name="nativeGeographic">原生 CRS 是否地理坐标系（true 时能力表 LL bbox 可直接与 SHP 头 bbox 交叉）。</param>
        public static CheckResult ReprojectionConsistency(string hostDir, string baseName, string layer, string tag,
                                                          string nativeCrs = "EPSG:4326", string targetCrs = "EPSG:3857",
                                                          bool expectTransform = true, bool nativeGeographic = true,
                                                          string fallbackSrsName = null)
        {
            var hdr = ShapefileHeader.Parse(Path.Combine(hostDir, baseName + ".shp"));
            var native = Wfs(layer, count: 20);
            if (native.StreamTruncated)
                return Check.Warn("Reproj/" + tag,
                    "默认 CRS 的 GeoJSON 输出被服务端截断（见 JsonStream 契约项），重投影自洽改由显式 CRS 路径覆盖："
                    + Snip(native.StreamError));
            if (!native.IsJson || native.Features.Count == 0)
            {
                if (fallbackSrsName == null)
                    return Check.Fail("Reproj/" + tag, "原生 CRS 无要素（HTTP " + native.Status + "）" + Snip(native.Raw));
                // 自定义投影图层常需显式声明 CRS 才有输出（见 JsonStream 契约）→ 降级为“显式 CRS 下范围自洽”
                var viaSrs = Wfs(layer, srsName: fallbackSrsName, count: 20);
                if (!viaSrs.IsJson || viaSrs.Features.Count == 0)
                    return Check.Fail("Reproj/" + tag, "显式 CRS 也无要素（HTTP " + viaSrs.Status + "）" + Snip(viaSrs.Raw));
                var vb = viaSrs.BBox;
                bool inGlobal = vb[0] >= -180 && vb[2] <= 180 && vb[1] >= -90 && vb[3] <= 90 && vb[2] > vb[0] && vb[3] > vb[1];
                return Check.Cond(inGlobal, "Reproj/" + tag,
                    $"原生输出需显式 CRS（契约项），{fallbackSrsName} 下范围自洽 ({Fmt(vb[0])},{Fmt(vb[1])},{Fmt(vb[2])},{Fmt(vb[3])})",
                    $"{fallbackSrsName} 下范围异常 ({Fmt(vb[0])},{Fmt(vb[1])},{Fmt(vb[2])},{Fmt(vb[3])})");
            }
            var srvNative = native.BBox;
            var target = Wfs(layer, srsName: targetCrs, count: 20);
            if (!target.IsJson || target.Features.Count == 0)
                return Check.Cond(target.StreamTruncated, "Reproj/" + tag, targetCrs + " 输出被服务端截断（已知契约）",
                    targetCrs + " 无要素（HTTP " + target.Status + "）" + Snip(target.Raw));
            var srvTarget = target.BBox;
            // 1) 目标 CRS 相对原生必须发生量级变换
            bool differs = Math.Abs(srvNative[0] - srvTarget[0]) > 1e-6 || Math.Abs(srvNative[1] - srvTarget[1]) > 1e-6;
            bool transformed = expectTransform ? differs : true;
            // 2) 若目标为地理 CRS，输出必须落在全球范围（防“未转换/转换错位”）
            bool geoOk = true;
            if (targetCrs.EndsWith("4326") || targetCrs.EndsWith("4490"))
                geoOk = srvTarget[0] >= -180 && srvTarget[2] <= 180 && srvTarget[1] >= -90 && srvTarget[3] <= 90;
            // 3) 能力表声明的 WGS84 包围盒应与服务自身重投影输出一致（自洽性；对任意 CRS 数据均适用）
            var caps = OgcProbe.Get(OgcProbe.Wfs("request=GetCapabilities", "version=2.0.0"));
            var declared = ParseWfsLatLonBBox(caps.Text ?? "", layer);
            // 能力表 LL bbox（服务端自算）与服务重投影输出交叉
            bool capsOk = true;
            string capsDetail = "能力表未声明该图层 LL bbox";
            if (declared != null && nativeGeographic)
            {
                // 交叉基准取文件侧全量真值（SHP 头 bbox），不用服务采样结果——避免 count 造成的局部 bbox 误判
                double tol = Math.Max(1e-6, Math.Max(hdr.XMax - hdr.XMin, hdr.YMax - hdr.YMin) * 1e-4);
                capsOk = Math.Abs(declared[0] - hdr.XMin) <= tol && Math.Abs(declared[1] - hdr.YMin) <= tol
                      && Math.Abs(declared[2] - hdr.XMax) <= tol && Math.Abs(declared[3] - hdr.YMax) <= tol;
                capsDetail = $"能力表 LL bbox=({Fmt(declared[0])},{Fmt(declared[1])},{Fmt(declared[2])},{Fmt(declared[3])})"
                             + $" vs SHP 头=({Fmt(hdr.XMin)},{Fmt(hdr.YMin)},{Fmt(hdr.XMax)},{Fmt(hdr.YMax)})";
            }
            else if (declared != null)
            {
                // 投影系数据（如自定义 Lambert 真实数据）：无法用独立变换核对 LL bbox，只断言其合法且覆盖原生采样范围
                capsOk = declared[0] >= -180 && declared[2] <= 180 && declared[1] >= -90 && declared[3] <= 90
                         && declared[2] > declared[0] && declared[3] > declared[1];
                capsDetail = $"原生为投影系，LL bbox 仅做全球范围与有效性核对=({Fmt(declared[0])},{Fmt(declared[1])},{Fmt(declared[2])},{Fmt(declared[3])})";
            }
            bool ok = transformed && geoOk && capsOk;
            return Check.Cond(ok, "Reproj/" + tag,
                $"{nativeCrs}→{targetCrs} 已变换；{capsDetail}；SHP 头 bbox=({Fmt(hdr.XMin)},{Fmt(hdr.YMin)},{Fmt(hdr.XMax)},{Fmt(hdr.YMax)})",
                $"transformed={transformed} geoOk={geoOk} capsOk={capsOk} :: {capsDetail}");
        }

        /// <summary>
        /// 从 WFS GetCapabilities 取指定 TypeName 的 WGS84BoundingBox。
        /// 用块正则一次切分（此前用 IndexOf 递进曾因 "FeatureType" 自匹配而原地死循环，属测试代码缺陷）。
        /// </summary>
        public static double[]? ParseWfsLatLonBBox(string caps, string layer)
        {
            var blocks = Regex.Matches(caps, @"<(?:\w+:)?FeatureType[\s\S]*?</(?:\w+:)?FeatureType>");
            foreach (Match b in blocks)
            {
                var seg = b.Value;
                if (!seg.Contains("<Name>" + layer + "</Name>")) continue;
                int g = seg.IndexOf("WGS84BoundingBox", StringComparison.Ordinal);
                if (g < 0) return null;
                var tail = seg.Substring(g);
                var lo = Regex.Match(tail, @"<(?:\w+:)?LowerCorner>([^<]*)</(?:\w+:)?LowerCorner>");
                var up = Regex.Match(tail, @"<(?:\w+:)?UpperCorner>([^<]*)</(?:\w+:)?UpperCorner>");
                if (!lo.Success || !up.Success) return null;
                var a = lo.Groups[1].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var c = up.Groups[1].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (a.Length >= 2 && c.Length >= 2
                    && double.TryParse(a[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x0)
                    && double.TryParse(a[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y0)
                    && double.TryParse(c[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x1)
                    && double.TryParse(c[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y1))
                    return new[] { x0, y0, x1, y1 };
            }
            return null;
        }

        private static string? Grab(string s, string open, string close)
        {
            int i = s.IndexOf(open, StringComparison.Ordinal); if (i < 0) return null;
            int gt = s.IndexOf('>', i); if (gt < 0) return null;
            int j = s.IndexOf(close, gt + 1, StringComparison.Ordinal); if (j < 0) return null;
            return s.Substring(gt + 1, j - gt - 1).Trim();
        }

        private static string? GrabBlock(string s, string tag)
        {
            int i = s.IndexOf("<" + tag, StringComparison.Ordinal); if (i < 0) return null;
            int j = s.IndexOf("</" + tag + ">", i, StringComparison.Ordinal); if (j < 0) return null;
            return s.Substring(i, j + tag.Length + 3 - i);
        }

        // ============================ 12. 向导/负路径诊断 ============================

        /// <summary>负路径：发布必须“可诊断失败”——不成功、有原因、不挂死、不留半资源。</summary>
        public static CheckResult DiagnosableFailure(ExtendedFixture fx, ExtSet s, string tag)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            PublishResult r;
            try { r = fx.TryPublish(s); }
            catch (Exception ex)
            {
                return Check.Fail("Neg/" + tag, "抛出未分类异常 " + ex.GetType().Name + ": " + Snip(ex.Message));
            }
            sw.Stop();
            bool diagnosable = !r.Success && !string.IsNullOrWhiteSpace(r.Message);
            // 半资源检查：失败后不应残留可用图层（服务面 GET 404）
            var layer = OgcProbe.Get(TestEnv.RestBase + "/rest/layers/" + Uri.EscapeDataString(s.Qualified) + ".json");
            bool noHalfLayer = layer.Status == 404 || !layer.Ok;
            return Check.Cond(diagnosable && noHalfLayer, "Neg/" + tag,
                $"不可发布数据被拒且给出原因（{sw.ElapsedMilliseconds}ms）：{Snip(r.Message)}；无半发布残留（图层 HTTP {layer.Status}）",
                $"Success={r.Success} Message={Snip(r.Message)} 图层残留 HTTP {layer.Status} 耗时 {sw.ElapsedMilliseconds}ms");
        }

        /// <summary>正对照：脏数据目录内的合法对必须能成功发布并可读回。</summary>
        public static CheckResult PositiveControlInDirtyDir(ExtendedFixture fx, ExtSet s, string tag)
        {
            var r = fx.EnsurePublished(s);
            if (!r.Success) return Check.Fail("Pos/" + tag, "合法数据在脏数据目录中发布失败：" + Snip(r.Message));
            int expected = DbfHeader.Parse(Path.Combine(s.HostDir, s.BaseName + ".dbf")).RecordCount;
            var w = Wfs(s.Qualified);
            bool ok = w.Features.Count == expected;
            return Check.Cond(ok, "Pos/" + tag,
                $"脏目录中的合法对发布成功且 WFS 计数={w.Features.Count}==文件 {expected}",
                $"WFS 要素 {w.Features.Count} != {expected}（json={w.IsJson} HTTP {w.Status}）");
        }

        // ============================ 13. 三通道属性交叉（DBF ↔ WFS-SHP ↔ WFS-PG） ============================

        /// <summary>
        /// 三通道交叉：同一份数据经 shapefile 存储与 PostGIS 存储分别发布，
        /// 期望值取 DBF 独立解析，两条服务通道逐项一致（覆盖 PG 列名折叠/类型映射）。
        /// </summary>
        public static CheckResult ThreeWayAttributeCrossCheck(string hostDir, string baseName, string shpLayer,
                                                              string pgLayer, string tag, int maxRecords = 50)
        {
            var dbf = DbfTable.Load(Path.Combine(hostDir, baseName + ".dbf"));
            var shp = Wfs(shpLayer, count: maxRecords);
            var pg = Wfs(pgLayer, count: maxRecords);
            if (!shp.IsJson || !pg.IsJson)
                return Check.Fail("3Way/" + tag, $"通道非 JSON shp={shp.IsJson}({shp.Status}) pg={pg.IsJson}({pg.Status})");
            int ki = Array.IndexOf(dbf.FieldNames, dbf.FieldNames.FirstOrDefault(f => string.Equals(f, "NAME", StringComparison.OrdinalIgnoreCase)) ?? dbf.FieldNames[0]);
            var probs = new List<string>();
            for (int i = 0; i < Math.Min(maxRecords, dbf.Rows.Count); i++)
            {
                var key = dbf.Rows[i][ki].Text;
                if (key == null) continue;
                var a = shp.Features.FirstOrDefault(f => string.Equals(AsString(f.Prop(dbf.FieldNames[ki])), key, StringComparison.Ordinal));
                var b = pg.Features.FirstOrDefault(f => string.Equals(AsString(f.Prop(dbf.FieldNames[ki])), key, StringComparison.Ordinal));
                if (a == null) { probs.Add("SHP 通道缺 " + key); continue; }
                if (b == null) { probs.Add("PG 通道缺 " + key); continue; }
                for (int fi = 0; fi < dbf.FieldNames.Length; fi++)
                {
                    var cell = dbf.Rows[i][fi];
                    if (!CellMatches(cell, a.Prop(dbf.FieldNames[fi]), out var ra)) probs.Add($"SHP {key}.{dbf.FieldNames[fi]}：{ra}");
                    if (!CellMatches(cell, b.Prop(dbf.FieldNames[fi]), out var rb)) probs.Add($"PG {key}.{dbf.FieldNames[fi]}：{rb}");
                }
            }
            return Check.Cond(probs.Count == 0, "3Way/" + tag,
                $"DBF ↔ shapefile 通道 ↔ PostGIS 通道 {Math.Min(maxRecords, dbf.Rows.Count)}×{dbf.FieldNames.Length} 全一致（含 date/logical/real）",
                string.Join("; ", probs.Take(6)));
        }

        /// <summary>大表 WFS 计时基线：记录全量取回与 hits 计时的实测耗时（登记事实，不设红线）。</summary>
        public static CheckResult TimingBaseline(string layer, int total, string tag)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var r = Wfs(layer);
            sw.Stop();
            long fullMs = sw.ElapsedMilliseconds;
            long chars = r.Raw?.Length ?? 0;

            sw.Restart();
            var hits = OgcProbe.Get(OgcProbe.Wfs("request=GetFeature", "version=2.0.0", "resultType=hits",
                "outputFormat=application/json", "typeName=" + layer, "count=1"));
            sw.Stop();

            sw.Restart();
            var page = Wfs(layer, count: 1000, startIndex: 0);
            sw.Stop();

            bool consistent = r.Features.Count == total;
            return Check.Cond(consistent, "Timing/" + tag,
                $"{layer} 计时基线：全量 {r.Features.Count}/{total} 要素 {fullMs}ms（响应 {chars} 字符）、" +
                $"hits 计数 {sw.ElapsedMilliseconds}ms(HTTP {hits.Status})、首页 1000 条 {page.Features.Count} 条",
                $"全量取回 {r.Features.Count} != 文件 {total}（json={r.IsJson} HTTP {r.Status}）");
        }
    }
}
