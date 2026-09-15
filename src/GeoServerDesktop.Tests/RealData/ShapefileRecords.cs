using System;
using System.Collections.Generic;
using System.IO;

namespace GeoServerDesktop.Tests.RealData
{
    /// <summary>
    /// 极小 SHP 记录几何读取器（独立于 GDAL/GeoServer）：逐记录解析多边形的环坐标与包络框，
    /// 并据此推导“部件数/洞数/总面积”等期望值，供服务面几何锚定与面积比对使用。
    /// 仅覆盖测试所需 ShapeType：5/15/25 Polygon、3/13/23 PolyLine、1/11/21 Point（含 Z/M 变体按 XY 读）。
    /// </summary>
    public sealed class ShapefileRecords
    {
        public sealed class Ring
        {
            public double[] Xs = Array.Empty<double>();
            public double[] Ys = Array.Empty<double>();
            public double MinX, MinY, MaxX, MaxY;
            public double SignedArea;                 // 有向面积（shoelace），符号表示绕向
        }

        public sealed class Record
        {
            public int ShapeType;
            public double MinX, MinY, MaxX, MaxY;     // 记录级包络框
            public List<Ring> Rings = new List<Ring>();
        }

        public static List<Record> ReadAll(string shpPath)
        {
            var bytes = File.ReadAllBytes(shpPath);
            var list = new List<Record>();
            int pos = 100;
            while (pos + 8 <= bytes.Length)
            {
                int recNo = BeInt32(bytes, pos);
                int contentLen = BeInt32(bytes, pos + 4) * 2;           // 16bit 字→字节
                int contentStart = pos + 8;
                if (contentLen < 4 || contentStart + contentLen > bytes.Length) break;
                int st = BitConverter.ToInt32(bytes, contentStart);
                var rec = new Record();
                // 归一化去 Z/M：11/13/15/21/23/25/31 → 基类型
                rec.ShapeType = BaselineType(st);
                if (rec.ShapeType == 5 || rec.ShapeType == 3 || rec.ShapeType == 15 || rec.ShapeType == 13
                    || rec.ShapeType == 25 || rec.ShapeType == 23)
                {
                    // 头：shapeType(4) + box(32) + numParts(4) + numPoints(4) + parts(4*np) + points(16*np) [+Z][+M]
                    rec.MinX = BitConverter.ToDouble(bytes, contentStart + 4);
                    rec.MinY = BitConverter.ToDouble(bytes, contentStart + 12);
                    rec.MaxX = BitConverter.ToDouble(bytes, contentStart + 20);
                    rec.MaxY = BitConverter.ToDouble(bytes, contentStart + 28);
                    int numParts = BitConverter.ToInt32(bytes, contentStart + 36);
                    int numPoints = BitConverter.ToInt32(bytes, contentStart + 40);
                    var parts = new int[numParts];
                    for (int i = 0; i < numParts; i++) parts[i] = BitConverter.ToInt32(bytes, contentStart + 44 + i * 4);
                    int ptsOff = contentStart + 44 + numParts * 4;
                    for (int i = 0; i < numParts; i++)
                    {
                        int start = parts[i];
                        int end = (i + 1 < numParts) ? parts[i + 1] : numPoints;
                        var r = new Ring();
                        int n = end - start;
                        r.Xs = new double[n]; r.Ys = new double[n];
                        r.MinX = r.MinY = double.MaxValue; r.MaxX = r.MaxY = double.MinValue;
                        for (int k = 0; k < n; k++)
                        {
                            int po = ptsOff + (start + k) * 16;
                            double x = BitConverter.ToDouble(bytes, po);
                            double y = BitConverter.ToDouble(bytes, po + 8);
                            r.Xs[k] = x; r.Ys[k] = y;
                            if (x < r.MinX) r.MinX = x; if (x > r.MaxX) r.MaxX = x;
                            if (y < r.MinY) r.MinY = y; if (y > r.MaxY) r.MaxY = y;
                        }
                        r.SignedArea = Shoelace(r.Xs, r.Ys);
                        rec.Rings.Add(r);
                    }
                }
                list.Add(rec);
                pos = contentStart + contentLen;
                _ = recNo;
            }
            return list;
        }

        private static int BaselineType(int st)
        {
            int a = Math.Abs(st);
            if (a >= 3000 && a <= 3999) a %= 1000;              // 测量 M 系列
            return a switch { 11 or 21 => 1, 13 or 23 => 3, 15 or 25 => 5, 18 or 28 => 4, 31 => 20, _ => a };
        }

        private static int BeInt32(byte[] b, int i)
            => (b[i] << 24) | (b[i + 1] << 16) | (b[i + 2] << 8) | b[i + 3];

        /// <summary>闭合环的有向面积（shoelace）。正/负号代表绕向，用于区分外环与内环（洞）。</summary>
        public static double Shoelace(double[] xs, double[] ys)
        {
            int n = xs.Length; if (n < 3) return 0;
            double s = 0;
            for (int i = 0, j = n - 1; i < n; j = i++)
                s += (xs[j] + xs[i]) * (ys[j] - ys[i]);
            return s / 2.0;
        }

        /// <summary>把一条记录的所有环按“外环 + 其内包含的洞”分组，得到部件（多面体）数与洞数。</summary>
        public static (int parts, int holes, double area) Classify(Record rec)
        {
            // 以绝对面积从大到小排序：大环为潜在外环，被某外环包围者视为其洞。
            var rings = new List<Ring>(rec.Rings);
            rings.Sort((a, b) => Math.Abs(b.SignedArea).CompareTo(Math.Abs(a.SignedArea)));
            var consumed = new bool[rings.Count];
            int parts = 0, holes = 0; double area = 0;
            for (int i = 0; i < rings.Count; i++)
            {
                if (consumed[i]) continue;
                consumed[i] = true; parts++;
                double extA = Math.Abs(Shoelace(rings[i].Xs, rings[i].Ys));
                for (int k = i + 1; k < rings.Count; k++)
                {
                    if (consumed[k]) continue;
                    if (Contains(rings[i], rings[k]))
                    {
                        consumed[k] = true; holes++;
                        extA -= Math.Abs(Shoelace(rings[k].Xs, rings[k].Ys));
                    }
                }
                area += extA;
            }
            return (parts, holes, area);
        }

        /// <summary>外环 bbox 是否完整包含内环 bbox（且严格更大）——对本简单数据足够判定洞归属。</summary>
        private static bool Contains(Ring outer, Ring inner)
            => inner.MinX >= outer.MinX && inner.MaxX <= outer.MaxX
            && inner.MinY >= outer.MinY && inner.MaxY <= outer.MaxY
            && (Math.Abs(Shoelace(inner.Xs, inner.Ys)) + 1e-12 < Math.Abs(Shoelace(outer.Xs, outer.Ys)));
    }
}
