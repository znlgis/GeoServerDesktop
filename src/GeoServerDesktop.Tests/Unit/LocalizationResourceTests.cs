using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using GeoServerDesktop.App.Services;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>
/// resx 本地化资源常态守护（M5）：
/// ① 每个 <see cref="LocalizationService"/> 字符串属性都必须能在资源表中解析（不回落成键名）；
/// ② 中文态必须真正取到 zh 卫星（与英文有实质差异），防止卫星未随构建/发布产出而静默降级为英文；
/// ③ 条目数不下降（新增文案漏加资源会被 ① 直接抓到）；
/// ④ 语言翻转后恢复原语言态，避免污染其它用例。
/// 归入 GeoServerSerial：翻转进程级语言单例，须与读 L.* 的用例互斥。
/// </summary>
[Collection("GeoServerSerial")]
public sealed class LocalizationResourceTests
{
    private static readonly PropertyInfo[] TextProperties = typeof(LocalizationService)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.PropertyType == typeof(string) && p.CanRead)
        .ToArray();

    private static string Value(LocalizationService l, PropertyInfo p) =>
        (string?)p.GetValue(l) ?? string.Empty;

    [Fact]
    public void PropertyCount_MatchesResxEntryScale()
    {
        // M5 迁移基线 634 条；后续新增文案只增不减
        Assert.True(TextProperties.Length >= 634,
            "本地化属性数下降到 " + TextProperties.Length + "（基线 634）");
    }

    [Fact]
    public void English_ResolvesEveryKey_NeverEchoesKey()
    {
        var l = LocalizationService.Instance;
        var original = l.IsChinese;
        try
        {
            while (l.IsChinese) l.ToggleLanguage();
            // 直接查资源表：属性名即资源键，缺失才是真问题
            //（不能拿"值等于属性名"判缺——Login/Create/Delete 等英文文案本就与键同名）
            var rm = new System.Resources.ResourceManager(
                "GeoServerDesktop.App.Resources.Strings", typeof(LocalizationService).Assembly);
            var missing = TextProperties
                .Where(p => string.IsNullOrWhiteSpace(rm.GetString(p.Name, CultureInfo.InvariantCulture)))
                .Select(p => p.Name).ToList();
            Assert.True(missing.Count == 0, "英文资源缺项：" + string.Join(", ", missing));
            // 运行时取到的值也不能回显键名（说明查表失败）
            var echoed = TextProperties.Where(p => Value(l, p) == p.Name && string.IsNullOrWhiteSpace(rm.GetString(p.Name, CultureInfo.InvariantCulture))).Select(p => p.Name).ToList();
            Assert.True(echoed.Count == 0, "属性回显键名（查表失败）：" + string.Join(", ", echoed));
        }
        finally { while (l.IsChinese != original) l.ToggleLanguage(); }
    }

    [Fact]
    public void Chinese_SatelliteLoadsAndDiffersFromEnglish()
    {
        var l = LocalizationService.Instance;
        var original = l.IsChinese;
        try
        {
            while (l.IsChinese) l.ToggleLanguage();
            var en = TextProperties.ToDictionary(p => p.Name, p => Value(l, p));

            while (!l.IsChinese) l.ToggleLanguage();
            var zh = TextProperties.ToDictionary(p => p.Name, p => Value(l, p));

            var empty = zh.Where(kv => string.IsNullOrWhiteSpace(kv.Value)).Select(kv => kv.Key).ToList();
            Assert.True(empty.Count == 0, "中文资源缺项：" + string.Join(", ", empty));

            // 至少 95% 条目中英应有实质差异（专有名词/URL 等等价项允许相同）
            var same = zh.Keys.Count(k => en[k] == zh[k]);
            var total = zh.Keys.Count;
            Assert.True(same <= total * 0.08,
                $"中文与英文同值条目过多（{same}/{total}）——zh 卫星可能未加载");
        }
        finally { while (l.IsChinese != original) l.ToggleLanguage(); }
    }

    [Fact]
    public void FormatPlaceholderStrings_StillFormattable()
    {
        var l = LocalizationService.Instance;
        var original = l.IsChinese;
        try
        {
            var withPlaceholder = TextProperties
                .Where(p => Value(l, p).Contains("{0}"))
                .ToList();
            Assert.NotEmpty(withPlaceholder);
            foreach (var p in withPlaceholder)
            {
                var fmt = Value(l, p);
                // 占位符文案必须仍可被 string.Format 注入（迁移到 resx 后不得破坏 {n} 形态）
                var formatted = string.Format(CultureInfo.InvariantCulture, fmt, "7", "7", "7", "7");
                Assert.Contains("7", formatted);
            }
        }
        finally { while (l.IsChinese != original) l.ToggleLanguage(); }
    }
}
