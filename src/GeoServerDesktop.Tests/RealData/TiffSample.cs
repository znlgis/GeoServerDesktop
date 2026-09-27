using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GeoServerDesktop.Tests.RealData
{
    /// <summary>
    /// 通用 GeoTIFF 像元/标签读取器（独立实现，不经 GDAL/JAI）：
    /// 支持 Byte/UInt16/Int16/Int32/Float32、多波段（CONTIG 像素交织与 SEPARATE 波段分离）、
    /// 条带与瓦片布局、大小端、PACKBITS 之外的 NONE/DEFLATE 之外的常见未压缩场景。
    /// 用途：真实栅格（多波段/整型/nodata/瓦片化）经 WCS 输出后的像元级保真比对。
    /// </summary>
    public sealed class TiffSample
    {
        public int Width, Height, SamplesPerPixel, BitsPerSample, SampleFormat, PlanarConfig, Compression;
        public int RowsPerStrip;
        public long[] StripOffsets = Array.Empty<long>();
        public long[] StripByteCounts = Array.Empty<long>();
        public bool IsTiled;
        public int TileWidth, TileLength;
        public long[] TileOffsets = Array.Empty<long>();
        public string? NoDataAscii;              // GDAL_NODATA (42112)
        public double[]? ModelPixelScale;
        public double[]? ModelTiepoint;
        public int GeoKeyProjCS = -1;
        public bool LittleEndian;
        public byte[] Bytes = Array.Empty<byte>();
        public string FilePath = "";

        public static TiffSample Parse(string path)
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length < 8) throw new InvalidDataException("TIFF 过短: " + path);
            bool little;
            if (bytes[0] == 'I' && bytes[1] == 'I') little = true;
            else if (bytes[0] == 'M' && bytes[1] == 'M') little = false;
            else throw new InvalidDataException("非法 TIFF 魔数: " + path);

            Func<int, ushort> u16 = i => little ? (ushort)((int)bytes[i] | ((int)bytes[i + 1] << 8))
                                                : (ushort)(((int)bytes[i] << 8) | (int)bytes[i + 1]);
            Func<int, byte> u8 = i => bytes[i];
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

            var t = new TiffSample { FilePath = path, Bytes = bytes, LittleEndian = little };
            int ifd = (int)u32(4);
            int n = u16(ifd);
            for (int e = 0; e < n; e++)
            {
                int ent = ifd + 2 + e * 12;
                int tag = u16(ent), type = u16(ent + 2);
                uint count = u32(ent + 4);
                int size = TypeSize(type);
                uint total = (uint)(size * (int)count);
                int valOff = total <= 4 ? ent + 8 : (int)u32(ent + 8);

                switch (tag)
                {
                    case 256: t.Width = (int)Scalar(valOff, type, count, u16, u32, u8); break;
                    case 257: t.Height = (int)Scalar(valOff, type, count, u16, u32, u8); break;
                    case 258: t.BitsPerSample = (int)Scalar(valOff, type, count, u16, u32, u8); break;   // 多波段取首值（各波段同宽）
                    case 277: t.SamplesPerPixel = (int)Scalar(valOff, type, count, u16, u32, u8); break;
                    case 279:
                        t.StripByteCounts = new long[count];
                        for (int k = 0; k < count; k++) t.StripByteCounts[k] = total <= 4 ? u32(valOff) : u32(valOff + k * 4);
                        break;
                    case 273:
                        t.StripOffsets = new long[count];
                        for (int k = 0; k < count; k++) t.StripOffsets[k] = total <= 4 ? u32(valOff) : u32(valOff + k * 4);
                        break;
                    case 278: t.RowsPerStrip = (int)Scalar(valOff, type, count, u16, u32, u8); break;
                    case 284: t.PlanarConfig = (int)Scalar(valOff, type, count, u16, u32, u8); break;
                    case 322: t.TileWidth = (int)Scalar(valOff, type, count, u16, u32, u8); break;
                    case 323: t.TileLength = (int)Scalar(valOff, type, count, u16, u32, u8); break;
                    case 324:
                        t.TileOffsets = new long[count];
                        for (int k = 0; k < count; k++) t.TileOffsets[k] = u32(valOff + k * 4);
                        t.IsTiled = true;
                        break;
                    case 325:
                        if (t.StripByteCounts == null || t.StripByteCounts.Length == 0)
                        {
                            t.StripByteCounts = new long[count];
                            for (int k = 0; k < count; k++) t.StripByteCounts[k] = u32(valOff + k * 4);
                        }
                        break;
                    case 339: t.SampleFormat = (int)Scalar(valOff, type, count, u16, u32, u8); break;
                    case 259: t.Compression = (int)Scalar(valOff, type, count, u16, u32, u8); break;
                    case 33550:
                        t.ModelPixelScale = new double[count];
                        for (int k = 0; k < count; k++) t.ModelPixelScale[k] = f64(valOff + k * 8);
                        break;
                    case 33922:
                        t.ModelTiepoint = new double[count];
                        for (int k = 0; k < count; k++) t.ModelTiepoint[k] = f64(valOff + k * 8);
                        break;
                    case 42112:
                        t.NoDataAscii = Encoding.ASCII.GetString(bytes, valOff, (int)count).Trim('\0', ' ', '\r', '\n');
                        break;
                    case 34735:
                        var geo = new int[count];
                        for (int k = 0; k < count; k++) geo[k] = u16(valOff + k * 2);
                        t.GeoKeyProjCS = FindProjCs(geo);
                        break;
                }
            }
            if (t.SamplesPerPixel == 0) t.SamplesPerPixel = 1;
            if (t.BitsPerSample == 0) t.BitsPerSample = 8;
            if (t.PlanarConfig == 0) t.PlanarConfig = 1;
            return t;
        }

        /// <summary>
        /// 单值标签按 TIFF 规范取数：SHORT(3) 只占值域前 2 字节，LONG(4) 占 4 字节；
        /// 值域不足 4 字节时值就地存放。早前一律按 u32 读，会把大端 SHORT 200 读成 13107200。
        /// </summary>
        private static uint Scalar(int valOff, int type, uint count, Func<int, ushort> u16, Func<int, uint> u32, Func<int, byte> u8)
        {
            if (type == 3) return u16(valOff);
            if (type == 4 || type == 9 || type == 13) return u32(valOff);
            if (type == 1 || type == 6 || type == 7) return u8(valOff);
            return u32(valOff);
        }

        private static int FindProjCs(int[] keys)
        {
            // GeoKeyDirectory: [KeyDirectoryVersion, MinorRev, NumberOfKeys, (keyId,type,count,valOffset)* ]
            for (int i = 4; i + 3 < keys.Length; i += 4)
                if (keys[i] == 3072) return keys[i + 3];
            return -1;
        }

        private static int TypeSize(int type)
        {
            switch (type)
            {
                case 1: case 2: case 6: case 7: return 1;
                case 3: case 8: return 2;
                case 4: case 9: case 11: return 4;
                case 5: case 10: case 12: return 8;
                default: return 1;
            }
        }

        /// <summary>读 (px,py,band) 的像元值（double 归一，Row-0 = 顶部行）。band 从 1 起。</summary>
        public double ReadSample(int px, int py, int band = 1)
        {
            if (px < 0 || py < 0 || px >= Width || py >= Height)
                throw new InvalidDataException($"像元 ({px},{py}) 越界 {Width}x{Height}");
            if (Compression != 1)
                throw new InvalidOperationException("仅支持未压缩 TIFF，Compression=" + Compression);
            int bytesPerSample = BitsPerSample / 8;
            long off;
            if (IsTiled)
            {
                if (PlanarConfig == 2) throw new NotSupportedException("瓦片 + SEPARATE 布局未实现（本数据集为 CONTIG）");
                int tilesWide = (Width + TileWidth - 1) / Math.Max(1, TileWidth);
                int tr = py / Math.Max(1, TileLength), tc = px / Math.Max(1, TileWidth);
                int idx = tr * tilesWide + tc;
                if (idx >= TileOffsets.Length) throw new InvalidDataException("瓦片索引越界");
                int inRow = py - tr * TileLength, inCol = px - tc * TileWidth;
                off = TileOffsets[idx] + (((long)inRow * TileWidth + inCol) * SamplesPerPixel + (band - 1)) * bytesPerSample;
            }
            else
            {
                int rps = RowsPerStrip <= 0 ? Height : RowsPerStrip;
                int strip = py / rps;
                if (strip >= StripOffsets.Length) throw new InvalidDataException("条带索引越界");
                int rowInStrip = py - strip * rps;
                off = PlanarConfig == 2
                    ? StripOffsets[strip] + (long)(band - 1) * RowsPerStrip * Width * bytesPerSample
                      + ((long)rowInStrip * Width + px) * bytesPerSample
                    : StripOffsets[strip] + (((long)rowInStrip * Width + px) * SamplesPerPixel + (band - 1)) * bytesPerSample;
            }
            return Decode(off, bytesPerSample);
        }

        private double Decode(long off, int size)
        {
            if (off < 0 || off + size > Bytes.Length) throw new InvalidDataException("像元偏移越界 " + off);
            var b = new byte[size];
            Array.Copy(Bytes, off, b, 0, size);
            if (!LittleEndian) Array.Reverse(b);
            switch (BitsPerSample)
            {
                case 8: return SampleFormat == 2 ? (sbyte)b[0] : b[0];
                case 16: return SampleFormat == 2 ? BitConverter.ToInt16(b, 0) : BitConverter.ToUInt16(b, 0);
                case 32: return SampleFormat == 3 ? BitConverter.ToSingle(b, 0) : BitConverter.ToInt32(b, 0);
                default: throw new NotSupportedException("位深 " + BitsPerSample + " 未实现");
            }
        }

        /// <summary>nodata 值（按声明类型解释）；无声明返回 null。</summary>
        public double? NoDataValue()
        {
            if (string.IsNullOrWhiteSpace(NoDataAscii)) return null;
            return double.TryParse(NoDataAscii.Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : (double?)null;
        }
    }
}
