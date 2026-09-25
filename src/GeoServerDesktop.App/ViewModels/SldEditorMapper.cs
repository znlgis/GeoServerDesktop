using System;
using System.Collections.Generic;
using System.Globalization;
using GeoServerDesktop.GeoServerClient.Sld;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// SLD 编辑器结构化模式与库层模型之间的映射（M3）：
    /// 编辑项 → SldDocument（保存用），SldDocument → 编辑项（加载用）。
    /// 数字字段按 InvariantCulture 解析/格式化；无效或空输入映射为 null（由生成器省略该 CssParameter）。
    /// </summary>
    internal static class SldEditorMapper
    {
        /// <summary>从编辑项集合构建结构化文档（LayerName 用目标样式名，保存时覆盖服务端规范化名）。</summary>
        public static SldDocument ToDocument(string styleName, IEnumerable<SldRuleEditor> editors)
        {
            var doc = new SldDocument { LayerName = styleName };
            foreach (var e in editors)
            {
                doc.Rules.Add(ToRule(e));
            }
            return doc;
        }

        /// <summary>从解析结果填充编辑项集合（追加到目标集合）。</summary>
        public static void ApplyDocument(SldDocument doc, IList<SldRuleEditor> target)
        {
            foreach (var rule in doc.Rules)
            {
                target.Add(FromRule(rule));
            }
        }

        /// <summary>编辑项 → 规则。</summary>
        public static SldRule ToRule(SldRuleEditor e)
        {
            var rule = new SldRule
            {
                Name = string.IsNullOrWhiteSpace(e.Name) ? null : e.Name,
                Symbolizer = ToSymbolizer(e),
            };

            if (e.FilterEnabled)
            {
                rule.Filter = new SldFilter
                {
                    Property = e.FilterProperty,
                    Operator = string.IsNullOrWhiteSpace(e.FilterOperator) ? "=" : e.FilterOperator,
                    Value = e.FilterValue,
                };
            }

            return rule;
        }

        /// <summary>规则 → 编辑项。</summary>
        public static SldRuleEditor FromRule(SldRule rule)
        {
            var e = new SldRuleEditor { Name = rule.Name ?? string.Empty };

            switch (rule.Symbolizer)
            {
                case SldPointSymbolizer p:
                    e.SymbolizerKind = "Point";
                    e.WellKnownName = p.WellKnownName ?? "circle";
                    e.Size = FormatNum(p.Size, "6");
                    e.FillColor = p.FillColor ?? string.Empty;
                    e.FillOpacity = FormatNum(p.FillOpacity, "1");
                    e.StrokeColor = p.StrokeColor ?? string.Empty;
                    e.StrokeWidth = FormatNum(p.StrokeWidth, "1");
                    break;
                case SldLineSymbolizer l:
                    e.SymbolizerKind = "Line";
                    e.StrokeColor = l.StrokeColor ?? string.Empty;
                    e.StrokeWidth = FormatNum(l.StrokeWidth, "1");
                    break;
                case SldPolygonSymbolizer poly:
                    e.SymbolizerKind = "Polygon";
                    e.FillColor = poly.FillColor ?? string.Empty;
                    e.FillOpacity = FormatNum(poly.FillOpacity, "1");
                    e.StrokeColor = poly.StrokeColor ?? string.Empty;
                    e.StrokeWidth = FormatNum(poly.StrokeWidth, "1");
                    break;
            }

            if (rule.Filter != null)
            {
                e.FilterEnabled = true;
                e.FilterProperty = rule.Filter.Property ?? string.Empty;
                e.FilterOperator = string.IsNullOrWhiteSpace(rule.Filter.Operator) ? "=" : rule.Filter.Operator;
                e.FilterValue = rule.Filter.Value ?? string.Empty;
            }

            return e;
        }

        private static SldSymbolizer ToSymbolizer(SldRuleEditor e)
        {
            switch (e.SymbolizerKind)
            {
                case "Point":
                    return new SldPointSymbolizer
                    {
                        WellKnownName = string.IsNullOrWhiteSpace(e.WellKnownName) ? "circle" : e.WellKnownName,
                        Size = ParseNum(e.Size),
                        FillColor = NullIfEmpty(e.FillColor),
                        FillOpacity = ParseNum(e.FillOpacity),
                        StrokeColor = NullIfEmpty(e.StrokeColor),
                        StrokeWidth = ParseNum(e.StrokeWidth),
                    };
                case "Line":
                    return new SldLineSymbolizer
                    {
                        StrokeColor = NullIfEmpty(e.StrokeColor),
                        StrokeWidth = ParseNum(e.StrokeWidth),
                    };
                default:
                    return new SldPolygonSymbolizer
                    {
                        FillColor = NullIfEmpty(e.FillColor),
                        FillOpacity = ParseNum(e.FillOpacity),
                        StrokeColor = NullIfEmpty(e.StrokeColor),
                        StrokeWidth = ParseNum(e.StrokeWidth),
                    };
            }
        }

        private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

        private static double? ParseNum(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : (double?)null;
        }

        private static string FormatNum(double? v, string fallback) =>
            v.HasValue ? v.Value.ToString(CultureInfo.InvariantCulture) : fallback;
    }
}
