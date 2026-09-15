using System;
using System.IO;

namespace GeoServerDesktop.Tests.RealData
{
    /// <summary>
    /// 从 GeoTIFF 独立读取期望像元（仅依赖 TIFF 头，不经 GDAL）。
    /// 支持两类布局：条带（strip，含多段、非连续排布）与瓦片（tile）——
    /// GeoServer WCS GetCoverage 返回的 GeoTIFF 常被重排为 tiled，故不能假设单条带或连续。
    /// </summary>
    public static class TiffPixels
    {
        /// <summary>读 uncompressed float32 单波段像元值（Row-0 = 顶部行）。</summary>
        public static float ReadFloat32(string tifPath, int px, int py)
            => ReadFloat32(TiffHeader.Parse(tifPath), File.ReadAllBytes(tifPath), px, py);

        public static float ReadFloat32(TiffHeader h, byte[] bytes, int px, int py)
        {
            if (px < 0 || py < 0 || px >= h.Width || py >= h.Height)
                throw new InvalidDataException($"像元 ({px},{py}) 越界 {h.Width}x{h.Height}");
            // 仅支持未压缩单波段 32 位浮点（本数据集满足；压缩态需另实现解码器）
            if (h.Compression != 1)
                throw new InvalidOperationException("仅支持未压缩(Compression=1) TIFF，当前=" + h.Compression);
            if (h.BitsPerSample != 32 || h.SamplesPerPixel != 1)
                throw new InvalidOperationException("仅支持单波段 Float32");
            if (h.SampleFormat != 3)
                throw new InvalidOperationException("SampleFormat 非 float(3)，当前=" + h.SampleFormat);

            long off = h.IsTiled ? TileOffset(h, px, py) : StripOffset(h, px, py);
            if (off < 0 || off + 4 > bytes.Length) throw new InvalidDataException("像元偏移越界");
            // TIFF 数据字节序与文件头一致；本项目源文件为小端、WCS 返回为大端，
            // BitConverter.ToSingle 依 CPU(小端) 解释，故大端文件需按反序读。
            return ReadFloat(h, bytes, off);
        }

        /// <summary>条带布局：定位 py 所在条带，使用该条带真实起始偏移（不假设条带连续）。</summary>
        private static long StripOffset(TiffHeader h, int px, int py)
        {
            int rps = h.RowsPerStrip <= 0 ? h.Height : h.RowsPerStrip;
            int strip = py / rps;
            if (strip >= h.StripOffsets.Length)
                throw new InvalidDataException("条带索引越界");
            int rowInStrip = py - strip * rps;
            return h.StripOffsets[strip] + ((long)rowInStrip * h.Width + px) * 4;
        }

        /// <summary>瓦片布局：按 (row,col) 定位瓦片，瓦片内行主序（瓦片宽=TileWidth 步长）。</summary>
        private static long TileOffset(TiffHeader h, int px, int py)
        {
            int tilesWide = (h.Width + h.TileWidth - 1) / h.TileWidth;
            int tr = py / h.TileLength, tc = px / h.TileWidth;
            int idx = tr * tilesWide + tc;
            if (idx >= h.TileOffsets.Length)
                throw new InvalidDataException("瓦片索引越界");
            int inRow = py - tr * h.TileLength;
            int inCol = px - tc * h.TileWidth;
            return h.TileOffsets[idx] + ((long)inRow * h.TileWidth + inCol) * 4;
        }

        /// <summary>依 TIFF 声明字节序读取 float32（大端文件反字节序，源文件小端直读）。</summary>
        private static float ReadFloat(TiffHeader h, byte[] bytes, long off)
        {
            if (h.LittleEndian) return BitConverter.ToSingle(bytes, (int)off);
            return BitConverter.ToSingle(new[] { bytes[off + 3], bytes[off + 2], bytes[off + 1], bytes[off] }, 0);
        }

        /// <summary>像元 (px,py) → 地理坐标（中心点），基于 ModelPixelScale + ModelTiepoint。</summary>
        public static (double x, double y) PixelToGeo(TiffHeader h, int px, int py)
        {
            var ps = h.ModelPixelScale; var tp = h.ModelTiepoint;
            if (ps == null || tp == null) throw new InvalidOperationException("缺 geo 标签");
            return (tp[3] + (px + 0.5 - tp[0]) * ps[0], tp[4] - (py + 0.5 - tp[1]) * ps[1]);
        }
    }
}
