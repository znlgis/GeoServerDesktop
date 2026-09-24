using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GeoServerDesktop.GeoServerClient.Import
{
    /// <summary>
    /// 导入向导：本地地理数据文件的“头部独立解析”工具（不依赖 GDAL/GeoTools）。
    /// 用于发布前的预检预览（bbox、SRID、字段、记录数、栅格尺寸）。
    /// 解析技术与测试侧 RealData 同源：.shp 头（偏移 32 类型 / 36-67 bbox）、.dbf 头（记录数/字段表）、
    /// .prj（EPSG AUTHORITY）、经典 TIFF/GeoTIFF（IFD 标签 256/257/258/277/273/324/33550/34735）。
    /// 读取策略为“只读头/有限前缀”，避免大文件全量载入。
    /// </summary>
    public static class GeoFileInspector
    {
        private const int ShpHeaderBytes = 100;
        private const int MaxTiffProbeBytes = 4 * 1024 * 1024;

        private static readonly Regex EpsgAuthorityRegex =
            new Regex("AUTHORITY\\s*\\[\\s*\"EPSG\"\\s*,\\s*\"(\\d+)\"\\s*\\]", RegexOptions.IgnoreCase);

        private static readonly Regex EpsgIdRegex =
            new Regex("ID\\s*\\[\\s*\"EPSG\"\\s*,\\s*(\\d+)\\s*\\]", RegexOptions.IgnoreCase);

        private static readonly Regex CrsNameRegex =
            new Regex("^\\s*(?:PROJCS|GEOGCS|PROJCRS|GEOGCRS|COMPD_CS|COMPOUNDCRS)\\s*\\[\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);

        private static readonly Dictionary<int, string> ShapeTypeNames = new Dictionary<int, string>
        {
            { 0, "Null" }, { 1, "Point" }, { 3, "PolyLine" }, { 4, "MultiPoint" },
            { 5, "Polygon" }, { 8, "MultiPatch" }, { 11, "PointZ" }, { 13, "PolyLineZ" },
            { 15, "PolygonZ" }, { 18, "MultiPointZ" }, { 21, "PointM" }, { 23, "PolyLineM" },
            { 25, "PolygonM" }, { 28, "MultiPointM" }, { 31, "MultiPatch" },
        };

        // ---------- Shapefile ----------

        /// <summary>
        /// 预检 Shapefile（.shp 头 + 同目录 .dbf/.prj/.shx/.cpg）。
        /// </summary>
        /// <param name="shpPath">.shp 文件路径。</param>
        /// <returns>预检结果。</returns>
        public static ShapefilePreview InspectShapefile(string shpPath)
        {
            if (string.IsNullOrEmpty(shpPath)) throw new ArgumentNullException(nameof(shpPath));
            if (!File.Exists(shpPath)) throw new FileNotFoundException("Shapefile 不存在: " + shpPath, shpPath);

            var header = new byte[ShpHeaderBytes];
            using (var fs = new FileStream(shpPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (fs.Length < ShpHeaderBytes) throw new InvalidDataException("SHP 头不足 100 字节: " + shpPath);
                ReadExactly(fs, header, 0, ShpHeaderBytes);
            }

            int fileCode = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
            if (fileCode != 9994) throw new InvalidDataException("非法 SHP 文件（file code != 9994）: " + shpPath);

            var preview = new ShapefilePreview
            {
                ShpPath = shpPath,
                ShapeType = BitConverter.ToInt32(header, 32),
                MinX = BitConverter.ToDouble(header, 36),
                MinY = BitConverter.ToDouble(header, 44),
                MaxX = BitConverter.ToDouble(header, 52),
                MaxY = BitConverter.ToDouble(header, 60),
            };
            string typeName;
            preview.ShapeTypeName = ShapeTypeNames.TryGetValue(preview.ShapeType, out typeName) ? typeName : "Unknown(" + preview.ShapeType + ")";

            string dir = Path.GetDirectoryName(Path.GetFullPath(shpPath));
            string baseName = Path.GetFileNameWithoutExtension(shpPath);
            string dbfPath = Path.Combine(dir, baseName + ".dbf");
            string prjPath = Path.Combine(dir, baseName + ".prj");
            preview.HasShx = File.Exists(Path.Combine(dir, baseName + ".shx"));
            preview.HasCpg = File.Exists(Path.Combine(dir, baseName + ".cpg"));
            preview.HasDbf = File.Exists(dbfPath);
            preview.HasPrj = File.Exists(prjPath);

            if (preview.HasDbf)
            {
                try
                {
                    var dbf = ParseDbfHeader(dbfPath);
                    preview.RecordCount = dbf.RecordCount;
                    preview.Fields = dbf.Fields;
                }
                catch (Exception)
                {
                    preview.RecordCount = -1;   // 字段/记录数解析失败不阻断（头解析为主）
                }
            }

            if (preview.HasPrj)
            {
                try
                {
                    string wkt = File.ReadAllText(prjPath, Encoding.UTF8).Trim();
                    preview.EpsgCode = ExtractEpsgCode(wkt);
                    var m = CrsNameRegex.Match(wkt);
                    if (m.Success) preview.ProjectionName = m.Groups[1].Value;
                }
                catch (Exception)
                {
                    // .prj 解析失败不阻断
                }
            }

            return preview;
        }

        /// <summary>从 .prj WKT 文本提取 EPSG 代码（优先取最后一个 AUTHORITY["EPSG",...] / ID["EPSG",...]，即最外层坐标系的权威码）。</summary>
        public static string ExtractEpsgCode(string wkt)
        {
            if (string.IsNullOrEmpty(wkt)) return null;
            string code = null;
            foreach (Match m in EpsgAuthorityRegex.Matches(wkt))
            {
                code = m.Groups[1].Value;
            }
            if (code != null) return code;
            foreach (Match m in EpsgIdRegex.Matches(wkt))
            {
                code = m.Groups[1].Value;
            }
            return code;
        }

        // ---------- GeoTIFF ----------

        /// <summary>
        /// 预检 GeoTIFF（经典 TIFF IFD 头；读取范围限文件前 4MB，覆盖 IFD 与常见标签值）。
        /// </summary>
        /// <param name="tifPath">.tif/.tiff 文件路径。</param>
        /// <returns>预检结果。</returns>
        public static GeoTiffPreview InspectGeoTiff(string tifPath)
        {
            if (string.IsNullOrEmpty(tifPath)) throw new ArgumentNullException(nameof(tifPath));
            if (!File.Exists(tifPath)) throw new FileNotFoundException("GeoTIFF 不存在: " + tifPath, tifPath);

            var fi = new FileInfo(tifPath);
            byte[] bytes;
            using (var fs = new FileStream(tifPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int len = (int)Math.Min(fs.Length, MaxTiffProbeBytes);
                bytes = new byte[len];
                ReadExactly(fs, bytes, 0, len);
            }

            if (bytes.Length < 8) throw new InvalidDataException("TIFF 文件过小: " + tifPath);
            bool little;
            if (bytes[0] == (byte)'I' && bytes[1] == (byte)'I') little = true;
            else if (bytes[0] == (byte)'M' && bytes[1] == (byte)'M') little = false;
            else throw new InvalidDataException("非法 TIFF（字节序标记缺失）: " + tifPath);

            Func<int, ushort> u16 = i => little
                ? (ushort)(bytes[i] | (bytes[i + 1] << 8))
                : (ushort)((bytes[i] << 8) | bytes[i + 1]);
            Func<int, uint> u32 = i => little
                ? (uint)bytes[i] | ((uint)bytes[i + 1] << 8) | ((uint)bytes[i + 2] << 16) | ((uint)bytes[i + 3] << 24)
                : ((uint)bytes[i] << 24) | ((uint)bytes[i + 1] << 16) | ((uint)bytes[i + 2] << 8) | (uint)bytes[i + 3];
            Func<int, double> f64 = i =>
            {
                var b = new byte[8];
                Array.Copy(bytes, i, b, 0, 8);
                if (!little) Array.Reverse(b);
                return BitConverter.ToDouble(b, 0);
            };

            ushort magic = u16(2);
            if (magic != 42) throw new InvalidDataException("非法 TIFF（magic != 42）: " + tifPath);

            uint ifdOff = u32(4);
            if (ifdOff + 2 > (uint)bytes.Length)
                throw new InvalidDataException("TIFF IFD 偏移超出预检读取范围（文件过大或非常规布局）: " + tifPath);

            var preview = new GeoTiffPreview
            {
                TifPath = tifPath,
                FileSizeBytes = fi.Length,
                Compression = 1,
                BitsPerSample = 8,
                SamplesPerPixel = 1,
            };

            int n = u16((int)ifdOff);
            bool hasStripOffsets = false;
            bool hasTileOffsets = false;
            int tileWidth = 0;
            int tileLength = 0;

            for (int e = 0; e < n; e++)
            {
                int ent = (int)ifdOff + 2 + e * 12;
                if (ent + 12 > bytes.Length) break;
                int tag = u16(ent);
                int type = u16(ent + 2);
                uint count = u32(ent + 4);
                int size = TiffTypeSize(type);
                uint total = (uint)(size * (int)count);
                int valOff = total <= 4 ? ent + 8 : (int)u32(ent + 8);

                switch (tag)
                {
                    case 256: preview.Width = (int)ReadTiffNumber(bytes, type, valOff, u16, u32); break;
                    case 257: preview.Height = (int)ReadTiffNumber(bytes, type, valOff, u16, u32); break;
                    case 258: preview.BitsPerSample = (int)ReadTiffNumber(bytes, type, valOff, u16, u32); break;
                    case 259: preview.Compression = (int)ReadTiffNumber(bytes, type, valOff, u16, u32); break;
                    case 277: preview.SamplesPerPixel = (int)ReadTiffNumber(bytes, type, valOff, u16, u32); break;
                    case 273: hasStripOffsets = true; break;
                    case 322: tileWidth = (int)ReadTiffNumber(bytes, type, valOff, u16, u32); break;
                    case 323: tileLength = (int)ReadTiffNumber(bytes, type, valOff, u16, u32); break;
                    case 324: hasTileOffsets = true; break;
                    case 33550: // ModelPixelScale (sx, sy, sz) —— double 数组
                        if (valOff + 16 <= bytes.Length)
                        {
                            preview.PixelSizeX = f64(valOff);
                            preview.PixelSizeY = f64(valOff + 8);
                        }
                        break;
                    case 34735: // GeoKeyDirectory —— 提取 3072 (ProjectedCSType)
                        if (valOff + 8 <= bytes.Length && count >= 4)
                        {
                            int numKeys = u16(valOff + 6);
                            for (int k = 0; k < numKeys; k++)
                            {
                                int b = valOff + 8 + k * 8;
                                if (b + 7 >= bytes.Length) break;
                                if (u16(b) == 3072 && u16(b + 2) == 1)
                                {
                                    preview.EpsgCode = u16(b + 6).ToString();
                                    break;
                                }
                            }
                        }
                        break;
                }
            }

            preview.IsTiled = !hasStripOffsets && hasTileOffsets && tileWidth > 0 && tileLength > 0;
            if (preview.Width <= 0 || preview.Height <= 0)
                throw new InvalidDataException("TIFF 尺寸标签缺失: " + tifPath);
            return preview;
        }

        // ---------- 目录浏览与识别 ----------

        /// <summary>
        /// 浏览本地目录中的数据文件（.shp/.tif/.tiff/.zip），按名称排序。
        /// </summary>
        /// <param name="dirPath">目录路径。</param>
        /// <returns>数据文件条目列表。</returns>
        public static IList<DataFileEntry> BrowseDirectory(string dirPath)
        {
            if (string.IsNullOrEmpty(dirPath)) throw new ArgumentNullException(nameof(dirPath));
            if (!Directory.Exists(dirPath)) throw new DirectoryNotFoundException("目录不存在: " + dirPath);

            var entries = new List<DataFileEntry>();
            foreach (var file in Directory.GetFiles(dirPath))
            {
                var kind = ClassifyFile(file);
                if (kind == ImportDataSourceKind.Unknown) continue;
                var info = new FileInfo(file);
                entries.Add(new DataFileEntry
                {
                    Name = info.Name,
                    FullPath = info.FullName,
                    Kind = kind,
                    SizeBytes = info.Length,
                });
            }
            return entries.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// 识别路径的数据源类型：目录（含 .shp）→ ShapefileDirectory；.shp/.tif/.zip → 对应类型。
        /// </summary>
        /// <param name="path">本地路径（文件或目录）。</param>
        /// <returns>识别结果；无法识别为 Unknown。</returns>
        public static ImportDataSourceKind DetectKind(string path)
        {
            if (string.IsNullOrEmpty(path)) return ImportDataSourceKind.Unknown;
            if (Directory.Exists(path))
            {
                return Directory.GetFiles(path, "*.shp").Length > 0
                    ? ImportDataSourceKind.ShapefileDirectory
                    : ImportDataSourceKind.Unknown;
            }
            if (File.Exists(path)) return ClassifyFile(path);
            return ImportDataSourceKind.Unknown;
        }

        private static ImportDataSourceKind ClassifyFile(string file)
        {
            string ext = Path.GetExtension(file);
            if (string.IsNullOrEmpty(ext)) return ImportDataSourceKind.Unknown;
            switch (ext.ToLowerInvariant())
            {
                case ".shp": return ImportDataSourceKind.ShapefileFile;
                case ".tif":
                case ".tiff": return ImportDataSourceKind.GeoTiffFile;
                case ".zip": return ImportDataSourceKind.ZipArchive;
                default: return ImportDataSourceKind.Unknown;
            }
        }

        // ---------- file URL 校验 ----------

        /// <summary>
        /// 校验“file:”引用（GeoServer data_dir 相对/绝对路径）的格式，并给出规范化与语义提示。
        /// </summary>
        /// <param name="url">用户输入的引用。</param>
        /// <returns>校验结果。</returns>
        public static FileUrlValidation ValidateFileUrl(string url)
        {
            var result = new FileUrlValidation();
            if (string.IsNullOrWhiteSpace(url))
            {
                result.IsValid = false;
                result.Message = "引用不能为空";
                return result;
            }

            string v = url.Trim();
            if (!v.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                result.IsValid = false;
                result.Message = "引用需以 “file:” 开头（值相对 GeoServer data_dir 或为其可达的绝对路径）";
                return result;
            }

            string body = v.Substring(5);
            if (string.IsNullOrWhiteSpace(body))
            {
                result.IsValid = false;
                result.Message = "“file:” 后缺少路径";
                return result;
            }

            result.IsValid = true;
            result.Normalized = "file:" + body.Trim();

            var sb = new StringBuilder();
            if (body.Contains("\\"))
                sb.Append("路径含反斜杠，建议使用正斜杠；");
            if (body.Length >= 2 && char.IsLetter(body[0]) && body[1] == ':')
                sb.Append("检测到 Windows 盘符，请确认 GeoServer 侧为同构路径；");
            if (!body.StartsWith("/", StringComparison.Ordinal))
                sb.Append("相对路径将相对 GeoServer data_dir 解析；");
            result.Message = sb.Length > 0 ? sb.ToString() : null;
            return result;
        }

        // ---------- 内部辅助 ----------

        private sealed class DbfParseResult
        {
            public int RecordCount;
            public List<DbfFieldInfo> Fields = new List<DbfFieldInfo>();
        }

        private static DbfParseResult ParseDbfHeader(string dbfPath)
        {
            var head = new byte[32];
            int headerLength;
            int recordCount;
            using (var fs = new FileStream(dbfPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (fs.Length < 32) throw new InvalidDataException("DBF 头不足: " + dbfPath);
                ReadExactly(fs, head, 0, 32);
                recordCount = BitConverter.ToInt32(head, 4);
                headerLength = BitConverter.ToInt16(head, 8);
                if (headerLength < 33 || headerLength > fs.Length) headerLength = (int)Math.Min(fs.Length, 32 + 32 * 128);
            }

            var buf = new byte[headerLength];
            using (var fs = new FileStream(dbfPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                ReadExactly(fs, buf, 0, Math.Min(headerLength, (int)fs.Length));
            }

            var result = new DbfParseResult { RecordCount = recordCount };
            int pos = 32;
            while (pos + 32 <= buf.Length && buf[pos] != 0x0D)
            {
                string name = Encoding.ASCII.GetString(buf, pos, 11);
                int st = name.IndexOf('\0');
                result.Fields.Add(new DbfFieldInfo
                {
                    Name = (st >= 0 ? name.Substring(0, st) : name).Trim(),
                    Type = (char)buf[pos + 11],
                    Length = buf[pos + 16],
                    Decimals = buf[pos + 17],
                });
                pos += 32;
            }
            return result;
        }

        private static int TiffTypeSize(int type)
        {
            switch (type)
            {
                case 1:
                case 2:
                case 6:
                case 7: return 1;
                case 3:
                case 8: return 2;
                case 4:
                case 9:
                case 11: return 4;
                case 5:
                case 10:
                case 12: return 8;
                default: return 1;
            }
        }

        private static uint ReadTiffNumber(byte[] bytes, int type, int valOff, Func<int, ushort> u16, Func<int, uint> u32)
        {
            if (valOff + TiffTypeSize(type) > bytes.Length) return 0;
            switch (type)
            {
                case 3: return u16(valOff);
                case 4: return u32(valOff);
                case 1: return bytes[valOff];
                default: return u16(valOff);
            }
        }

        private static void ReadExactly(Stream stream, byte[] buffer, int offset, int count)
        {
            int read = 0;
            while (read < count)
            {
                int n = stream.Read(buffer, offset + read, count - read);
                if (n <= 0) throw new EndOfStreamException("读取文件时提前到达末尾");
                read += n;
            }
        }
    }
}
