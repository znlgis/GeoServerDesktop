using System;
using System.IO;
using System.Linq;
using System.Text;
using GeoServerDesktop.GeoServerClient.Import;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>
/// GeoFileInspector 离线单元测试：手工构造最小合法 shp/dbf/prj/tif 文件头，
/// 验证导入向导预检的独立解析（bbox/SRID/字段/记录数/栅格尺寸）与目录浏览/识别/校验。
/// </summary>
public class GeoFileInspectorTests : IDisposable
{
    private readonly string _dir;

    public GeoFileInspectorTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "gfi_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    // ---------- 构造辅助 ----------

    private string WriteShp(string baseName, int shapeType = 5,
        double minX = -10, double minY = -20, double maxX = 30, double maxY = 40)
    {
        var b = new byte[100];
        b[0] = 0; b[1] = 0; b[2] = 0x27; b[3] = 0x0A;          // file code 9994（大端）
        b[24] = 0; b[25] = 0; b[26] = 0; b[27] = 50;            // file length = 50 字 = 100 字节（大端）
        BitConverter.GetBytes(1000).CopyTo(b, 28);              // version
        BitConverter.GetBytes(shapeType).CopyTo(b, 32);
        BitConverter.GetBytes(minX).CopyTo(b, 36);
        BitConverter.GetBytes(minY).CopyTo(b, 44);
        BitConverter.GetBytes(maxX).CopyTo(b, 52);
        BitConverter.GetBytes(maxY).CopyTo(b, 60);
        var p = Path.Combine(_dir, baseName + ".shp");
        File.WriteAllBytes(p, b);
        return p;
    }

    private string WriteDbf(string baseName, int recordCount = 3,
        params (string Name, char Type, int Len, int Dec)[] fields)
    {
        int headerLen = 32 + fields.Length * 32 + 1;
        var b = new byte[headerLen];
        b[0] = 0x03;
        BitConverter.GetBytes(recordCount).CopyTo(b, 4);
        BitConverter.GetBytes((short)headerLen).CopyTo(b, 8);
        BitConverter.GetBytes((short)1).CopyTo(b, 10);
        int pos = 32;
        foreach (var f in fields)
        {
            var name = Encoding.ASCII.GetBytes(f.Name);
            Array.Copy(name, 0, b, pos, Math.Min(11, name.Length));
            b[pos + 11] = (byte)f.Type;
            b[pos + 16] = (byte)f.Len;
            b[pos + 17] = (byte)f.Dec;
            pos += 32;
        }
        b[pos] = 0x0D;
        var p = Path.Combine(_dir, baseName + ".dbf");
        File.WriteAllBytes(p, b);
        return p;
    }

    private string WritePrj(string baseName, string wkt)
    {
        var p = Path.Combine(_dir, baseName + ".prj");
        File.WriteAllText(p, wkt, new UTF8Encoding(false));
        return p;
    }

    private string WriteTiff(string baseName, int width, int height)
    {
        var b = new byte[36];
        b[0] = 0x49; b[1] = 0x49;                               // II（小端）
        BitConverter.GetBytes((ushort)42).CopyTo(b, 2);
        BitConverter.GetBytes((uint)8).CopyTo(b, 4);            // IFD 偏移
        BitConverter.GetBytes((ushort)2).CopyTo(b, 8);          // 条目数
        BitConverter.GetBytes((ushort)256).CopyTo(b, 10);       // ImageWidth
        BitConverter.GetBytes((ushort)4).CopyTo(b, 12);
        BitConverter.GetBytes((uint)1).CopyTo(b, 14);
        BitConverter.GetBytes((uint)width).CopyTo(b, 18);
        BitConverter.GetBytes((ushort)257).CopyTo(b, 22);       // ImageLength
        BitConverter.GetBytes((ushort)4).CopyTo(b, 24);
        BitConverter.GetBytes((uint)1).CopyTo(b, 26);
        BitConverter.GetBytes((uint)height).CopyTo(b, 30);
        var p = Path.Combine(_dir, baseName + ".tif");
        File.WriteAllBytes(p, b);
        return p;
    }

