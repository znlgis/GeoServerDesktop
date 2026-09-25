using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.RealData
{
    /// <summary>
    /// M3 样式路径检查函数（双形态：harness 收集 CheckResult）。期望值全部经裸 REST
    /// 独立重建（styles.json + layers.json + 逐图层详情），不经被测代码回抄。
    /// </summary>
    public static class StyleChecks
    {
        /// <summary>
        /// 样式使用关系全量交叉核对：裸 REST 重建期望与库层聚合结果比较：
        /// ① 样式集合等价（styles.json vs 库结果）；② 每个样式 used/unused 一致；③ 引用图层集合等价。
        /// 工作空间样式引用（defaultStyle.href 含 /workspaces/）不计入——与 StyleUsageService 语义一致。
        /// </summary>
        public static CheckResult[] UsageCrossCheck(StyleUsage[] libraryUsages)
        {
            var results = new List<CheckResult>();
            try
            {
                // 1) 裸 REST：样式清单 + 图层清单
                var stylesJson = OgcProbe.Get(TestEnv.RestBase + "/rest/styles.json").Text ?? "";
                var layersJson = OgcProbe.Get(TestEnv.RestBase + "/rest/layers.json").Text ?? "";
                var expectedStyles = new HashSet<string>(CleanupTracker.ExtractNames(stylesJson, "style"), StringComparer.Ordinal);
                var layerNames = CleanupTracker.ExtractNames(layersJson, "layer").ToList();

                // 2) 逐图层详情 → 期望引用集合（跳过工作空间样式引用；详情读取失败仅登记 Warn）
                var expected = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                int detailFails = 0;
                foreach (var full in layerNames)
                {
                    var detail = OgcProbe.Get(LayerDetailUrl(full));
                    if (!detail.Ok) { detailFails++; continue; }
                    var (name, href) = ExtractDefaultStyle(detail.Text);
                    if (string.IsNullOrEmpty(name)) continue;
                    if (href != null && href.IndexOf("/workspaces/", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    if (!expected.TryGetValue(name, out var list)) { list = new List<string>(); expected[name] = list; }
                    list.Add(full);
                }
                if (detailFails > 0)
                    results.Add(Check.Warn("Style/usage-scan", $"裸 REST 有 {detailFails} 个图层详情读取失败，期望集可能不完整"));

                // 3) 样式集合等价
                var libNames = new HashSet<string>(libraryUsages.Select(u => u.StyleName), StringComparer.Ordinal);
                results.Add(Check.Cond(expectedStyles.SetEquals(libNames), "Style/usage-styleset",
                    $"样式集合等价（{expectedStyles.Count} 个）",
                    $"裸 REST {expectedStyles.Count} vs 库 {libNames.Count}：缺[{string.Join(",", expectedStyles.Except(libNames).Take(5))}] 多[{string.Join(",", libNames.Except(expectedStyles).Take(5))}]"));

                // 4) used/unused + 引用图层集合
                var mismatches = new List<string>();
                foreach (var u in libraryUsages)
                {
                    expected.TryGetValue(u.StyleName, out var exp);
                    var expSet = new HashSet<string>(exp ?? new List<string>(), StringComparer.Ordinal);
                    var gotSet = new HashSet<string>(u.Layers ?? new List<string>(), StringComparer.Ordinal);
                    bool usedOk = u.IsUsed == (expSet.Count > 0);
                    bool layersOk = expSet.SetEquals(gotSet);
                    if (!usedOk || !layersOk)
                        mismatches.Add(u.StyleName + "（期望 used=" + (expSet.Count > 0) + " [" + string.Join(",", expSet)
                            + "]；实际 used=" + u.IsUsed + " [" + string.Join(",", gotSet) + "]）");
                }
                if (mismatches.Count == 0)
                    results.Add(Check.Pass("Style/usage-crosscheck",
                        $"全量一致：{libraryUsages.Length} 个样式（used/unused 双向 + 引用图层集合）"));
                else
                    results.Add(Check.Fail("Style/usage-crosscheck",
                        $"不一致 {mismatches.Count} 个：" + string.Join("；", mismatches.Take(3))));
            }
            catch (Exception ex)
            {
                results.Add(Check.Fail("Style/usage-crosscheck", ex.GetType().Name + ": " + ex.Message));
            }
            return results.ToArray();
        }

        /// <summary>裸 REST 图层详情 URL（qualified 名走工作空间路径；无前缀走全局路径）。</summary>
        private static string LayerDetailUrl(string fullName)
        {
            int idx = fullName.IndexOf(':');
            if (idx > 0)
                return TestEnv.RestBase + "/rest/workspaces/" + Uri.EscapeDataString(fullName.Substring(0, idx))
                    + "/layers/" + Uri.EscapeDataString(fullName.Substring(idx + 1)) + ".json";
            return TestEnv.RestBase + "/rest/layers/" + Uri.EscapeDataString(fullName) + ".json";
        }

        /// <summary>独立解析图层详情 JSON 的 defaultStyle name/href（缺失返回 (null, null)）。</summary>
        private static (string? name, string? href) ExtractDefaultStyle(string json)
        {
            if (string.IsNullOrEmpty(json)) return (null, null);
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("layer", out var l) &&
                    l.TryGetProperty("defaultStyle", out var ds))
                {
                    var name = ds.TryGetProperty("name", out var n) ? n.GetString() : null;
                    var href = ds.TryGetProperty("href", out var h) ? h.GetString() : null;
                    return (name, href);
                }
            }
            catch { }
            return (null, null);
        }
    }
}
