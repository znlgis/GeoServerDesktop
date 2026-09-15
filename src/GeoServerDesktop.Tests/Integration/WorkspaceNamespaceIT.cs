using System;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// 工作空间 + 命名空间全 CRUD 闭环（真实 GeoServer REST）。
    /// 命名族缩写：wns。资源：gdtest_ws_wns（改名后 gdtest_ws_wns2）、命名空间 gdtest_ns_wns。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class WorkspaceNamespaceIT : GeoServerTestBase
    {
        public WorkspaceNamespaceIT(GeoServerFixture fx) : base(fx) { }

        private const string Ws1 = TestEnv.Prefix + "_ws_wns";
        private const string Ws2 = TestEnv.Prefix + "_ws_wns2";
        private const string NsPrefix = TestEnv.Prefix + "_ns_wns";
        private const string NsUri1 = "http://gdtest.io/wns";
        private const string NsUri2 = "http://gdtest.io/wns-updated";

        [Fact]
        public async Task Workspace_Full_Crud_And_Rename()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateWorkspaceService();
            try
            {
                // 前置清理：上轮崩溃残留直接删掉，保证自身可独立重跑
                await GsKit.WipeWorkspaceAsync(Fx, Ws1);
                await GsKit.WipeWorkspaceAsync(Fx, Ws2);

                // --- CREATE ---
                await svc.CreateWorkspaceAsync(Ws1);
                Fx.Cleanup.TrackWorkspace(Ws1);

                // --- READ：列表含 + 单体 name ---
                var all = await svc.GetWorkspacesAsync();
                Assert.Contains(all, w => w.Name == Ws1);
                var one = await svc.GetWorkspaceAsync(Ws1);
                Assert.Equal(Ws1, one!.Name);

                // --- UPDATE：改名（PUT workspaces/{old} {"workspace":{"name":new}}，实测 200） ---
                await svc.UpdateWorkspaceAsync(Ws1, Ws2);
                Fx.Cleanup.TrackWorkspace(Ws2);
                var renamed = await svc.GetWorkspaceAsync(Ws2);
                Assert.Equal(Ws2, renamed!.Name);
                var goneEx = await Record.ExceptionAsync(() => svc.GetWorkspaceAsync(Ws1));
                GsKit.AssertRequest(goneEx, 404, "改名后旧名 GET");

                // --- DELETE ---
                await svc.DeleteWorkspaceAsync(Ws2, recurse: false);
                var ex = await Record.ExceptionAsync(() => svc.GetWorkspaceAsync(Ws2));
                var gre = GsKit.AssertRequest(ex, 404, "删除后 GET");
                // 实测 3.0.1：旧式 catalog 404 报文为纯文本 "No such workspace: 'xxx' found"
                Assert.Contains("No such", gre.ResponseContent ?? "");
                Assert.True(await GsKit.RawStatusAsync(Fx, "/rest/workspaces/" + Ws2 + ".json") == 404);
            }
            finally
            {
                await GsKit.WipeWorkspaceAsync(Fx, Ws1);
                await GsKit.WipeWorkspaceAsync(Fx, Ws2);
            }
        }

        [Fact]
        public async Task Namespace_Full_Crud()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateNamespaceService();
            try
            {
                await GsKit.WipeNamespaceAsync(Fx, NsPrefix);

                // --- CREATE ---
                await svc.CreateNamespaceAsync(NsPrefix, NsUri1);

                // --- READ ---
                // 实测 3.0.1：列表项只有 {"name","href"}（无 prefix/uri 键）→ 模型 Prefix/Uri 恒 null
                //（新发现）。列表存在性用 href 判定；字段校验以单体 GET 为准。
                var all = await svc.GetNamespacesAsync();
                Assert.Contains(all, n => (n.Href ?? "").EndsWith("/namespaces/" + NsPrefix + ".json"));
                var one = await svc.GetNamespaceAsync(NsPrefix);
                Assert.Equal(NsPrefix, one!.Prefix);
                Assert.Equal(NsUri1, one.Uri);

                // --- UPDATE：改 uri（无改名语义，PUT 携带 prefix+uri，实测 200） ---
                await svc.UpdateNamespaceAsync(NsPrefix, NsUri2);
                var updated = await svc.GetNamespaceAsync(NsPrefix);
                Assert.Equal(NsUri2, updated!.Uri);

                // --- DELETE ---
                await svc.DeleteNamespaceAsync(NsPrefix);
                var ex = await Record.ExceptionAsync(() => svc.GetNamespaceAsync(NsPrefix));
                GsKit.AssertRequest(ex, 404, "删除后 GET");
            }
            finally
            {
                await GsKit.WipeNamespaceAsync(Fx, NsPrefix);
            }
        }
    }
}