    private const string Utm54S =
        "PROJCS[\"WGS 84 / UTM zone 54S\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\"," +
        "SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]]," +
        "PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433],AUTHORITY[\"EPSG\",\"4326\"]]," +
        "PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0]," +
        "PARAMETER[\"central_meridian\",177],PARAMETER[\"scale_factor\",0.9996]," +
        "PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",10000000]," +
        "UNIT[\"metre\",1],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"32754\"]]";

    // ---------- Shapefile ----------

    [Fact]
    public void InspectShapefile_ParsesHeaderAndDbf()
    {
        var shp = WriteShp("poly", 5, -10, -20, 30, 40);
        WriteDbf("poly", 3, ("NAME", 'C', 20, 0), ("AREA", 'N', 12, 2));

        var preview = GeoFileInspector.InspectShapefile(shp);

        Assert.Equal(5, preview.ShapeType);
        Assert.Equal("Polygon", preview.ShapeTypeName);
        Assert.Equal(-10, preview.MinX);
        Assert.Equal(-20, preview.MinY);
        Assert.Equal(30, preview.MaxX);
        Assert.Equal(40, preview.MaxY);
        Assert.Equal(3, preview.RecordCount);
        Assert.Equal(2, preview.Fields.Count);
        Assert.Equal("NAME", preview.Fields[0].Name);
        Assert.Equal('C', preview.Fields[0].Type);
        Assert.Equal("AREA", preview.Fields[1].Name);
        Assert.True(preview.HasDbf);
        Assert.False(preview.HasShx);
        Assert.False(preview.HasPrj);
    }

    [Fact]
    public void InspectShapefile_WithPrj_ParsesLastAuthorityEpsg()
    {
        var shp = WriteShp("utm");
        WriteDbf("utm", 0);
        WritePrj("utm", Utm54S);

        var preview = GeoFileInspector.InspectShapefile(shp);

        Assert.Equal("32754", preview.EpsgCode);   // 取最后一个 AUTHORITY（最外层 PROJCS）
        Assert.Equal("WGS 84 / UTM zone 54S", preview.ProjectionName);
        Assert.True(preview.HasPrj);
    }

    [Fact]
    public void InspectShapefile_InvalidFileCode_Throws()
    {
        var p = Path.Combine(_dir, "bad.shp");
        var b = new byte[100];
        b[0] = 1; b[1] = 2; b[2] = 3; b[3] = 4;    // 非 9994
        File.WriteAllBytes(p, b);

        Assert.Throws<InvalidDataException>(() => GeoFileInspector.InspectShapefile(p));
    }

    [Fact]
    public void InspectShapefile_MissingFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() =>
            GeoFileInspector.InspectShapefile(Path.Combine(_dir, "nope.shp")));
    }

    // ---------- GeoTIFF ----------

    [Fact]
    public void InspectGeoTiff_ParsesDimensions()
    {
        var tif = WriteTiff("dem", 512, 256);

        var preview = GeoFileInspector.InspectGeoTiff(tif);

        Assert.Equal(512, preview.Width);
        Assert.Equal(256, preview.Height);
        Assert.Equal(36, preview.FileSizeBytes);
        Assert.False(preview.IsTiled);
    }

    [Fact]
    public void InspectGeoTiff_InvalidMagic_Throws()
    {
        var p = Path.Combine(_dir, "bad.tif");
        var b = new byte[16];
        b[0] = 0x49; b[1] = 0x49;
        b[2] = 1; b[3] = 2;                        // magic != 42
        File.WriteAllBytes(p, b);

        Assert.Throws<InvalidDataException>(() => GeoFileInspector.InspectGeoTiff(p));
    }

    // ---------- EPSG 提取（WKT2） ----------

    [Fact]
    public void ExtractEpsgCode_Wkt2IdForm()
    {
        Assert.Equal("4326", GeoFileInspector.ExtractEpsgCode(
            "GEOGCRS[\"WGS 84\",ID[\"EPSG\",4326]]"));
        Assert.Null(GeoFileInspector.ExtractEpsgCode("not a wkt"));
    }

    // ---------- 目录浏览与识别 ----------

    [Fact]
    public void BrowseDirectory_FindsAndSortsDataFiles()
    {
        WriteShp("b_layer");
        WriteTiff("a_dem", 8, 8);
        File.WriteAllBytes(Path.Combine(_dir, "c_arch.zip"), new byte[] { 0x50, 0x4B });
        File.WriteAllText(Path.Combine(_dir, "d_note.txt"), "x");

        var entries = GeoFileInspector.BrowseDirectory(_dir);

        Assert.Equal(3, entries.Count);
        Assert.Equal("a_dem.tif", entries[0].Name);
        Assert.Equal(ImportDataSourceKind.GeoTiffFile, entries[0].Kind);
        Assert.Equal("b_layer.shp", entries[1].Name);
        Assert.Equal(ImportDataSourceKind.ShapefileFile, entries[1].Kind);
        Assert.Equal("c_arch.zip", entries[2].Name);
        Assert.Equal(ImportDataSourceKind.ZipArchive, entries[2].Kind);
    }

    [Fact]
    public void BrowseDirectory_MissingDir_Throws()
    {
        Assert.Throws<DirectoryNotFoundException>(() =>
            GeoFileInspector.BrowseDirectory(Path.Combine(_dir, "nope")));
    }

    [Fact]
    public void DetectKind_CoversDirectoryAndFiles()
    {
        WriteShp("kindly");
        Assert.Equal(ImportDataSourceKind.ShapefileDirectory, GeoFileInspector.DetectKind(_dir));

        var tif = WriteTiff("kind2", 4, 4);
        Assert.Equal(ImportDataSourceKind.GeoTiffFile, GeoFileInspector.DetectKind(tif));

        var txt = Path.Combine(_dir, "x.txt");
        File.WriteAllText(txt, "x");
        Assert.Equal(ImportDataSourceKind.Unknown, GeoFileInspector.DetectKind(txt));

        var empty = Path.Combine(_dir, "empty_sub");
        Directory.CreateDirectory(empty);
        Assert.Equal(ImportDataSourceKind.Unknown, GeoFileInspector.DetectKind(empty));
    }

    // ---------- file URL 校验 ----------

    [Fact]
    public void ValidateFileUrl_EmptyOrNoPrefix_Invalid()
    {
        Assert.False(GeoFileInspector.ValidateFileUrl(null).IsValid);
        Assert.False(GeoFileInspector.ValidateFileUrl("   ").IsValid);
        Assert.False(GeoFileInspector.ValidateFileUrl("data/x.shp").IsValid);
        Assert.False(GeoFileInspector.ValidateFileUrl("file:").IsValid);
    }

    [Fact]
    public void ValidateFileUrl_RelativeRef_ValidWithDataDirHint()
    {
        var v = GeoFileInspector.ValidateFileUrl("  file:gdtest_data  ");

        Assert.True(v.IsValid);
        Assert.Equal("file:gdtest_data", v.Normalized);
        Assert.Contains("data_dir", v.Message);
    }

    [Fact]
    public void ValidateFileUrl_WindowsDrive_Warns()
    {
        var v = GeoFileInspector.ValidateFileUrl(@"file:C:\data\shapes");

        Assert.True(v.IsValid);
        Assert.Contains("反斜杠", v.Message);
        Assert.Contains("盘符", v.Message);
    }

    [Fact]
    public void ValidateFileUrl_AbsolutePosix_NoRelativeHint()
    {
        var v = GeoFileInspector.ValidateFileUrl("file:/mnt/data/shapes");

        Assert.True(v.IsValid);
        Assert.Null(v.Message);
    }
}
