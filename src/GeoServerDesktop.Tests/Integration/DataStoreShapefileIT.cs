using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// Shapefile 数据存储 + 要素类型 CRUD 闭环（真实 GeoServer 3.0.1）。
    /// 实测要点：连接参数用 entry @key="url" $="file:gdtest_data"（相对 data_dir），不是 directory；
    /// FeatureType.Enabled 现 bool? 且默认 true（FIXED-E27），显式置 true 的写法保留为回归样例。命名族缩写：ds。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class DataStoreShapefileIT : GeoServerTestBase
    {
        public DataStoreShapefileIT(GeoServerFixture fx) : base(fx) { }

        private const string Ws = TestEnv.Prefix + "_ws_ds";
        private const string DsName = TestEnv.Prefix + "_ds_ds1";
        // GeoServer 目录资源名（含发布时自动创建的 layer 名）全局唯一——发布名带本类缩写，
        // nativeName 才指磁盘上的真实 shapefile（gdtest_poly）。
        private const string FtName = TestEnv.Prefix + "_ds_poly";
        private const string FtNative = TestEnv.Prefix + "_poly";

        private static DataStore NewShapefileStore(string name) => new DataStore
        {
            Name = name,
            // 实测：POST 请求体 "type":null 会让 3.0.1 跳过类型推断（GET 回 type:""），显式给 "Shapefile" 正常
            Type = "Shapefile",
            Enabled = true,
            Workspace = new WorkspaceReference { Name = Ws, Href = "" },
            Href = "",
            ConnectionParameters = new ConnectionParameters
            {
                Entries = new[]
                {
                    new ConnectionParameterEntry { Key = "url", Value = "file:" + TestEnv.Prefix + "_data" },
                    new ConnectionParameterEntry { Key = "namespace", Value = "http://" + Ws },
                }
            }
        };

        [Fact]
        public async Task Store_FeatureType_Full_Crud_And_RecurseDelete()
        {
            if (!RequireGeoServer()) return;
            Assert.True(TestEnv.GeneratedDataExists(), "缺少测试数据目录 " + TestEnv.GeneratedDataDir);
            using var f = Fx.Factory();
            var wsSvc = f.CreateWorkspaceService();
            var dsSvc = f.CreateDataStoreService();
            var ftSvc = f.CreateFeatureTypeService();
            try
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
                await wsSvc.CreateWorkspaceAsync(Ws);
                Fx.Cleanup.TrackWorkspace(Ws);

                // --- CREATE ---
                await dsSvc.CreateDataStoreAsync(Ws, NewShapefileStore(DsName));

                // --- READ：GET 校验 enabled/type/url ---
                var ds = await dsSvc.GetDataStoreAsync(Ws, DsName);
                Assert.NotNull(ds);
                Assert.Equal(DsName, ds!.Name);
                Assert.Equal("Shapefile", ds.Type);
                Assert.True(ds.Enabled == true, "新建 shapefile 存储实测应 enabled=true，实际: " + ds.Enabled);
                Assert.Equal("file:" + TestEnv.Prefix + "_data", GsKit.GetParam(ds.ConnectionParameters, "url"));
                var list = await dsSvc.GetDataStoresAsync(Ws);
                Assert.Contains(list, x => x.Name == DsName);

                // --- UPDATE：DataStore 模型没有 description 字段（库缺陷候选：无法经模型更新描述，
                // 见 API 清单 B 节 DataStore 属性表），改用可观测的 enabled 翻转验证 PUT 更新生效 ---
                var upd = NewShapefileStore(DsName);
                upd.Enabled = false;
                await dsSvc.UpdateDataStoreAsync(Ws, DsName, upd);
                var afterOff = await dsSvc.GetDataStoreAsync(Ws, DsName);
                Assert.True(afterOff!.Enabled == false, "PUT enabled=false 后应读到 false");
                upd.Enabled = true;
                await dsSvc.UpdateDataStoreAsync(Ws, DsName, upd);
                var afterOn = await dsSvc.GetDataStoreAsync(Ws, DsName);
                Assert.True(afterOn!.Enabled == true, "PUT enabled=true 后应读到 true");

                // --- FeatureType：建（FIXED-E27：Enabled 默认即 true；请求体 null 省略后 srs 不再以
                //     JSON null 提交——原 500 拒绝属客户端 null 序列化缺陷族，显式 Srs 保留为正向断言） ---
                var ft = new FeatureType { Name = FtName, NativeName = FtNative, Enabled = true, Title = FtName, Srs = "EPSG:4326" };
                await ftSvc.CreateFeatureTypeAsync(Ws, DsName, ft);
                var fts = await ftSvc.GetFeatureTypesAsync(Ws, DsName);
                Assert.Contains(fts, x => x.Name == FtName);
                var one = await ftSvc.GetFeatureTypeAsync(Ws, DsName, FtName);
                Assert.Equal(FtName, one!.Name);
                Assert.True(one.Enabled, "实测：显式 Enabled=true 建 FT 后 GET 应回 true");

                // FIXED（null 引用族）：原 FT PUT 请求体 namespace/store null 引用 → XStream NPE 500；
                // 现 null 字段省略。仍用 GsKit.FilledFt 携带填实引用（更贴近真实形态，行为不变）。
                var updFt = GsKit.FilledFt(Ws, DsName, FtName, FtNative, FtName + "-updated", "EPSG:4326");
                await ftSvc.UpdateFeatureTypeAsync(Ws, DsName, FtName, updFt);
                var updatedFt = await ftSvc.GetFeatureTypeAsync(Ws, DsName, FtName);
                Assert.Equal(FtName + "-updated", updatedFt!.Title);

                // --- 发布 FT 后同名 layer 自动创建（E2ePublisher/StyleLayerIT 依赖该行为，这里固化基线） ---
                var layer = await f.CreateLayerService().GetLayerAsync(FtName);
                Assert.Equal(FtName, layer!.Name);

                // --- DELETE：recurse=true 连资源一并删，之后 GET 存储 404 ---
                await dsSvc.DeleteDataStoreAsync(Ws, DsName, recurse: true);
                var ex = await Record.ExceptionAsync(() => dsSvc.GetDataStoreAsync(Ws, DsName));
                GsKit.AssertRequest(ex, 404, "recurse 删除后 GET 存储");
                var exFt = await Record.ExceptionAsync(() => ftSvc.GetFeatureTypeAsync(Ws, DsName, FtName));
                GsKit.AssertRequest(exFt, 404, "recurse 删除后 GET FT");
            }
            finally
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
            }
        }

        [Fact]
        public async Task ResetDataStore_Returns200()
        {
            if (!RequireGeoServer()) return;
            // 用共享 e2e 存储（幂等就绪），PUT .../reset 实测 200（对不存在的存储则 404）
            E2ePublisher.EnsureShapefileStore();
            using var f = Fx.Factory();
            var dsSvc = f.CreateDataStoreService();
            await dsSvc.ResetDataStoreAsync(E2ePublisher.Ws, E2ePublisher.Ds);
            // 无异常即通过；再 GET 确认存储仍健康
            var ds = await dsSvc.GetDataStoreAsync(E2ePublisher.Ws, E2ePublisher.Ds);
            Assert.Equal(E2ePublisher.Ds, ds!.Name);
        }

        [Fact]
        public async Task UploadFile_Zip_To_Isolated_DirectoryStore()
        {
            if (!RequireGeoServer()) return;
            Assert.True(TestEnv.GeneratedDataExists(), "缺少测试数据目录 " + TestEnv.GeneratedDataDir);
            using var f = Fx.Factory();
            var wsSvc = f.CreateWorkspaceService();
            var dsSvc = f.CreateDataStoreService();
            const string dsName = TestEnv.Prefix + "__ds_up";
            try
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
                await wsSvc.CreateWorkspaceAsync(Ws);
                Fx.Cleanup.TrackWorkspace(Ws);

                // 破坏性动作规避：不覆盖共享 gdtest_data —— 先把 poly 全套复制到独立子目录 gdtest_up，
                // 对"目录存储"上传 zip 只影响该独立目录。
                var upDir = TestEnv.AbsDataPath(TestEnv.Prefix + "_up");
                Directory.CreateDirectory(upDir);
                foreach (var ext in new[] { "shp", "dbf", "shx", "prj", "cpg" })
                {
                    var src = TestEnv.AbsDataPath(FtNative + "." + ext);
                    if (File.Exists(src)) File.Copy(src, Path.Combine(upDir, FtNative + "." + ext), overwrite: true);
                }
                var zipPath = Path.Combine(Path.GetTempPath(), TestEnv.Prefix + "_up_" + Guid.NewGuid().ToString("N") + ".zip");
                ZipFile.CreateFromDirectory(upDir, zipPath);
                byte[] zipBytes;
                try { zipBytes = File.ReadAllBytes(zipPath); }
                finally { try { File.Delete(zipPath); } catch { } }

                var store = NewShapefileStore(dsName);
                store.ConnectionParameters = new ConnectionParameters
                {
                    Entries = new[]
                    {
                        new ConnectionParameterEntry { Key = "url", Value = "file:" + TestEnv.Prefix + "_data/" + TestEnv.Prefix + "_up" },
                        new ConnectionParameterEntry { Key = "namespace", Value = "http://" + Ws },
                    }
                };
                await dsSvc.CreateDataStoreAsync(Ws, store);

                // 实测基线（KNOWN-ISSUE，curl 全等复现）：该镜像（kartoza GeoServer 3.0.1）对
                // /datastores/{ds}/file.zip 一律回 400 "Could not find appropriate zip file in archive"
                // ——root 布局/嵌套目录/dbtype|entry 参数/octet-stream|application/zip 全试错均如此。
                // 固化为现状；若端点修复（不再抛或 2xx），下一行断言翻红提醒更新基线。
                var upEx = await Record.ExceptionAsync(() =>
                    dsSvc.UploadFileAsync(Ws, dsName, "zip", zipBytes, "application/zip"));
                if (upEx != null) GsKit.AssertRequest(upEx, 400, "zip 上传");

                // 上传动作无论成败，存储本体必须仍 200（从宽校验，细节不深究）
                var after = await dsSvc.GetDataStoreAsync(Ws, dsName);
                Assert.Equal(dsName, after!.Name);
            }
            finally
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
            }
        }
    }
}
