using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace GeoServerDesktop.Tests.RealData
{
    /// <summary>平面环（xs/ys 等长）。同一部件的外环与洞环混列，用奇偶规则统一处理。</summary>
    public sealed class FlatRing
    {
        public double[] Xs = Array.Empty<double>();
        public double[] Ys = Array.Empty<double>();
        public double MinX, MinY, MaxX, MaxY;
        public double SignedArea;

        public static FlatRing Create(double[] xs, double[] ys)
        {
            var r = new FlatRing { Xs = xs, Ys = ys };
            r.MinX = double.MaxValue; r.MinY = double.MaxValue;
            r.MaxX = double.MinValue; r.MaxY = double.MinValue;
            for (int i = 0; i < xs.Length; i++)
            {
                if (xs[i] < r.MinX) r.MinX = xs[i];
                if (xs[i] > r.MaxX) r.MaxX = xs[i];
                if (ys[i] < r.MinY) r.MinY = ys[i];
                if (ys[i] > r.MaxY) r.MaxY = ys[i];
            }
            r.SignedArea = Shoelace(xs, ys);
            return r;
        }

        public static double Shoelace(double[] xs, double[] ys)
        {
            int n = xs.Length; if (n < 3) return 0;
            double s = 0;
            for (int i = 0, j = n - 1; i < n; j = i++) s += (xs[j] + xs[i]) * (ys[j] - ys[i]);
            return s / 2.0;
        }
    }

    /// <summary>
    /// 独立几何推导：从 shapefile 记录或服务侧 GeoJSON 得到“部件（外环+洞）”集合，
    /// 并提供奇偶内点判定、扫描线内点派生、顶点计数与面积归并。全部纯实现，不经 GDAL/JTS/GeoServer。
    /// </summary>
    public static class GeometryDerive
    {
        /// <summary>把环列表按“包含关系”分组为部件（外层环 + 其洞）。</summary>
        public static List<List<FlatRing>> GroupParts(IReadOnlyList<FlatRing> rings)
        {
            var ordered = rings.OrderByDescending(r => Math.Abs(r.SignedArea)).ToList();
            var parts = new List<List<FlatRing>>();
            var owner = new int[ordered.Count];
            for (int i = 0; i < ordered.Count; i++) owner[i] = -1;
            for (int i = 0; i < ordered.Count; i++)
            {
                if (owner[i] >= 0) continue;
                var part = new List<FlatRing> { ordered[i] };
                owner[i] = i;
                for (int j = i + 1; j < ordered.Count; j++)
                {
                    if (owner[j] >= 0) continue;
                    if (InsideRing(ordered[j].Xs[0], ordered[j].Ys[0], ordered[i]))
                    { part.Add(ordered[j]); owner[j] = i; }
                }
                parts.Add(part);
            }
            return parts;
        }

        /// <summary>奇偶规则点内判定（单环）。</summary>
        public static bool InsideRing(double x, double y, FlatRing r)
        {
            bool inside = false;
            int n = r.Xs.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                double xi = r.Xs[i], yi = r.Ys[i], xj = r.Xs[j], yj = r.Ys[j];
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi + double.Epsilon) + xi)
                    inside = !inside;
            }
            return inside;
        }

        /// <summary>奇偶规则点内判定（部件：外环 + 洞，洞自动抵消）。</summary>
        public static bool InsidePart(double x, double y, IReadOnlyList<FlatRing> part)
        {
            int crossings = 0;
            foreach (var r in part)
            {
                int n = r.Xs.Length;
                for (int i = 0, j = n - 1; i < n; j = i++)
                {
                    double yi = r.Ys[i], yj = r.Ys[j];
                    if ((yi > y) != (yj > y))
                    {
                        double xi = r.Xs[i], xj = r.Xs[j];
                        double xInt = (xj - xi) * (y - yi) / (yj - yi + double.Epsilon) + xi;
                        if (x < xInt) crossings++;
                    }
                }
            }
            return (crossings & 1) == 1;
        }

        /// <summary>
        /// 派生一个“严格内部点”（不在边界上）：以顶点 y 偏移 ε 作扫描线，取相邻交点中点。
        /// 找不到（退化/零面积）返回 false。
        /// </summary>
        public static bool TryInteriorPoint(IReadOnlyList<FlatRing> part, out double x, out double y)
        {
            x = y = 0;
            var ys = part.SelectMany(r => r.Ys).Distinct().OrderBy(v => v).ToList();
            double minSpan = part.Min(r => r.MaxX - r.MinX);
            double eps = Math.Max(1e-9, minSpan * 1e-9);
            foreach (var baseY in ys)
            {
                foreach (var candY in new[] { baseY + eps, baseY - eps })
                {
                    var xs = new List<double>();
                    foreach (var r in part)
                    {
                        int n = r.Xs.Length;
                        for (int i = 0, j = n - 1; i < n; j = i++)
                        {
                            double yi = r.Ys[i], yj = r.Ys[j];
                            if ((yi > candY) == (yj > candY)) continue;
                            double xi2 = r.Xs[i], xj2 = r.Xs[j];
                            xs.Add(xi2 + (candY - yi) / (yj - yi) * (xj2 - xi2));
                        }
                    }
                    if (xs.Count < 2) continue;
                    xs.Sort();
                    for (int i = 0; i + 1 < xs.Count; i += 2)
                    {
                        double mid = (xs[i] + xs[i + 1]) / 2.0;
                        if (InsidePart(mid, candY, part)) { x = mid; y = candY; return true; }
                    }
                }
            }
            return false;
        }

        /// <summary>总顶点数（所有环）。</summary>
        public static int VertexCount(IReadOnlyList<FlatRing> rings) => rings.Sum(r => r.Xs.Length);

        /// <summary>净面积（各部件：外环面积减去洞面积，取绝对值累加）。</summary>
        public static double NetArea(IReadOnlyList<FlatRing> rings)
        {
            double total = 0;
            foreach (var part in GroupParts(rings))
            {
                double a = 0;
                foreach (var r in part) a += Math.Abs(r.SignedArea);
                // 奇偶归并：外环 + 洞 → 最大环为外环，其余为洞
                var sorted = part.OrderByDescending(r => Math.Abs(r.SignedArea)).ToList();
                a = sorted.Count == 0 ? 0 : Math.Abs(sorted[0].SignedArea);
                for (int i = 1; i < sorted.Count; i++) a -= Math.Abs(sorted[i].SignedArea);
                total += Math.Abs(a);
            }
            return total;
        }

        public static double[] BBox(IReadOnlyList<FlatRing> rings)
        {
            if (rings == null || rings.Count == 0) return new[] { 0d, 0d, 0d, 0d };
            return new[] { rings.Min(r => r.MinX), rings.Min(r => r.MinY), rings.Max(r => r.MaxX), rings.Max(r => r.MaxY) };
        }

        /// <summary>shapefile 记录 → 环列表（Polygon/PolyLine 类）。</summary>
        public static List<FlatRing> RingsOf(ShapefileRecords.Record rec)
        {
            var list = new List<FlatRing>();
            foreach (var r in rec.Rings)
                list.Add(FlatRing.Create((double[])r.Xs.Clone(), (double[])r.Ys.Clone()));
            return list;
        }

        /// <summary>WFS GeoJSON geometry → 环列表（Point/Line/Polygon 及 Multi* 全部展平为环序列）。</summary>
        public static List<FlatRing> RingsOf(JsonElement? geometry)
        {
            var list = new List<FlatRing>();
            if (geometry == null || geometry.Value.ValueKind != JsonValueKind.Object) return list;
            if (!geometry.Value.TryGetProperty("coordinates", out var coords)
                || coords.ValueKind != JsonValueKind.Array) return list;
            Collect(coords, list);
            return list;
        }

        private static void Collect(JsonElement el, List<FlatRing> into)
        {
            if (el.ValueKind != JsonValueKind.Array) return;
            var arr = el.EnumerateArray().ToArray();
            if (arr.Length == 0) return;
            if (arr[0].ValueKind == JsonValueKind.Number)
            {
                // 单个坐标点：[x,y] 或 [x,y,z]
                var xs = new double[1]; var ys = new double[1];
                xs[0] = arr[0].GetDouble(); ys[0] = arr[1].GetDouble();
                into.Add(FlatRing.Create(xs, ys));
                return;
            }
            bool isRing = arr.All(p => p.ValueKind == JsonValueKind.Array
                                     && p.GetArrayLength() >= 2
                                     && p.EnumerateArray().First().ValueKind == JsonValueKind.Number);
            if (isRing)
            {
                var xs = new double[arr.Length]; var ys = new double[arr.Length];
                int i = 0;
                foreach (var p in arr)
                {
                    var pt = p.EnumerateArray().ToArray();
                    xs[i] = pt[0].GetDouble(); ys[i] = pt[1].GetDouble(); i++;
                }
                into.Add(FlatRing.Create(xs, ys));
                return;
            }
            foreach (var c in arr) Collect(c, into);
        }

        /// <summary>WFS GeoJSON geometry 的坐标元组维度（3=带 Z；无几何返回 0）。</summary>
        public static int OrdinateDepth(JsonElement? geometry)
        {
            if (geometry == null || geometry.Value.ValueKind != JsonValueKind.Object) return 0;
            if (!geometry.Value.TryGetProperty("coordinates", out var coords)) return 0;
            return DepthOf(coords);
        }

        private static int DepthOf(JsonElement el)
        {
            if (el.ValueKind != JsonValueKind.Array) return 0;
            var arr = el.EnumerateArray().ToArray();
            if (arr.Length == 0) return 0;
            if (arr[0].ValueKind == JsonValueKind.Number) return arr.Length;
            return DepthOf(arr[0]);
        }

        /// <summary>逐元组遍历（用于 Z 范围）。</summary>
        public static IEnumerable<double[]> Tuples(JsonElement? geometry)
        {
            var outList = new List<double[]>();
            if (geometry == null || geometry.Value.ValueKind != JsonValueKind.Object) return outList;
            if (!geometry.Value.TryGetProperty("coordinates", out var coords)) return outList;
            Walk(coords, outList);
            return outList;
        }

        private static void Walk(JsonElement el, List<double[]> into)
        {
            if (el.ValueKind != JsonValueKind.Array) return;
            var arr = el.EnumerateArray().ToArray();
            if (arr.Length == 0) return;
            if (arr[0].ValueKind == JsonValueKind.Number)
            { into.Add(arr.Select(a => a.GetDouble()).ToArray()); return; }
            foreach (var c in arr) Walk(c, into);
        }
    }
}
