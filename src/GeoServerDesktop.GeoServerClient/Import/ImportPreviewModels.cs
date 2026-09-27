using System;
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

        /// <summary>SHP 头声明的文件长度（字节）。</summary>
        public long ShpHeaderFileLengthBytes { get; set; }

        /// <summary>SHP 实际文件长度（字节）。</summary>
        public long ShpActualFileLengthBytes { get; set; }

        /// <summary>DBF 头声明的记录数。</summary>
        public int DbfHeaderRecordCount { get; set; }

        /// <summary>DBF 按“头长 + 记录长”可容纳的记录数（与声明数不符即属性表与几何不同步）。</summary>
        public int DbfRecordsBySize { get; set; }

        /// <summary>.prj 存在且结构可解析（括号配平、以已知 WKT 关键字开头）。</summary>
        public bool PrjParseable { get; set; }

        /// <summary>数据完整性风险（结构性缺陷：缺表/长度不符/记录数不符/投影文件非法）。</summary>
        public ShapefileIntegrityRisk IntegrityRisk { get; set; }

        /// <summary>是否存在 .cpg。</summary>
        public bool HasCpg { get; set; }

        /// <summary>.cpg 声明的编码名（内容规范化后大写；无 .cpg 或为空时为 null）。</summary>
        public string CpgEncoding { get; set; }

        /// <summary>属性表文本字节中是否出现非 ASCII（&gt;=0x80）字节。</summary>
        public bool DbfHasNonAscii { get; set; }

        /// <summary>属性表文本字节是否是合法 UTF-8（仅在 DbfHasNonAscii 时有意义）。</summary>
        public bool DbfLooksUtf8 { get; set; }

        /// <summary>属性编码风险等级（服务端解码结果依赖 .cpg 声明，风险须在发布前告知用户）。</summary>
        public DbfEncodingRisk EncodingRisk { get; set; }

        /// <summary>投影声明风险（缺 .prj 或 .prj 无法识别 EPSG 时非 None）。</summary>
        public CrsDeclarationRisk CrsRisk { get; set; }

        /// <summary>人类可读告警（中文默认文案；无风险为空列表。UI 可据风险枚举本地化重写）。</summary>
        public List<string> Warnings { get; set; }

        /// <summary>初始化 ShapefilePreview。</summary>
        public ShapefilePreview()
        {
            Fields = new List<DbfFieldInfo>();
            Warnings = new List<string>();
            RecordCount = -1;
        }
    }

    /// <summary>
    /// DBF 属性编码风险。GeoServer/GeoTools 读 shapefile 属性表时只认 .cpg（无声明则按平台默认
    /// ISO-8859-1 解码），因此「含中文等非 ASCII 文本但缺 .cpg」的数据发布后属性面会静默变乱码——
    /// 属服务端解码契约，客户端无法代为改正，只能在预检阶段显式告知（M2 向导预检项）。
    /// </summary>
    public enum DbfEncodingRisk
    {
        /// <summary>无风险（纯 ASCII，或 .cpg 已声明）。</summary>
        None = 0,

        /// <summary>缺 .cpg、含非 ASCII 且字节非法 UTF-8：几乎必为 GBK/GB2312 等本地编码，服务端将乱码。</summary>
        UndeclaredNonUtf8 = 1,

        /// <summary>缺 .cpg、含非 ASCII 但字节合法 UTF-8：多数环境可正常读出，但建议显式声明以消除歧义。</summary>
        UndeclaredUtf8Bytes = 2,

        /// <summary>.cpg 声明了客户端无法识别的编码名：按平台默认解码的风险须提示用户确认。</summary>
        UnknownCpgDeclaration = 3,
    }

    /// <summary>
    /// Shapefile 数据完整性风险。GeoServer 对结构性损坏的 shapefile **不会在发布时拒绝**
    /// （REST 建 featureType 一律 201），缺陷要等服务查询/出图才暴露——所以客户端必须在发布前把关，
    /// 否则用户拿到的是“发布成功但图层不可用”。判定全部由文件头 O(1) 推导，不做全量扫描。
    /// </summary>
    [Flags]
    public enum ShapefileIntegrityRisk
    {
        /// <summary>无结构性问题。</summary>
        None = 0,

        /// <summary>缺 .dbf 属性表：图层可建但无属性面，WFS 属性/GetFeatureInfo 不可用。</summary>
        MissingDbf = 1,

        /// <summary>缺 .shx 索引：仍可读，但空间过滤退化为全量扫描（性能风险，非致命）。</summary>
        MissingShx = 2,

        /// <summary>SHP 头声明长度与实际文件长度不符：文件被截断或头被改写。</summary>
        ShpLengthMismatch = 4,

        /// <summary>DBF 头声明记录数与按记录长可容纳的记录数不符：属性表与几何不同步。</summary>
        DbfRecordCountMismatch = 8,

        /// <summary>.prj 存在但内容不是可解析的 WKT。</summary>
        PrjUnparseable = 16,
    }

    /// <summary>投影（.prj）声明风险：缺 .prj 或无法解析出 EPSG 时，服务端只能按经纬度处理或发布失败。</summary>
    public enum CrsDeclarationRisk
    {
        /// <summary>已声明且可识别。</summary>
        None = 0,

        /// <summary>缺 .prj 文件。</summary>
        MissingPrj = 1,

        /// <summary>有 .prj 但内容非法或不含可识别的 EPSG 权威码（自定义投影）。</summary>
        UnrecognizedPrj = 2,
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
