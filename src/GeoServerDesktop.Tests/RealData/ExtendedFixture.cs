using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.GeoServerClient.Import;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.RealData
{
    /// <summary>扩展数据集中的单个条目：磁盘目录、基名、发布名、SRS 与用例性质。</summary>
    public sealed class ExtSet
    {
        /// <summary>子集键（vec / vol / bad / img），决定存储名与 file: 引用。</summary>
        public string DirKey = "";

        /// <summary>宿主绝对路径（文件侧真值解析用）。</summary>
        public string HostDir = "";

        /// <summary>磁盘基名（含 CJK 时原样保留，用于验证转义链路）。</summary>
        public string BaseName = "";

        /// <summary>发布名（CJK 用例故意保持 CJK，以验证 REST/WFS/WMS 全链路）。</summary>
        public string LayerName = "";

        /// <summary>声明 SRS；null 表示不声明（由服务端按 .prj 推断）。</summary>
        public string? Srs;

        /// <summary>栅格条目（走 coverage 发布路径）。</summary>
        public bool IsCoverage;

        /// <summary>预期发布失败（脏数据负路径用例）。</summary>
        public bool ExpectFailure;

        public string Qualified => ExtendedFixture.Ws + ":" + LayerName;
    }

    /// <summary>
    /// 扩展真实数据夹具：把 gdtest_vec / gdtest_vol / gdtest_bad / gdtest_img 中的数据集经
    /// **产品发布向导**（ImportWizardService）发布到独立工作空间 gdtest_ws_x，与既有
    /// gdtest_ws_e2e 完全隔离，避免扰动既有基线。幂等，并逐项记录发布耗时（计时基线/幂等复核）。
    /// </summary>
    public sealed class ExtendedFixture : IDisposable
    {
        public const string Ws = "gdtest_ws_x";

        private readonly GeoServerClientFactory _f;

        public List<TimeSpan> PublishTimings { get; } = new List<TimeSpan>();

        public Dictionary<string, string> PublishOutcome { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

        public static string VecDir => Path.Combine(DataEnv.ContainerDataRoot, "gdtest_vec");
        public static string VolDir => Path.Combine(DataEnv.ContainerDataRoot, "gdtest_vol");
        public static string BadDir => Path.Combine(DataEnv.ContainerDataRoot, "gdtest_bad");
        public static string ImgDir => Path.Combine(DataEnv.ContainerDataRoot, "gdtest_img");

        /// <summary>
        /// 扩展数据是否真的可用：必须看到代表性文件——CI 会预先 mkdir 挂载点，
        /// 只看目录存在会把“数据未生成”误判成“数据在但发布失败”，伪装成产品缺陷。
        /// </summary>
        public static bool DataPresent =>
            File.Exists(Path.Combine(VecDir, "gdtest_types.shp"))
            && File.Exists(Path.Combine(VecDir, "gdtest_huge.shp"))
            && File.Exists(Path.Combine(ImgDir, "gdtest_rgb.tif"))
            && File.Exists(Path.Combine(VolDir, "gdtest_pts20k.shp"))
            && File.Exists(Path.Combine(BadDir, "bd_valid.shp"));

        /// <summary>调试保留：GSD_KEEP=1 时不清理扩展夹具（手工复现定位用）。</summary>
        public static bool Keep => TestEnv.Env("GSD_KEEP", "0") == "1";

        /// <summary>全部条目（矢量形态 + 大表 + 脏数据正负例 + 栅格）。</summary>
        public static List<ExtSet> Sets() => new List<ExtSet>
        {
            new ExtSet { DirKey = "vec", HostDir = VecDir, BaseName = "gdtest_types", LayerName = "gdtest_x_types", Srs = "EPSG:4326" },
            new ExtSet { DirKey = "vec", HostDir = VecDir, BaseName = "enc_utf8", LayerName = "gdtest_x_enc_utf8", Srs = "EPSG:4326" },
            new ExtSet { DirKey = "vec", HostDir = VecDir, BaseName = "enc_gbk_cpg", LayerName = "gdtest_x_enc_gbk_cpg", Srs = "EPSG:4326" },
            new ExtSet { DirKey = "vec", HostDir = VecDir, BaseName = "enc_gbk_nocpg", LayerName = "gdtest_x_enc_gbk_nocpg", Srs = "EPSG:4326" },
            new ExtSet { DirKey = "vec", HostDir = VecDir, BaseName = "gdtest_points", LayerName = "gdtest_x_points", Srs = "EPSG:4326" },
            new ExtSet { DirKey = "vec", HostDir = VecDir, BaseName = "gdtest_polyz", LayerName = "gdtest_x_polyz", Srs = "EPSG:4326" },
            new ExtSet { DirKey = "vec", HostDir = VecDir, BaseName = "gdtest_selfint", LayerName = "gdtest_x_selfint", Srs = "EPSG:4326" },
            new ExtSet { DirKey = "vec", HostDir = VecDir, BaseName = "gdtest_empty", LayerName = "gdtest_x_empty", Srs = "EPSG:4326" },
            new ExtSet { DirKey = "vec", HostDir = VecDir, BaseName = "gdtest_huge", LayerName = "gdtest_x_huge", Srs = "EPSG:4326" },
            // CJK 与含空格基名 + CJK 发布名：转义链路全量覆盖
            new ExtSet { DirKey = "vec", HostDir = VecDir, BaseName = "湖泊 与 水库", LayerName = "湖泊 与 水库", Srs = "EPSG:4326" },
            new ExtSet { DirKey = "vol", HostDir = VolDir, BaseName = "gdtest_pts20k", LayerName = "gdtest_x_pts20k", Srs = "EPSG:4326" },
            new ExtSet { DirKey = "bad", HostDir = BadDir, BaseName = "bd_valid", LayerName = "gdtest_x_bd_valid", Srs = "EPSG:4326" },
            // 脏数据负路径：客户端预检必须拦下并给出可诊断原因（实测 GeoServer 一律回 2xx）
            new ExtSet { DirKey = "bad", HostDir = BadDir, BaseName = "bd_nodbf", LayerName = "gdtest_x_bd_nodbf", Srs = "EPSG:4326", ExpectFailure = true },
            new ExtSet { DirKey = "bad", HostDir = BadDir, BaseName = "bd_mismatch", LayerName = "gdtest_x_bd_mismatch", Srs = "EPSG:4326", ExpectFailure = true },
            new ExtSet { DirKey = "bad", HostDir = BadDir, BaseName = "bd_badprj", LayerName = "gdtest_x_bd_badprj", Srs = "EPSG:4326", ExpectFailure = true },
            new ExtSet { DirKey = "bad", HostDir = BadDir, BaseName = "bd_truncated", LayerName = "gdtest_x_bd_truncated", Srs = "EPSG:4326", ExpectFailure = true },
            new ExtSet { DirKey = "bad", HostDir = BadDir, BaseName = "bd_dbfonly", LayerName = "gdtest_x_bd_dbfonly", Srs = "EPSG:4326", ExpectFailure = true },
            // 缺 .prj：数据本身合法，允许发布，但预检必须标记“投影未声明”
            new ExtSet { DirKey = "bad", HostDir = BadDir, BaseName = "bd_noprj", LayerName = "gdtest_x_bd_noprj", Srs = "EPSG:4326" },
            new ExtSet { DirKey = "img", HostDir = ImgDir, BaseName = "gdtest_rgb.tif", LayerName = "gdtest_x_rgb", Srs = "EPSG:32754", IsCoverage = true },
            new ExtSet { DirKey = "img", HostDir = ImgDir, BaseName = "gdtest_int16.tif", LayerName = "gdtest_x_int16", Srs = "EPSG:32754", IsCoverage = true },
            new ExtSet { DirKey = "img", HostDir = ImgDir, BaseName = "中文 栅格.tif", LayerName = "中文 栅格", Srs = "EPSG:32754", IsCoverage = true },
        };

        public ExtendedFixture()
        {
            _f = new GeoServerClientFactory(GeoServerAvailability.Options());
        }

        public GeoServerClientFactory Factory => _f;

        public void Dispose()
        {
            try { _f.Dispose(); } catch { }
        }

        public void EnsureWorkspace()
        {
            var ws = _f.CreateWorkspaceService();
            try { ws.GetWorkspaceAsync(Ws).GetAwaiter().GetResult(); }
            catch { ws.CreateWorkspaceAsync(Ws).GetAwaiter().GetResult(); }
        }

        /// <summary>幂等发布一个条目（负路径条目亦经同一路径，由返回值判定）。</summary>
        public PublishResult EnsurePublished(ExtSet s)
        {
            EnsureWorkspace();
            var wiz = _f.CreateImportWizardService();
            var sw = Stopwatch.StartNew();
            PublishResult r;
            if (s.IsCoverage)
            {
                r = wiz.PublishGeoTiffAsync(new ImportSourceRequest
                {
                    Kind = ImportDataSourceKind.GeoTiffFile,
                    Workspace = Ws,
                    LayerName = s.LayerName,
                    NativeName = Path.GetFileNameWithoutExtension(s.BaseName),
                    StoreName = CoverageStoreFor(s),
                    FileRef = FileRefFor(s),
                    Srs = s.Srs,
                    LocalSourcePath = Path.Combine(s.HostDir, s.BaseName),
                }).GetAwaiter().GetResult();
            }
            else
            {
                r = wiz.PublishShapefileAsync(new ImportSourceRequest
                {
                    Kind = ImportDataSourceKind.ShapefileDirectory,
                    Workspace = Ws,
                    LayerName = s.LayerName,
                    NativeName = s.BaseName,
                    StoreName = StoreFor(s.DirKey),
                    FileRef = DirRefFor(s.DirKey),
                    Srs = s.Srs,
                    LocalSourcePath = Path.Combine(s.HostDir, s.BaseName + ".shp"),
                }).GetAwaiter().GetResult();
            }
            sw.Stop();
            PublishTimings.Add(sw.Elapsed);
            PublishOutcome[s.LayerName] = r.Success ? "ok" : (r.Message ?? "fail");
            return r;
        }

        /// <summary>负路径条目：与 EnsurePublished 同路径，返回值供“可诊断失败”断言使用。</summary>
        public PublishResult TryPublish(ExtSet s) => EnsurePublished(s);

        public static string StoreFor(string dirKey) => "gdtest_ds_" + dirKey;

        public static string CoverageStoreFor(ExtSet s) => "gdtest_xcs_" + Sanitize(s.LayerName);

        public static string DirRefFor(string dirKey)
        {
            string dir = dirKey == "vec" ? VecDir : dirKey == "vol" ? VolDir : dirKey == "bad" ? BadDir : ImgDir;
            return DataEnv.HostPathToDataDirRef(dir) ?? dir;
        }

        public static string FileRefFor(ExtSet s)
        {
            var full = Path.Combine(s.HostDir, s.BaseName);
            return DataEnv.HostPathToDataDirRef(full) ?? "file:" + full;
        }

        public static string Sanitize(string name)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in name) sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            return sb.ToString();
        }

        /// <summary>整工作空间级联清理（幂等，容忍不存在）。</summary>
        public static void Wipe(GeoServerClientFactory f)
        {
            try { f.CreateWorkspaceService().DeleteWorkspaceAsync(Ws, true).GetAwaiter().GetResult(); } catch { }
        }
    }

    /// <summary>
    /// 外部真实数据（GSD_REAL_DATA_DIR）泛化检查夹具：一律经**产品发布向导**发布。
    /// 早前此处手拼 store/featureType 请求（多塞 namespace 连接参数），对含非 ASCII 基名的
    /// 真实数据会走偏成服务端报 “no attributes were specified”，把测试路径差异误读成产品缺陷；
    /// 统一走向导既贴近用户真实动作，也让向导本身接受真实数据检验。
    /// </summary>
    public static class ExternalRealData
    {
        public const string Ws = "gdtest_ext";
        public const string Store = "gdtest_ext_ds";

        public static List<(string shp, string dbf)> Pairs() => DataEnv.DiscoverShapefilePairs(TestEnv.RealDataDir);

        public static string? DirRef() => DataEnv.HostPathToDataDirRef(TestEnv.RealDataDir);

        public static void EnsureWorkspace(GeoServerClientFactory f)
        {
            var ws = f.CreateWorkspaceService();
            try { ws.GetWorkspaceAsync(Ws).GetAwaiter().GetResult(); }
            catch { ws.CreateWorkspaceAsync(Ws).GetAwaiter().GetResult(); }
        }

        /// <summary>
        /// 向导发布一个真实 shapefile。返回 null 表示可用；否则为可诊断原因。
        /// 向导报成功不等于服务可用，故额外做一次 REST 回读。
        /// </summary>
        public static string? PublishViaWizard(GeoServerClientFactory f, string dirRef, string nativeName)
        {
            try
            {
                EnsureWorkspace(f);
                var r = f.CreateImportWizardService().PublishShapefileAsync(new ImportSourceRequest
                {
                    Kind = ImportDataSourceKind.ShapefileDirectory,
                    Workspace = Ws,
                    LayerName = nativeName,
                    NativeName = nativeName,
                    StoreName = Store,
                    FileRef = dirRef,
                    Srs = null,                 // 真实数据常为自定义投影（无 EPSG 码）：交给 .prj
                }).GetAwaiter().GetResult();
                if (!r.Success) return "向导返回失败：" + (r.Message ?? "(无原因)");
                var ft = f.CreateFeatureTypeService().GetFeatureTypeAsync(Ws, Store, nativeName).GetAwaiter().GetResult();
                return ft == null ? "向导报成功但回读为空" : null;
            }
            catch (Exception ex) { return ex.GetType().Name + ": " + Trunc(ex.Message); }
        }

        public static void Wipe(GeoServerClientFactory f)
        {
            try { f.CreateWorkspaceService().DeleteWorkspaceAsync(Ws, true).GetAwaiter().GetResult(); } catch { }
        }

        /// <summary>压缩为单行诊断串（异常消息含换行会打乱 harness 输出）。</summary>
        private static string Trunc(string? s)
        {
            if (s == null) return "";
            if (s.Length > 260) s = s.Substring(0, 260);
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var c in s) sb.Append(c == '\n' || c == '\r' ? ' ' : c);
            return sb.ToString();
        }
    }
}
