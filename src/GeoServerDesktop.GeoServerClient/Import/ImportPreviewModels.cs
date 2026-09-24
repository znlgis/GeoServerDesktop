using System.Collections.Generic;

namespace GeoServerDesktop.GeoServerClient.Import
{
    /// <summary>导入向导：本地数据源类型。</summary>
    public enum ImportDataSourceKind
    {
        /// <summary>未识别。</summary>
        Unknown = 0,

        /// <summary>目录（含 .shp 文件组），发布为 Shapefile 目录存储。</summary>
        ShapefileDirectory = 1,

        /// <summary>单个 .shp 文件（需同目录配套 .dbf/.shx/.prj）。</summary>
        ShapefileFile = 2,

        /// <summary>GeoTIFF 栅格文件（.tif/.tiff）。</summary>
        GeoTiffFile = 3,

        /// <summary>ZIP 归档（内容无法本地预览，供上传/服务器侧解压）。</summary>
        ZipArchive = 4,

        /// <summary>数据库表（PostGIS），经连接参数发布为 PostGIS 存储。</summary>
        Postgis = 5,
    }

    /// <summary>导入向导：目录浏览条目。</summary>
    public sealed class DataFileEntry
    {
        /// <summary>条目名（文件/目录名）。</summary>
        public string Name { get; set; }

        /// <summary>完整路径。</summary>
        public string FullPath { get; set; }

        /// <summary>识别出的数据源类型。</summary>
        public ImportDataSourceKind Kind { get; set; }

        /// <summary>文件大小（字节；目录为 0）。</summary>
        public long SizeBytes { get; set; }
    }

    /// <summary>
    /// 导入向导：Shapefile 预检结果。基于文件头独立解析（不经 GDAL），
    /// 与测试侧 RealData 的解析技术同源（.shp 头 bbox/类型、.dbf 字段/记录数、.prj EPSG）。
    /// </summary>
    public sealed class ShapefilePreview
    {
        /// <summary>.shp 文件路径。</summary>
        public string ShpPath { get; set; }

        /// <summary>几何类型编码（1=Point、3=PolyLine、5=Polygon 等）。</summary>
        public int ShapeType { get; set; }

        /// <summary>几何类型名。</summary>
        public string ShapeTypeName { get; set; }

        /// <summary>包络框 X 最小值。</summary>
        public double MinX { get; set; }

        /// <summary>包络框 Y 最小值。</summary>
        public double MinY { get; set; }

        /// <summary>包络框 X 最大值。</summary>
        public double MaxX { get; set; }

        /// <summary>包络框 Y 最大值。</summary>
        public double MaxY { get; set; }

        /// <summary>记录数（来自 .dbf 头；无 .dbf 时为 -1）。</summary>
        public int RecordCount { get; set; }

        /// <summary>EPSG 代码（来自 .prj 解析；无法确定时为 null）。</summary>
        public string EpsgCode { get; set; }

        /// <summary>投影/坐标系名称（.prj WKT 首个引号名；无法确定时为 null）。</summary>
        public string ProjectionName { get; set; }

        /// <summary>DBF 字段列表。</summary>
        public List<DbfFieldInfo> Fields { get; set; }

        /// <summary>是否存在 .dbf。</summary>
        public bool HasDbf { get; set; }

        /// <summary>是否存在 .shx。</summary>
        public bool HasShx { get; set; }

        /// <summary>是否存在 .prj。</summary>
        public bool HasPrj { get; set; }

        /// <summary>是否存在 .cpg。</summary>
        public bool HasCpg { get; set; }

        /// <summary>初始化 ShapefilePreview。</summary>
        public ShapefilePreview()
        {
            Fields = new List<DbfFieldInfo>();
            RecordCount = -1;
        }
    }

    /// <summary>导入向导：DBF 字段描述。</summary>
    public sealed class DbfFieldInfo
    {
        /// <summary>字段名。</summary>
        public string Name { get; set; }

        /// <summary>字段类型字符（C/N/D/L/F）。</summary>
        public char Type { get; set; }

        /// <summary>字段长度。</summary>
        public int Length { get; set; }

        /// <summary>小数位。</summary>
        public int Decimals { get; set; }
    }

    /// <summary>导入向导：GeoTIFF 预检结果（经典 TIFF/GeoTIFF 头独立解析）。</summary>
    public sealed class GeoTiffPreview
    {
        /// <summary>.tif 文件路径。</summary>
        public string TifPath { get; set; }

        /// <summary>栅格宽度（像素）。</summary>
        public int Width { get; set; }

        /// <summary>栅格高度（像素）。</summary>
        public int Height { get; set; }

        /// <summary>位深。</summary>
        public int BitsPerSample { get; set; }

        /// <summary>波段数。</summary>
        public int SamplesPerPixel { get; set; }

        /// <summary>压缩方式（1=无压缩）。</summary>
        public int Compression { get; set; }

        /// <summary>是否为瓦片布局。</summary>
        public bool IsTiled { get; set; }

        /// <summary>EPSG 代码（GeoKey 3072；无法确定时为 null）。</summary>
        public string EpsgCode { get; set; }

        /// <summary>X 方向像素分辨率（ModelPixelScale，0=未知）。</summary>
        public double PixelSizeX { get; set; }

        /// <summary>Y 方向像素分辨率（ModelPixelScale，0=未知）。</summary>
        public double PixelSizeY { get; set; }

        /// <summary>文件大小（字节）。</summary>
        public long FileSizeBytes { get; set; }
    }

    /// <summary>导入向导：file URL 校验结果。</summary>
    public sealed class FileUrlValidation
    {
        /// <summary>是否通过格式校验。</summary>
        public bool IsValid { get; set; }

        /// <summary>规范化后的引用（去空白；保持用户语义）。</summary>
        public string Normalized { get; set; }

        /// <summary>提示信息（非致命警告，如“相对 data_dir 解析”语义说明）。</summary>
        public string Message { get; set; }
    }
}
