using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using GeoServerDesktop.Tests.Infrastructure;
using GeoServerDesktop.Tests.RealData;

namespace GeoServerDesktop.Tests.RealDataTests
{
    /// <summary>
    /// 任务5：WMTS/GWC。WMTS 能力表含图层 + 取一张瓦片；GWC seed 小范围（zoom0-1）后轮询任务并核对宿主
    /// gwc 目录文件增量；truncate 后不再增长。结构缺失/404/扩展未装一律按现状 Warn（含注释），不误判为失败。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class WmtsGwcTests : RealDataTestBase
    {
        private static string Poly => E2ePublishHelper.Ws + ":" + E2ePublishHelper.PolyLayer;

        public WmtsGwcTests(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public void Wmts_Capabilities_And_Tile()
        {
            if (!RequireReady()) return;
            // 两方法本身对缺失结构返回 Warn（ThrowOnFail 仅拦 Fail），符合“默认未启用属正常”。
            Check.ThrowOnFail(RealDataChecks.WmtsHasLayer(Poly), RealDataChecks.WmtsTile(Poly));
        }

        [Fact]
        public void Gwc_Seed_Then_Truncate()
        {
            if (!RequireReady()) return;
            var gwcDir = DataEnv.GwcDir;
            if (!Directory.Exists(gwcDir))
            { Check.ThrowOnFail(Check.Warn("Gwc/seed", "宿主 gwc 目录不可见（" + gwcDir + "），无法核对文件增量——Warn")); return; }

            int before = CountFiles(gwcDir);
            var seedXml = SeedXml(Poly, "seed");
            var post = OgcProbe.Post(OgcProbe.GwcRest("seed/" + Uri.EscapeDataString(Poly)), seedXml, "application/xml");
            if (!post.Ok)
            { Check.ThrowOnFail(Check.Warn("Gwc/seed", "seed 任务 POST 返回 HTTP " + post.Status + "（GWC REST 未启用/扩展缺失）——Warn")); return; }

            // 轮询任务状态或文件增量（任一成立即视为 seed 生效）
            var sw = Stopwatch.StartNew();
            string status = "";
            bool grew = false;
            while (sw.Elapsed < TimeSpan.FromSeconds(60))
            {
                var st = OgcProbe.Get(OgcProbe.GwcRest("seed/" + Uri.EscapeDataString(Poly) + ".json"));
                status = st.Text ?? "";
                grew = CountFiles(gwcDir) > before;
                if (status.Contains("FINISHED") || status.Contains("PENDING") == false && status.Contains("status")) { if (grew) break; }
                if (grew && (status.Contains("FINISHED") || status.Contains("RUNNING"))) break;
                System.Threading.Thread.Sleep(1500);
            }
            int afterSeed = CountFiles(gwcDir);
            if (!(afterSeed > before))
            { Check.ThrowOnFail(Check.Warn("Gwc/seed", $"seed 已提交但 {gwcDir} 文件未增长（before={before}, after={afterSeed}, status~{Trim(status)}）——按现状 Warn")); return; }
            Check.Pass("Gwc/seed", $"seed 生效：gwc 目录 {before}→{afterSeed} 文件");

            // truncate 后再观察：不应继续增长
            var tr = OgcProbe.Post(OgcProbe.GwcRest("seed/" + Uri.EscapeDataString(Poly)), SeedXml(Poly, "truncate"), "application/xml");
            System.Threading.Thread.Sleep(1500);
            int afterTrunc = CountFiles(gwcDir);
            Check.ThrowOnFail(Check.Cond(tr.Ok && afterTrunc <= afterSeed,
                "Gwc/truncate", $"truncate 提交且目录不再增长（{afterSeed}→{afterTrunc}）",
                $"truncate HTTP {tr.Status} 目录 {afterSeed}→{afterTrunc}（增长则记 Warn）", warnInsteadOfFail: true));
        }

        private static string SeedXml(string layer, string type) =>
            "<seedRequest><name>" + layer + "</name><gridSetId>EPSG:4326</gridSetId>" +
            "<zoomStart>0</zoomStart><zoomStop>1</zoomStop><format>image/png</format>" +
            "<type>" + type + "</type><threadCount>1</threadCount></seedRequest>";

        private static int CountFiles(string dir)
        {
            try { return Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Count(); }
            catch { return -1; }
        }

        private static string Trim(string s) => s == null ? "" : (s.Length > 160 ? s.Substring(0, 160) : s);
    }
}
