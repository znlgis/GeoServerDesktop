using System;
using System.IO;
using System.Text;
using GeoServerDesktop.GeoServerClient.Import;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit
{
    /// <summary>
    /// E42：shapefile 属性编码与投影声明风险的预检检测（纯离线，不依赖服务器）。
    /// 风险判定必须只由数据文件自身字节与 .cpg/.prj 推导——服务端解码契约不可由客户端改正，
    /// 因此“能否在发布前告知用户”就是本项的产品正确性标准。
    /// </summary>
    public class DbfEncodingRiskTests
    {
        // ---------- IsValidUtf8 合成字节 ----------
        [Theory]
        [InlineData(new byte[] { (byte)'A', (byte)'B' }, true)]                      // 纯 ASCII
        [InlineData(new byte[] { 0xE5, 0x8C, 0x97, 0xE4, 0xBA, 0xAC }, true)]        // “北京” UTF-8
        [InlineData(new byte[] { 0xB1, 0xB1, 0xBE, 0xA9 }, false)]                   // “北京” GBK
        [InlineData(new byte[] { 0xD6, 0xD0, 0xB9, 0xFA }, false)]                   // “中国” GBK
        [InlineData(new byte[] { 0xC3, 0xA9 }, true)]                                // 合法二字节（é）
        [InlineData(new byte[] { 0xC3, 0x28 }, false)]                               // 非法续字节
        [InlineData(new byte[] { 0xE4, 0xB8 }, false)]                               // 截断三字节
        [InlineData(new byte[] { 0x80, 0x80 }, false)]                               // 落单续字节
        [InlineData(new byte[] { 0xC0, 0x80 }, false)]                               // 过长编码
        [InlineData(new byte[] { 0xED, 0xA0, 0x80 }, false)]                         // UTF-16 代理区
        [InlineData(new byte[] { 0xF0, 0x9F, 0x98, 0x80 }, true)]                    // emoji（4 字节）
        [InlineData(new byte[] { 0xF4, 0x90, 0x80, 0x80 }, false)]                   // 越界码位
        public static void IsValidUtf8_ClassifiesByteSequences(byte[] bytes, bool expected)
        {
            Assert.Equal(expected, GeoFileInspector.IsValidUtf8(bytes, 0, bytes.Length));
        }

        [Fact]
        public static void IsValidUtf8_TreatsNulAndSpacePaddingAsFill()
        {
            // DBF 文本字段以空格/零字节填充，不得被当成非法序列
            var b = Encoding.UTF8.GetBytes("北京");
            var padded = new byte[b.Length + 10];
            Array.Copy(b, padded, b.Length);
            Assert.True(GeoFileInspector.IsValidUtf8(padded, 0, padded.Length));
            var zeroFilled = new byte[b.Length + 10];
            Array.Copy(b, zeroFilled, b.Length);
            Assert.True(GeoFileInspector.IsValidUtf8(zeroFilled, 0, zeroFilled.Length));
        }

        // ---------- 风险判定矩阵 ----------
        [Theory]
        [InlineData(false, true, null, DbfEncodingRisk.None)]                        // 纯 ASCII → 无风险
        [InlineData(false, true, "UTF-8", DbfEncodingRisk.None)]
        [InlineData(true, false, "UTF-8", DbfEncodingRisk.None)]                     // 有声明 → 交给服务端按声明解码
        [InlineData(true, true, "GBK", DbfEncodingRisk.None)]
        [InlineData(true, false, null, DbfEncodingRisk.UndeclaredNonUtf8)]           // 中文 GBK 且无声明 → 必报
        [InlineData(true, true, null, DbfEncodingRisk.UndeclaredUtf8Bytes)]          // UTF-8 无声明 → 提示补声明
        [InlineData(true, true, "FOOBAR", DbfEncodingRisk.UnknownCpgDeclaration)]    // 声明不可识别
        public static void EvaluateEncodingRisk_Matrix(bool nonAscii, bool validUtf8, string? cpg, DbfEncodingRisk expected)
            => Assert.Equal(expected, GeoFileInspector.EvaluateEncodingRisk(nonAscii, validUtf8, cpg));

        [Theory]
        [InlineData(false, null, null, CrsDeclarationRisk.MissingPrj)]
        [InlineData(true, "4326", "WGS 84", CrsDeclarationRisk.None)]
        [InlineData(true, null, "China_Lambert_Conformal_Conic", CrsDeclarationRisk.UnrecognizedPrj)]
        [InlineData(true, null, null, CrsDeclarationRisk.UnrecognizedPrj)]
        public static void EvaluateCrsRisk_Matrix(bool hasPrj, string? epsg, string? name, CrsDeclarationRisk expected)
            => Assert.Equal(expected, GeoFileInspector.EvaluateCrsRisk(hasPrj, epsg, name));

        // ---------- 端到端：自造最小 DBF，验证扫描与告警文案 ----------
        [Fact]
        public static void InspectShapefile_FlagsGbkAttributesWithoutCpg()
        {
            var dir = Path.Combine(Path.GetTempPath(), "gsd_enc_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var shp = Path.Combine(dir, "probe.shp");
                var dbf = Path.Combine(dir, "probe.dbf");
                WriteMinimalShp(shp);
                WriteDbf(dbf, new[] { "NAME" }, new[] { new[] { GBK("北京市") } });   // GBK 字节，无 .cpg

                var p = GeoFileInspector.InspectShapefile(shp);
                Assert.True(p.DbfHasNonAscii);
                Assert.False(p.DbfLooksUtf8);
                Assert.False(p.HasCpg);
                Assert.Equal(DbfEncodingRisk.UndeclaredNonUtf8, p.EncodingRisk);
                Assert.NotEmpty(p.Warnings);
                Assert.Contains(p.Warnings, w => w.Contains("ISO-8859-1"));
                Assert.Equal(CrsDeclarationRisk.MissingPrj, p.CrsRisk);                 // 无 .prj 亦须报
                Assert.Contains(p.Warnings, w => w.Contains(".prj"));
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }

        [Fact]
        public static void InspectShapefile_QuietWhenCpgDeclaresGbk()
        {
            var dir = Path.Combine(Path.GetTempPath(), "gsd_enc_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var shp = Path.Combine(dir, "probe.shp");
                WriteMinimalShp(shp);
                WriteDbf(Path.Combine(dir, "probe.dbf"), new[] { "NAME" }, new[] { new[] { GBK("北京市") } });
                File.WriteAllText(Path.Combine(dir, "probe.cpg"), "GBK");
                File.WriteAllText(Path.Combine(dir, "probe.prj"),
                    "GEOGCS[\"WGS 84\",DATUM[\"D_WGS_1984\"],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.0174532925],AUTHORITY[\"EPSG\",\"4326\"]]");

                var p = GeoFileInspector.InspectShapefile(shp);
                Assert.Equal(DbfEncodingRisk.None, p.EncodingRisk);                     // 已声明 → 不报编码风险
                Assert.Equal(CrsDeclarationRisk.None, p.CrsRisk);
                Assert.Empty(p.Warnings);
                Assert.Equal("4326", p.EpsgCode);
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }

        [Fact]
        public static void InspectShapefile_AsciiDataProducesNoRisk()
        {
            var dir = Path.Combine(Path.GetTempPath(), "gsd_enc_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var shp = Path.Combine(dir, "probe.shp");
                WriteMinimalShp(shp);
                WriteDbf(Path.Combine(dir, "probe.dbf"), new[] { "NAME" }, new[] { new[] { Encoding.ASCII.GetBytes("AB  ") } });
                var p = GeoFileInspector.InspectShapefile(shp);
                Assert.False(p.DbfHasNonAscii);
                Assert.Equal(DbfEncodingRisk.None, p.EncodingRisk);
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }

        [Fact]
        public static void ScanDbfTextBytes_SkipsDeletedRecords()
        {
            var dir = Path.Combine(Path.GetTempPath(), "gsd_enc_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var dbf = Path.Combine(dir, "probe.dbf");
                WriteDbf(dbf, new[] { "NAME" }, new[] { new[] { GBK("北京市") }, new[] { Encoding.ASCII.GetBytes("AB  ") } },
                    deletedFlags: new[] { false, true });           // 第二条已删除——但第一条仍非 ASCII
                bool nonAscii, validUtf8;
                GeoFileInspector.ScanDbfTextBytes(dbf, out nonAscii, out validUtf8);
                Assert.True(nonAscii);
                Assert.False(validUtf8);

                WriteDbf(dbf, new[] { "NAME" }, new[] { new[] { GBK("北京市") } }, deletedFlags: new[] { true });
                GeoFileInspector.ScanDbfTextBytes(dbf, out nonAscii, out validUtf8);
                Assert.False(nonAscii);                                            // 全部删除 → 无风险可言
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }

        // ---------------- 最小文件写手（不依赖 GDAL/任何 GIS 库） ----------------
        private static byte[] GBK(string s)
        {
            try { Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); } catch { }
            try { return Encoding.GetEncoding("GBK").GetBytes(s); }
            catch { return new byte[] { 0xB1, 0xB1, 0xBE, 0xA9 }; }   // “北京” GBK 字面量兜底
        }

        private static void WriteMinimalShp(string path)
        {
            // 只需合法 100 字节头：fileCode(0-3)=9994 大端、version(28)=1000 LE、shapeType(32)=5(Polygon) LE
            var b = new byte[100];
            b[0] = 0x00; b[1] = 0x00; b[2] = 0x27; b[3] = 0x0A;      // fileCode 9994（大端）
            b[24] = 0; b[25] = 0; b[26] = 0; b[27] = 50;             // 文件长度 50 个 16 位字 = 100 字节
            Array.Copy(BitConverter.GetBytes(1000), 0, b, 28, 4);    // version
            Array.Copy(BitConverter.GetBytes(5), 0, b, 32, 4);       // shapeType = Polygon
            File.WriteAllBytes(path, b);
            // 合成夹具同步给出 .shx（只判存在性），使“缺索引”告警不干扰本用例的断言目标
            File.WriteAllBytes(Path.ChangeExtension(path, ".shx"), b);
        }

        /// <summary>写一个单文本字段（宽度 8）的最小 DBF。</summary>
        private static void WriteDbf(string path, string[] fields, byte[][][] values, bool[] deletedFlags = null)
        {
            int fieldCount = fields.Length;
            int headerLength = 32 + 32 * fieldCount + 1;
            int recordLength = 1 + 8 * fieldCount;
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            var head = new byte[32];
            head[0] = 0x03;
            head[1] = 90; head[2] = 1; head[3] = 1;
            Array.Copy(BitConverter.GetBytes(values.Length), 0, head, 4, 4);
            Array.Copy(BitConverter.GetBytes((short)headerLength), 0, head, 8, 2);
            Array.Copy(BitConverter.GetBytes((short)recordLength), 0, head, 10, 2);
            fs.Write(head, 0, 32);
            foreach (var name in fields)
            {
                var fd = new byte[32];
                Encoding.ASCII.GetBytes(name.PadRight(11).Substring(0, 11), 0, 11, fd, 0);
                fd[11] = (byte)'C';
                fd[16] = 8;
                fs.Write(fd, 0, 32);
            }
            fs.WriteByte(0x0D);
            for (int r = 0; r < values.Length; r++)
            {
                fs.WriteByte(deletedFlags != null && deletedFlags[r] ? (byte)'*' : (byte)' ');
                for (int f = 0; f < fieldCount; f++)
                {
                    var cell = new byte[8];
                    for (int i = 0; i < 8; i++) cell[i] = 0x20;
                    var src = values[r][f];
                    Array.Copy(src, 0, cell, 0, Math.Min(src.Length, 8));
                    fs.Write(cell, 0, 8);
                }
            }
            fs.WriteByte(0x1A);
        }
    }
}
