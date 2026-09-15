using System;
using System.Collections.Generic;
using System.Linq;
using GeoServerDesktop.Tests.Infrastructure;
using GeoServerDesktop.Tests.RealData;

namespace GeoServerDesktop.Tests.RealDataTests
{
    /// <summary>
    /// 任务6：残留扫描。本层跑完后（顺序不保证，退而求其次）扫描 REST 树，列出除本层 fixture
    /// （gdtest_ws_e2e / gdtest_red_e2e）外的 gdtest_ 前缀孤儿资源。并行套件（Layer2/Headless）
    /// 可能也在建 gdtest_ 资源，故一律 best-effort Warn，不误判失败。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class ResidueCheckTests : RealDataTestBase
    {
        public ResidueCheckTests(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public void Scan_NoUnexpectedGdtestOrphans()
        {
            if (!RequireReady()) return;
            var expectedWs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { E2ePublishHelper.Ws };
            var expectedStyle = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { E2ePublishHelper.RedStyle };

            var ws = Names("/rest/workspaces.json", "workspace");
            var st = Names("/rest/styles.json", "style");
            var lg = Names("/rest/layergroups.json", "layerGroup");

            var extraWs = ws.Where(n => n.StartsWith("gdtest", StringComparison.OrdinalIgnoreCase) && !expectedWs.Contains(n)).ToList();
            var extraSt = st.Where(n => n.StartsWith("gdtest", StringComparison.OrdinalIgnoreCase) && !expectedStyle.Contains(n)).ToList();
            var extraLg = lg.Where(n => n.StartsWith("gdtest", StringComparison.OrdinalIgnoreCase)).ToList();

            var detail = $"ws额外=[{string.Join(",", extraWs)}] style额外=[{string.Join(",", extraSt)}] lg额外=[{string.Join(",", extraLg)}]";
            // best-effort：并行层可能留有 gdtest_ 资源，仅信息性 Warn。
            Check.ThrowOnFail(Check.Cond(extraWs.Count + extraSt.Count + extraLg.Count == 0,
                "Residue/gdtest", "仅见本层 fixture（gdtest_ws_e2e/gdtest_red_e2e）", "残留：" + detail, warnInsteadOfFail: true));
        }

        private static List<string> Names(string restPath, string key)
        {
            var r = OgcProbe.Get(TestEnv.RestBase + restPath);
            return (r.Text == null ? Enumerable.Empty<string>() : CleanupTracker.ExtractNames(r.Text, key)).ToList();
        }
    }
}
