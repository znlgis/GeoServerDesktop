using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Infrastructure;
using GeoServerDesktop.Tests.RealData;

namespace GeoServerDesktop.Tests.RealDataTests
{
    /// <summary>
    /// 任务7：外部真实数据“发现式”泛化检查（数据无关 harness 模式）。仅当 GSD_REAL_DATA_DIR 提供时启用，
    /// 否则 Skip。递归发现成对 .shp/.dbf → 文件头交叉校验（DBF 记录数 vs SHP 逐记录扫描数一致）→
    /// 发布到独立 gdtest_ext 工作空间（url 指向该目录的容器内映射；无法映射则注明 Skip）→ WFS countMatched 与
    /// 文件头记录数一致 → 结束删除 gdtest_ext。期望值只来自文件头。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class DiscoverExternalRealDataTests : GeoServerTestBase
    {
        private const string ExtWs = "gdtest_ext";

        public DiscoverExternalRealDataTests(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public void Discover_Publish_Verify()
        {
            if (!RequireGeoServer()) return;
            var dir = TestEnv.RealDataDir;
            if (string.IsNullOrWhiteSpace(dir))
            { SkipLog.Skip("未提供 GSD_REAL_DATA_DIR，跳过外部真实数据泛化检查"); return; }

            var pairs = DataEnv.DiscoverShapefilePairs(dir);
            if (pairs.Count == 0)
            { SkipLog.Skip("GSD_REAL_DATA_DIR 下未发现成对 .shp/.dbf，跳过"); return; }

            var results = new List<CheckResult>();
            var publishedNames = new List<string>();
            try
            {
                using var factory = Fx.Factory();
                Try(() => factory.CreateWorkspaceService().CreateWorkspaceAsync(ExtWs));

                foreach (var (shp, dbf) in pairs)
                {
                    string baseName = Path.GetFileNameWithoutExtension(shp);
                    int shpCount = ShapefileHeader.CountRecords(shp);
                    int dbfCount = DbfHeader.Parse(dbf).RecordCount;
                    // 文件头交叉：SH P 逐记录扫描数 == DBF 头记录数
                    results.Add(Check.Cond(shpCount == dbfCount, "Ext/HeaderCross:" + baseName,
                        $"SHP 扫描 {shpCount} == DBF 记录 {dbfCount}",
                        $"SHP={shpCount} != DBF={dbfCount}"));

                    // 目录须可映射到容器 data_dir，否则记 Skip 语义（信息性）
                    var refDir = DataEnv.HostPathToDataDirRef(Path.GetDirectoryName(shp));
                    if (refDir == null)
                    { results.Add(Check.Warn("Ext/Map:" + baseName, "外部目录不在挂载卷内，无法发布（须置于 data_dir 挂载内）——跳过发布")); continue; }

                    string store = Sanitize("ds_" + baseName);
                    string layer = Sanitize(baseName);
                    if (Try(() => factory.CreateDataStoreService().CreateDataStoreAsync(ExtWs, new DataStore
                    {
                        Name = store, Type = "Shapefile", Enabled = true,
                        ConnectionParameters = new ConnectionParameters { Entries = new[] { new ConnectionParameterEntry { Key = "url", Value = refDir } } }
                    })))
                    {
                        Try(() => factory.CreateFeatureTypeService().CreateFeatureTypeAsync(ExtWs, store, new FeatureType
                        {
                            Name = layer, NativeName = baseName, Srs = "EPSG:4326", Enabled = true,
                            Namespace = new NamespaceReference { Name = ExtWs }
                        }));
                        publishedNames.Add(layer);
                        var nm = HitsOf(ExtWs + ":" + layer);
                        results.Add(Check.Cond(nm == shpCount, "Ext/WfsCount:" + baseName,
                            $"WFS countMatched={nm} 与文件头 {shpCount} 一致",
                            $"WFS={nm} != 文件头={shpCount}"));
                    }
                }
            }
            finally
            {
                try { using var f2 = Fx.Factory(); f2.CreateWorkspaceService().DeleteWorkspaceAsync(ExtWs, true).GetAwaiter().GetResult(); } catch { }
            }
            Check.ThrowOnFail(results.ToArray());
        }

        private static int HitsOf(string typeName)
        {
            var url = OgcProbe.Wfs("request=GetFeature", "version=2.0.0", "resultType=hits",
                "outputFormat=application/json", "typeName=" + typeName, "count=1");
            var t = OgcProbe.Get(url).Text ?? "";
            int i = t.IndexOf("numberMatched", StringComparison.Ordinal);
            if (i < 0) return -1;
            var digits = new string(t.Skip(i).SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out var v) ? v : -1;
        }

        private static bool Try(Func<System.Threading.Tasks.Task> act)
        {
            try { act().GetAwaiter().GetResult(); return true; } catch { return false; }
        }
        private static string Sanitize(string s) => new string(s.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray());
    }
}
