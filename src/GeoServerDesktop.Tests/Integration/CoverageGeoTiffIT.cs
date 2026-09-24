using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// GeoTIFF coveragestore + coverage CRUD 闭环，及 UploadCoverageFileAsync 上传流。
    /// 实测要点（3.0.1）：coveragestore 默认 enabled=false 需显式 true；
    /// 文件上传端点扩展名必须是 geotiff（tif/tiff 均回 400 "Unsupported format"）；
    /// 空存储（不带 url）上传后自动创建的 coverage 名 = 存储名（非文件名）。
    /// coverage GET 响应里 nativeCRS/crs 是对象（{"@class":...,"$":"..."}）——CrsStringConverter 宽容为字符串
    /// （原 KNOWN-ISSUE“单体 GET 抛 Newtonsoft 异常”已修复，本节改为严格断言防回归）。
    /// 命名族缩写：cov。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class CoverageGeoTiffIT : GeoServerTestBase
    {
        public CoverageGeoTiffIT(GeoServerFixture fx) : base(fx) { }

        private const string Ws = TestEnv.Prefix + "_ws_cov";
        private const string CsName = TestEnv.Prefix + "_cs_dem";
        // 资源名全局唯一：coverage 发布名带 _cov_ 缩写，nativeName 才是磁盘文件基名
        private const string CovName = TestEnv.Prefix + "_cov_dem";
        private const string CovNative = TestEnv.Prefix + "_dem";

        private static CoverageStore NewStore(string name, string? url) => new CoverageStore
        {
            Name = name,
            Type = "GeoTIFF",
            Enabled = true,
            Url = url,
            Workspace = new WorkspaceReference { Name = Ws },
        };

        [Fact]
        public async Task CoverageStore_And_Coverage_Full_Crud()
        {
            if (!RequireGeoServer()) return;
            Assert.True(TestEnv.GeneratedDataExists(), "缺少测试数据目录 " + TestEnv.GeneratedDataDir);
            using var f = Fx.Factory();
            var wsSvc = f.CreateWorkspaceService();
            var csSvc = f.CreateCoverageStoreService();
            var covSvc = f.CreateCoverageService();
            try
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
                await wsSvc.CreateWorkspaceAsync(Ws);
                Fx.Cleanup.TrackWorkspace(Ws);

                // --- CREATE store（url=file:gdtest_data/gdtest_dem.tif） ---
                await csSvc.CreateCoverageStoreAsync(Ws, NewStore(CsName, "file:" + TestEnv.Prefix + "_data/" + CovNative + ".tif"));

                // --- READ store：GET 重读 name/type；实测默认 enabled=false，PUT 显式置 true ---
                var cs = await csSvc.GetCoverageStoreAsync(Ws, CsName);
                Assert.NotNull(cs);
                Assert.Equal(CsName, cs!.Name);
                Assert.Equal("GeoTIFF", cs.Type);
                if (cs.Enabled != true)
                {
                    var upd = NewStore(CsName, cs.Url);
                    upd.Enabled = true;
                    await csSvc.UpdateCoverageStoreAsync(Ws, CsName, upd);
                    var cs2 = await csSvc.GetCoverageStoreAsync(Ws, CsName);
                    Assert.True(cs2!.Enabled == true, "PUT enabled=true 后应读到 true");
                }

                // --- CREATE coverage（nativeName=gdtest_dem；原实测请求体 srs=null → 500，null 省略后修复） ---
                var cov = new Coverage { Name = CovName, NativeName = CovNative, Enabled = true, Title = CovName, Srs = "EPSG:32754" };
                await covSvc.CreateCoverageAsync(Ws, CsName, cov);

                // --- READ：列表含它 ---
                var list = await covSvc.GetCoveragesAsync(Ws, CsName);
                Assert.Contains(list, c => c.Name == CovName);

                // --- 单体 GET：完整读取（回归防护：nativeCRS/crs 对象形态经 CrsStringConverter 宽容为字符串） ---
                var single = await covSvc.GetCoverageAsync(Ws, CsName, CovName);
                Assert.Equal(CovName, single!.Name);
                Assert.NotNull(single.NativeBoundingBox);
                Assert.Equal("EPSG:32754", single.NativeBoundingBox!.Crs);
                Assert.Contains("PROJCS", single.NativeCRS);

                // --- UPDATE：PUT coverage title（原实测 namespace/store null 引用 → 500 NPE，已 FIXED；填实引用写法保留） ---
                var updCov = GsKit.FilledCov(Ws, CsName, CovName, CovNative, CovName + "-upd", "EPSG:32754");
                await covSvc.UpdateCoverageAsync(Ws, CsName, CovName, updCov);
                var list2 = await covSvc.GetCoveragesAsync(Ws, CsName);
                Assert.Contains(list2, c => c.Name == CovName);

                // --- DELETE coverage → 404 ---
                await covSvc.DeleteCoverageAsync(Ws, CsName, CovName, recurse: true);
                var exDel = await Record.ExceptionAsync(() => covSvc.GetCoverageAsync(Ws, CsName, CovName));
                GsKit.AssertRequest(exDel, 404, "删除 coverage 后 GET");

                // --- DELETE store（recurse）→ 404 ---
                await csSvc.DeleteCoverageStoreAsync(Ws, CsName, recurse: true);
                var exCs = await Record.ExceptionAsync(() => csSvc.GetCoverageStoreAsync(Ws, CsName));
                GsKit.AssertRequest(exCs, 404, "删除 store 后 GET");
            }
            finally
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
            }
        }

        [Fact]
        public async Task UploadCoverageFile_GeoTiff_AutoPublishesCoverage()
        {
            if (!RequireGeoServer()) return;
            var tif = TestEnv.AbsDataPath(TestEnv.Prefix + "_dem.tif");
            Assert.True(File.Exists(tif), "缺少 " + tif);
            using var f = Fx.Factory();
            var wsSvc = f.CreateWorkspaceService();
            var csSvc = f.CreateCoverageStoreService();
            var covSvc = f.CreateCoverageService();
            const string upStore = TestEnv.Prefix + "_cs_upload";
            try
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
                await wsSvc.CreateWorkspaceAsync(Ws);
                Fx.Cleanup.TrackWorkspace(Ws);

                // 新建空 store（不给 url，实测 201，但 enabled=false——上传后 PUT 置 true 复核）
                await csSvc.CreateCoverageStoreAsync(Ws, NewStore(upStore, url: null));

                // PUT file.geotiff：实测扩展名必须 geotiff（tif/tiff 回 400）
                await csSvc.UploadCoverageFileAsync(Ws, upStore, File.ReadAllBytes(tif), "geotiff");

                // store 仍 200；实测自动生成 coverage（名字=存储名）
                var cs = await csSvc.GetCoverageStoreAsync(Ws, upStore);
                Assert.Equal(upStore, cs!.Name);
                var covs = await covSvc.GetCoveragesAsync(Ws, upStore);
                Assert.NotEmpty(covs);
                // 从宽校验：不锁死自动命名细节，仅确认存在已发布 coverage 且 GET 列表可经单体路径复核
                var auto = covs[0].Name;
                var status = await GsKit.RawStatusAsync(Fx,
                    $"/rest/workspaces/{Ws}/coveragestores/{upStore}/coverages/{auto}.json");
                Assert.Equal(200, status);
            }
            finally
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
            }
        }
    }
}
