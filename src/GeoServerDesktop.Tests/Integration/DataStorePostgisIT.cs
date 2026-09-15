using System;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// PostGIS 数据存储 + gdtest_poly 表要素类型 CRUD 闭环。
    /// 实测（3.0.1）接受的参数集：dbtype/host/port/database/schema/user/passwd/"Expose primary keys"；
    /// 注意 host 必须是 GeoServer 容器视角可达的地址（TestEnv.GeoServerVisiblePgHost，默认 host.docker.internal，
    /// 容器内 127.0.0.1 指容器自身）；GET 回读时 passwd 被加密为 crypt2:…，不可断言明文。命名族缩写：pg。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class DataStorePostgisIT : GeoServerTestBase
    {
        public DataStorePostgisIT(GeoServerFixture fx) : base(fx) { }

        private const string Ws = TestEnv.Prefix + "_ws_pg";
        private const string DsName = TestEnv.Prefix + "_ds_pg1";
        // 目录资源名全局唯一：发布名带 _pg_ 前缀，nativeName 指向真实表 gdtest_poly
        private const string FtName = TestEnv.Prefix + "_ds_pg_poly";
        private const string FtNative = TestEnv.Prefix + "_poly";

        private static DataStore NewPostgisStore() => new DataStore
        {
            Name = DsName,
            // 实测：请求体 "type":null 曾使 3.0.1 跳过类型推断（GET type:""）；FIXED：请求体
            // 现省略 null 字段（NullValueHandling.Ignore），显式 "PostGIS" 写法保留
            Type = "PostGIS",
            Enabled = true,
            Workspace = new WorkspaceReference { Name = Ws, Href = "" },
            Href = "",
            ConnectionParameters = new ConnectionParameters
            {
                Entries = new[]
                {
                    new ConnectionParameterEntry { Key = "dbtype", Value = "postgis" },
                    new ConnectionParameterEntry { Key = "host", Value = TestEnv.GeoServerVisiblePgHost },
                    new ConnectionParameterEntry { Key = "port", Value = TestEnv.PgPort.ToString() },
                    new ConnectionParameterEntry { Key = "database", Value = TestEnv.PgDb },
                    new ConnectionParameterEntry { Key = "schema", Value = "public" },
                    new ConnectionParameterEntry { Key = "user", Value = TestEnv.PgUser },
                    new ConnectionParameterEntry { Key = "passwd", Value = TestEnv.PgPass },
                    new ConnectionParameterEntry { Key = "Expose primary keys", Value = "true" },
                }
            }
        };

        [Fact]
        public async Task Postgis_Store_And_FeatureType_Full_Crud()
        {
            if (!RequireGeoServer()) return;
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
                await dsSvc.CreateDataStoreAsync(Ws, NewPostgisStore());

                // --- READ：GET 回读校验 host/database 参数值与类型 ---
                var ds = await dsSvc.GetDataStoreAsync(Ws, DsName);
                Assert.NotNull(ds);
                Assert.Equal("PostGIS", ds!.Type);
                Assert.True(ds.Enabled == true);
                Assert.Equal(TestEnv.GeoServerVisiblePgHost, GsKit.GetParam(ds.ConnectionParameters, "host"));
                Assert.Equal(TestEnv.PgDb, GsKit.GetParam(ds.ConnectionParameters, "database"));
                Assert.Equal("postgis", GsKit.GetParam(ds.ConnectionParameters, "dbtype"));
                // 实测：GET 回读时 passwd 被服务器加密（crypt2:…），绝不与明文比对
                Assert.DoesNotContain(TestEnv.PgPass, GsKit.GetParam(ds.ConnectionParameters, "passwd") ?? "");

                // --- UPDATE：description 模型字段缺失（同 shapefile 族注释），用 enabled 翻转验证 PUT ---
                var upd = NewPostgisStore();
                upd.Enabled = false;
                await dsSvc.UpdateDataStoreAsync(Ws, DsName, upd);
                var after = await dsSvc.GetDataStoreAsync(Ws, DsName);
                Assert.True(after!.Enabled == false, "PUT enabled=false 后应读到 false");
                upd.Enabled = true;
                await dsSvc.UpdateDataStoreAsync(Ws, DsName, upd);

                // --- FeatureType：建表 gdtest_poly 的 FT（实测 201 需表已存在；srs null 序列化缺陷族已 FIXED） ---
                var ft = new FeatureType { Name = FtName, NativeName = FtNative, Enabled = true, Title = FtName, Srs = "EPSG:4326" };
                await ftSvc.CreateFeatureTypeAsync(Ws, DsName, ft);
                var fts = await ftSvc.GetFeatureTypesAsync(Ws, DsName);
                Assert.Contains(fts, x => x.Name == FtName);
                var one = await ftSvc.GetFeatureTypeAsync(Ws, DsName, FtName);
                Assert.Equal(FtName, one!.Name);
                // 实测：PostGIS FT 的 srs 应为 EPSG:4326（表为 4326 MultiPolygon）
                Assert.Contains("4326", one.Srs ?? "");

                // --- UPDATE FT title（原实测：PUT 请求体 namespace/store null 引用 → XStream NPE 500，已 FIXED；
                // 必须用填实引用体，见 GsKit.FilledFt） ---
                var updFt = GsKit.FilledFt(Ws, DsName, FtName, FtNative, FtName + "-upd", "EPSG:4326");
                await ftSvc.UpdateFeatureTypeAsync(Ws, DsName, FtName, updFt);
                var updated = await ftSvc.GetFeatureTypeAsync(Ws, DsName, FtName);
                Assert.Equal(FtName + "-upd", updated!.Title);

                // --- DELETE：recurse 删存储 → 404 ---
                await dsSvc.DeleteDataStoreAsync(Ws, DsName, recurse: true);
                var ex = await Record.ExceptionAsync(() => dsSvc.GetDataStoreAsync(Ws, DsName));
                GsKit.AssertRequest(ex, 404, "recurse 删除后 GET 存储");
            }
            finally
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws);
            }
        }
    }
}
