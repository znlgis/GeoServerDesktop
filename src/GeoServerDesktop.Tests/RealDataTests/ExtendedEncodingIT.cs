using System;
using System.IO;
using GeoServerDesktop.GeoServerClient.Import;
using GeoServerDesktop.Tests.Infrastructure;
using GeoServerDesktop.Tests.RealData;
using Xunit;

namespace GeoServerDesktop.Tests.RealDataTests
{
    /// <summary>
    /// 真实数据常态守护：客户端预检对扩展数据集三种 DBF 编码变体的判定
    /// （不需要 GeoServer；数据目录缺失时按 SkipLog 显式登记跳过）。
    /// </summary>
    public class ExtendedEncodingIT
    {
        [Fact]
        public static void GeoFileInspector_OnExtendedFixtureFiles()
        {
            var root = DataEnv.ContainerDataRoot;
            var vec = Path.Combine(root, "gdtest_vec");
            if (!Directory.Exists(vec))
            {
                SkipLog.Skip("扩展数据集未生成（" + vec + "）");
                return;
            }
            var gbkNo = GeoFileInspector.InspectShapefile(Path.Combine(vec, "enc_gbk_nocpg.shp"));
            Assert.Equal(DbfEncodingRisk.UndeclaredNonUtf8, gbkNo.EncodingRisk);
            Assert.True(gbkNo.DbfHasNonAscii);
            Assert.False(gbkNo.DbfLooksUtf8);

            var gbkCpg = GeoFileInspector.InspectShapefile(Path.Combine(vec, "enc_gbk_cpg.shp"));
            Assert.Equal(DbfEncodingRisk.None, gbkCpg.EncodingRisk);
            Assert.Equal("GBK", gbkCpg.CpgEncoding);

            var utf8 = GeoFileInspector.InspectShapefile(Path.Combine(vec, "enc_utf8.shp"));
            Assert.Equal(DbfEncodingRisk.None, utf8.EncodingRisk);
            Assert.Equal("UTF-8", utf8.CpgEncoding);
            Assert.True(utf8.DbfHasNonAscii);
            Assert.True(utf8.DbfLooksUtf8);

            // 自定义投影（真实中国数据同型）：可发布，但须提示无 EPSG 权威码
            var noprj = GeoFileInspector.InspectShapefile(Path.Combine(root, "gdtest_bad", "bd_noprj.shp"));
            Assert.Equal(CrsDeclarationRisk.MissingPrj, noprj.CrsRisk);
        }
    }
}
