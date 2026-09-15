using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GeoServerDesktop.Tests.RealData
{
    /// <summary>SHP 头独立解析（不经 GDAL/GeoTools）：偏移 8..23 大端 file length(16bit字)，偏移 32 几何类型，偏移 36-67 bbox。</summary>
    public sealed class ShapefileHeader
    {
        public int FileLengthBytes;   // 文件总长（字节）
        public int Version;
        public int ShapeType;         // 0,1,3,5,8,11,13,15,18,21,23,25,31
        public double XMin, YMin, XMax, YMax;
        public string ShpPath;

        public static readonly Dictionary<int, string> ShapeTypeName = new Dictionary<int, string>
        {
            { 0, "Null" }, { 1, "Point" }, { 3, "PolyLine" }, { 4, "MultiPoint" },
            { 5, "Polygon" }, { 8, "MultiPatch" }, { 11, "PointZ" }, { 13, "PolyLineZ" },
            { 15, "PolygonZ" }, { 18, "MultiPointZ" }, { 21, "PointM" }, { 23, "PolyLineM" },
            { 25, "PolygonM" }, { 28, "MultiPointM" }, { 31, "MultiPatch" },
        };

        public static ShapefileHeader Parse(string shpPath)
        {
            var bytes = File.ReadAllBytes(shpPath);
            if (bytes.Length < 100) throw new InvalidDataException("SHP 头不足 100 字节: " + shpPath);
            var be = BitConverter.ToInt32(new[] { bytes[2], bytes[3], bytes[0], bytes[1] }, 0); // file length in 16-bit words, big endian
            if (BitConverter.ToInt32(bytes, 0) != 9994)
            {
                // 大端检查：偏移 0-3 应为 9994（big endian）
                int code = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
                if (code != 9994) throw new InvalidDataException("非法 SHP 文件: " + shpPath);
                be = (bytes[24] << 24) | (bytes[25] << 16) | (bytes[26] << 8) | bytes[27];
            }
            else
            {
                be = (bytes[24] << 24) | (bytes[25] << 16) | (bytes[26] << 8) | bytes[27];
            }
            var h = new ShapefileHeader
            {
                ShpPath = shpPath,
                FileLengthBytes = be * 2,
                Version = BitConverter.ToInt32(bytes, 28),
                ShapeType = BitConverter.ToInt32(bytes, 32),
                XMin = BitConverter.ToDouble(bytes, 36),
                YMin = BitConverter.ToDouble(bytes, 44),
                XMax = BitConverter.ToDouble(bytes, 52),
                YMax = BitConverter.ToDouble(bytes, 60),
            };
            return h;
        }

        public static int CountRecords(string shpPath)
        {
            var bytes = File.ReadAllBytes(shpPath);
            int count = 0, pos = 100;
            while (pos + 8 <= bytes.Length)
            {
                int recNo = (bytes[pos] << 24) | (bytes[pos + 1] << 16) | (bytes[pos + 2] << 8) | bytes[pos + 3];
                int contentLen = ((bytes[pos + 4] << 24) | (bytes[pos + 5] << 16) | (bytes[pos + 6] << 8) | bytes[pos + 7]) * 2;
                if (contentLen < 0 || pos + 8 + contentLen > bytes.Length) break;
                count++;
                pos += 8 + contentLen;
            }
            return count;
        }
    }

    /// <summary>DBF 头独立解析：偏移 4 记录数，偏移 8 头长，偏移 10 记录长，字段描述表。</summary>
    public sealed class DbfHeader
    {
        public int RecordCount;
        public int HeaderLength;
        public int RecordLength;
        public List<DbfField> Fields = new List<DbfField>();

        public sealed class DbfField
        {
            public string Name;
            public char Type;   // C/N/D/L/F
            public int Length;
            public int Decimals;
        }

        public static DbfHeader Parse(string dbfPath)
        {
            var bytes = File.ReadAllBytes(dbfPath);
            if (bytes.Length < 32) throw new InvalidDataException("DBF 头不足: " + dbfPath);
            var h = new DbfHeader
            {
                RecordCount = BitConverter.ToInt32(bytes, 4),
                HeaderLength = BitConverter.ToInt16(bytes, 8),
                RecordLength = BitConverter.ToInt16(bytes, 10),
            };
            int pos = 32;
            while (pos + 32 <= bytes.Length && bytes[pos] != 0x0D)
            {
                var name = Encoding.ASCII.GetString(bytes, pos, 11);
                int st = name.IndexOf('\0');
                h.Fields.Add(new DbfField
                {
                    Name = (st >= 0 ? name.Substring(0, st) : name).Trim(),
                    Type = (char)bytes[pos + 11],
                    Length = bytes[pos + 16],
                    Decimals = bytes[pos + 17],
                });
                pos += 32;
            }
            return h;
        }
    }

    /// <summary>
    /// GeoTIFF 独立解析（经典 TIFF）：IFD 标签 256/257/258/273/277/279/339/34735；
    /// 仿射标签按规范：33550 = ModelPixelScale(sx,sy,sz)，33922 = ModelTiepoint(i,j,k,x,y,z)（早期曾把两者对调，已纠正）。
    /// 仅解析测试所需：尺寸、位深、压缩、条带/瓦片、ModelPixelScale/ModelTiepoint、GeoKey 3072 投影代码。
    /// </summary>
    public sealed class TiffHeader
    {
        public int Width, Height, BitsPerSample = 8, SamplesPerPixel = 1;
        public int Compression = 1;
        public long[] StripOffsets = Array.Empty<long>();
        public int[] StripByteCounts = Array.Empty<int>();
        public int RowsPerStrip = int.MaxValue;
        // 瓦片布局（GeoServer WCS GetCoverage 返回的 GeoTIFF 常被重排为 tiled，需按瓦片读取）
        public bool IsTiled;                       // 存在瓦片标签（322/323/324/325）且无条带（273）
        public int TileWidth, TileLength;
        public long[] TileOffsets = Array.Empty<long>();
        public int[] TileByteCounts = Array.Empty<int>();
        public int SampleFormat = 1;               // 339：1=uint,2=int,3=float
        public bool LittleEndian = true;           // 源文件为 II（小端），GeoServer WCS 输出常为 MM（大端）
        public double[] ModelPixelScale;   // sx, sy, sz
        public double[] ModelTiepoint;     // i,j,k,x,y,z
        public int GeoTiffProjCS = -1;     // 3072ProjectedCSTypeGeoKey，含 EPSG 码
        public string TifPath;

        public static TiffHeader Parse(string tifPath)
        {
            var bytes = File.ReadAllBytes(tifPath);
            var h = new TiffHeader { TifPath = tifPath };
            bool little;
            if (bytes[0] == 'I' && bytes[1] == 'I') little = true;
            else if (bytes[0] == 'M' && bytes[1] == 'M') little = false;
            else throw new InvalidDataException("非法 TIFF: " + tifPath);
            h.LittleEndian = little;
            Func<int, ushort> u16 = i => little ? (ushort)(bytes[i] | (bytes[i + 1] << 8)) : (ushort)((bytes[i] << 8) | bytes[i + 1]);
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
            Func<int, float> f32 = i =>
            {
                var b = new byte[4];
                Array.Copy(bytes, i, b, 0, 4);
                if (!little) Array.Reverse(b);
                return BitConverter.ToSingle(b, 0);
            };
            uint ifdOff = u32(4);
            int n = u16((int)ifdOff);
            var tagValues = new Dictionary<int, uint[]>();
            var tagShorts = new Dictionary<int, ushort[]>();
            var tagDoubles = new Dictionary<int, double[]>();
            var tagFloats = new Dictionary<int, float[]>();
            for (int e = 0; e < n; e++)
            {
                int ent = (int)ifdOff + 2 + e * 12;
                int tag = u16(ent);
                int type = u16(ent + 2);
                uint count = u32(ent + 4);
                int size = TypeSize(type);
                uint total = (uint)(size * (int)count);
                int valOff = total <= 4 ? ent + 8 : (int)u32(ent + 8);
                if (type == 12 || type == 11)
                {
                    var arr = new double[count];
                    var fa = new float[count];
                    for (int k = 0; k < count; k++)
                    {
                        arr[k] = type == 12 ? f64(valOff + k * 8) : f32(valOff + k * 4);
                        fa[k] = type == 11 ? f32(valOff + k * 4) : (float)f64(valOff + k * 8);
                    }
                    if (type == 12) tagDoubles[tag] = arr; else tagFloats[tag] = fa;
                }
                else if (type == 3)
                {
                    var sa = new ushort[count];
                    for (int k = 0; k < count; k++) sa[k] = u16(valOff + k * 2);
                    tagShorts[tag] = sa;
                    var arr = new uint[count];
                    for (int k = 0; k < count; k++) arr[k] = sa[k];
                    tagValues[tag] = arr;
                }
                else
                {
                    var arr = new uint[count];
                    for (int k = 0; k < count; k++)
                    {
                        if (size == 2) arr[k] = u16(valOff + k * 2);
                        else if (size == 4) arr[k] = u32(valOff + k * 4);
                        else if (size == 1) arr[k] = bytes[valOff + k];
                    }
                    tagValues[tag] = arr;
                }
            }
            if (tagValues.TryGetValue(256, out var w)) h.Width = (int)w[0];
            if (tagValues.TryGetValue(257, out var hh)) h.Height = (int)hh[0];
            if (tagValues.TryGetValue(258, out var bps)) h.BitsPerSample = (int)bps[0];
            if (tagValues.TryGetValue(277, out var spp277)) h.SamplesPerPixel = (int)spp277[0];   // 277 = SamplesPerPixel
            if (tagValues.TryGetValue(278, out var rps278)) h.RowsPerStrip = (int)rps278[0];       // 278 = RowsPerStrip
            if (tagValues.TryGetValue(279, out var sbc))
            {
                h.StripByteCounts = new int[sbc.Length];
                for (int i = 0; i < sbc.Length; i++) h.StripByteCounts[i] = (int)sbc[i];
            }
            if (tagValues.TryGetValue(273, out var so))
            {
                h.StripOffsets = new long[so.Length];
                for (int i = 0; i < so.Length; i++) h.StripOffsets[i] = so[i];
            }
            if (tagValues.TryGetValue(339, out var sfmt)) h.SampleFormat = (int)sfmt[0];       // 339 = SampleFormat (3=float)
            if (tagValues.TryGetValue(259, out var comp)) h.Compression = (int)comp[0];
            // 瓦片标签（tiled GeoTIFF）：322 TileWidth, 323 TileLength, 324 TileOffsets, 325 TileByteCounts
            if (tagValues.TryGetValue(322, out var tw) && tw.Length > 0) h.TileWidth = (int)tw[0];
            if (tagValues.TryGetValue(323, out var tl) && tl.Length > 0) h.TileLength = (int)tl[0];
            if (tagValues.TryGetValue(324, out var toff))
            {
                h.TileOffsets = new long[toff.Length];
                for (int i = 0; i < toff.Length; i++) h.TileOffsets[i] = toff[i];
            }
            if (tagValues.TryGetValue(325, out var tbc))
            {
                h.TileByteCounts = new int[tbc.Length];
                for (int i = 0; i < tbc.Length; i++) h.TileByteCounts[i] = (int)tbc[i];
            }
            // SampleFormat 是标签 339；SamplesPerPixel 是标签 277（注意区分，见上）
            if (tagValues.TryGetValue(277, out var _277)) { /* 已在上面按 SamplesPerPixel 读取，保持兼容 */ }
            // 判定 tiled：无条带偏移 273 但存在瓦片偏移 324
            h.IsTiled = h.StripOffsets.Length == 0 && h.TileOffsets.Length > 0 && h.TileWidth > 0 && h.TileLength > 0;
            if (tagDoubles.TryGetValue(33550, out var ps)) h.ModelPixelScale = ps;   // 33550 = ModelPixelScaleTag (sx,sy,sz)
            if (tagDoubles.TryGetValue(33922, out var tp)) h.ModelTiepoint = tp;     // 33922 = ModelTiepointTag (i,j,k,x,y,z)
            // GeoKeyDirectory(34735) SHORT 数组：[KeyDirectoryVersion, KeyRevision, MinorRevision, NumberOfKeys, (KeyID, TIFFTagLocation, Count, Value|Offset)*N]
            if (tagShorts.TryGetValue(34735, out var gk) && gk.Length >= 4)
            {
                int num = gk[3];
                for (int k = 0; k < num; k++)
                {
                    int b = 4 + k * 4;
                    if (b + 3 >= gk.Length) break;
                    if (gk[b] == 3072 && gk[b + 1] == 0 && gk[b + 2] == 1)
                    {
                        h.GeoTiffProjCS = gk[b + 3];
                        break;
                    }
                }
            }
            return h;
        }

        private static int TypeSize(int type) => type switch
        {
            1 or 2 or 6 or 7 => 1,
            3 or 8 => 2,
            4 or 9 or 11 => 4,
            5 or 10 or 12 => 8,
            _ => 1
        };
    }
}
