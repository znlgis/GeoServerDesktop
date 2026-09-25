using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace GeoServerDesktop.GeoServerClient.Sld
{
    /// <summary>
    /// SLD 解析结果：成功/失败 + 文档 + 警告。
    /// 警告列出"超出结构化编辑子集、保存将丢失"的内容（如 TextSymbolizer、复杂过滤），供 UI 提示。
    /// </summary>
    public sealed class SldParseResult
    {
        /// <summary>是否解析成功（XML 良构 + 具备可编辑的 SLD 骨架）。</summary>
        public bool Success { get; set; }

        /// <summary>解析出的文档（Success 时非空）。</summary>
        public SldDocument Document { get; set; }

        /// <summary>失败原因（Success=false 时）。</summary>
        public string Error { get; set; }

        /// <summary>超子集内容说明（编辑器保存会丢失这些内容）。</summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// SLD 解析器：SLD 1.0.0 XML → 结构化模型（编辑器子集，尽力而为）。
    /// 按元素 LocalName 匹配（容忍命名空间前缀差异）；不支持的符号化器/过滤类型记警告并跳过。
    /// </summary>
    public static class SldParser
    {
        /// <summary>
        /// 解析 SLD XML 为结构化文档。
        /// </summary>
        /// <param name="xml">SLD XML 文本。</param>
        /// <returns>解析结果（永不抛异常，错误经 Result 传递）。</returns>
        public static SldParseResult Parse(string xml)
        {
            var result = new SldParseResult();
            if (string.IsNullOrWhiteSpace(xml))
            {
                result.Error = "SLD 内容为空";
                return result;
            }

            XDocument doc;
            try
            {
                doc = XDocument.Parse(xml);
            }
            catch (XmlException ex)
            {
                result.Error = "XML 解析失败：" + ex.Message;
                return result;
            }

            var root = doc.Root;
            if (root == null || root.Name.LocalName != "StyledLayerDescriptor")
            {
                result.Error = "根元素不是 StyledLayerDescriptor";
                return result;
            }

            var namedLayers = Children(root, "NamedLayer");
            if (namedLayers.Count == 0)
            {
                result.Error = "未找到 NamedLayer";
                return result;
            }
            if (namedLayers.Count > 1)
                result.Warnings.Add("存在多个 NamedLayer，仅解析第一个");

            var namedLayer = namedLayers[0];
            var layerNameEl = FirstChild(namedLayer, "Name");
            var userStyles = Children(namedLayer, "UserStyle");
            if (userStyles.Count == 0)
            {
                result.Error = "未找到 UserStyle（引用式 NamedStyle 不在结构化编辑子集内）";
                return result;
            }
            if (userStyles.Count > 1)
                result.Warnings.Add("存在多个 UserStyle，仅解析第一个");

            var userStyle = userStyles[0];
            var styleNameEl = FirstChild(userStyle, "Name");
            var ftsList = Children(userStyle, "FeatureTypeStyle");
            if (ftsList.Count == 0)
            {
                result.Error = "未找到 FeatureTypeStyle";
                return result;
            }
            if (ftsList.Count > 1)
                result.Warnings.Add("存在多个 FeatureTypeStyle，仅解析第一个");

            var document = new SldDocument
            {
                Version = (string)root.Attribute("version") ?? "1.0.0",
                LayerName = layerNameEl == null ? null : layerNameEl.Value.Trim(),
                StyleName = styleNameEl == null ? null : styleNameEl.Value.Trim(),
            };

            int index = 0;
            foreach (var ruleEl in Children(ftsList[0], "Rule"))
            {
                index++;
                ParseRule(ruleEl, index, document, result.Warnings);
            }
            if (document.Rules.Count == 0)
                result.Warnings.Add("未找到可结构化编辑的 Rule（点/线/面符号化器）");

            result.Success = true;
            result.Document = document;
            return result;
        }

        private static void ParseRule(XElement ruleEl, int index, SldDocument document, List<string> warnings)
        {
            var rule = new SldRule();
            var nameEl = FirstChild(ruleEl, "Name");
            if (nameEl != null) rule.Name = nameEl.Value.Trim();

            if (FirstChild(ruleEl, "ScaleDenominator") != null)
                warnings.Add("规则 " + index + "：ScaleDenominator 超出结构化编辑子集，已忽略");

            var filterEl = FirstChild(ruleEl, "Filter");
            if (filterEl != null)
                rule.Filter = ParseFilter(filterEl, index, warnings);

            var symbolizer = ParseSymbolizer(ruleEl, index, warnings);
            if (symbolizer == null)
            {
                warnings.Add("规则 " + index + "：无点/线/面符号化器，已跳过（保存将丢失该规则）");
                return;
            }
            rule.Symbolizer = symbolizer;
            document.Rules.Add(rule);
        }

        private static SldFilter ParseFilter(XElement filterEl, int index, List<string> warnings)
        {
            var opEl = FirstElementChild(filterEl);
            if (opEl == null)
            {
                warnings.Add("规则 " + index + "：过滤条件为空，已忽略");
                return null;
            }

            string op;
            switch (opEl.Name.LocalName)
            {
                case "PropertyIsEqualTo": op = "="; break;
                case "PropertyIsNotEqualTo": op = "<>"; break;
                case "PropertyIsGreaterThan": op = ">"; break;
                case "PropertyIsLessThan": op = "<"; break;
                case "PropertyIsGreaterThanOrEqualTo": op = ">="; break;
                case "PropertyIsLessThanOrEqualTo": op = "<="; break;
                case "PropertyIsLike": op = "LIKE"; break;
                default:
                    warnings.Add("规则 " + index + "：过滤类型 " + opEl.Name.LocalName
                        + " 超出结构化编辑子集，已忽略（保存将丢失该过滤）");
                    return null;
            }

            var propEl = FirstChild(opEl, "PropertyName");
            var litEl = FirstChild(opEl, "Literal");
            if (propEl == null || litEl == null)
            {
                warnings.Add("规则 " + index + "：过滤条件缺少 PropertyName/Literal，已忽略");
                return null;
            }
            return new SldFilter { Property = propEl.Value.Trim(), Operator = op, Value = litEl.Value };
        }

        private static SldSymbolizer ParseSymbolizer(XElement ruleEl, int index, List<string> warnings)
        {
            SldSymbolizer found = null;
            int symbolizerCount = 0;
            foreach (var el in ruleEl.Elements())
            {
                switch (el.Name.LocalName)
                {
                    case "PointSymbolizer":
                        symbolizerCount++;
                        if (found == null) found = ParsePoint(el);
                        break;
                    case "LineSymbolizer":
                        symbolizerCount++;
                        if (found == null) found = ParseLine(el);
                        break;
                    case "PolygonSymbolizer":
                        symbolizerCount++;
                        if (found == null) found = ParsePolygon(el);
                        break;
                    case "TextSymbolizer":
                    case "RasterSymbolizer":
                        symbolizerCount++;
                        warnings.Add("规则 " + index + "：" + el.Name.LocalName
                            + " 超出结构化编辑子集，已忽略（保存将丢失）");
                        break;
                }
            }
            if (symbolizerCount > 1 && found != null)
                warnings.Add("规则 " + index + "：存在多个符号化器，仅解析第一个");
            return found;
        }

        private static SldPointSymbolizer ParsePoint(XElement el)
        {
            var p = new SldPointSymbolizer();
            var graphic = FirstChild(el, "Graphic");
            var mark = graphic == null ? null : FirstChild(graphic, "Mark");
            var wkn = mark == null ? null : FirstChild(mark, "WellKnownName");
            if (wkn != null) p.WellKnownName = wkn.Value.Trim();
            var size = graphic == null ? null : FirstChild(graphic, "Size");
            p.Size = size == null ? null : ParseNum(size.Value);

            var fill = mark == null ? null : FirstChild(mark, "Fill");
            var stroke = mark == null ? null : FirstChild(mark, "Stroke");
            p.FillColor = CssValue(fill, "fill");
            p.FillOpacity = ParseNum(CssValue(fill, "fill-opacity"));
            p.StrokeColor = CssValue(stroke, "stroke");
            p.StrokeWidth = ParseNum(CssValue(stroke, "stroke-width"));
            return p;
        }

        private static SldLineSymbolizer ParseLine(XElement el)
        {
            var l = new SldLineSymbolizer();
            var stroke = FirstChild(el, "Stroke");
            l.StrokeColor = CssValue(stroke, "stroke");
            l.StrokeWidth = ParseNum(CssValue(stroke, "stroke-width"));
            return l;
        }

        private static SldPolygonSymbolizer ParsePolygon(XElement el)
        {
            var p = new SldPolygonSymbolizer();
            var fill = FirstChild(el, "Fill");
            var stroke = FirstChild(el, "Stroke");
            p.FillColor = CssValue(fill, "fill");
            p.FillOpacity = ParseNum(CssValue(fill, "fill-opacity"));
            p.StrokeColor = CssValue(stroke, "stroke");
            p.StrokeWidth = ParseNum(CssValue(stroke, "stroke-width"));
            return p;
        }

        private static string CssValue(XElement fillOrStroke, string cssName)
        {
            if (fillOrStroke == null) return null;
            foreach (var p in fillOrStroke.Elements())
            {
                if (p.Name.LocalName != "CssParameter") continue;
                var nameAttr = p.Attribute("name");
                if (nameAttr != null && nameAttr.Value == cssName) return p.Value.Trim();
            }
            return null;
        }

        private static double? ParseNum(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            double v;
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? (double?)v : null;
        }

        private static List<XElement> Children(XElement parent, string localName)
        {
            var list = new List<XElement>();
            foreach (var el in parent.Elements())
            {
                if (el.Name.LocalName == localName) list.Add(el);
            }
            return list;
        }

        private static XElement FirstChild(XElement parent, string localName)
        {
            foreach (var el in parent.Elements())
            {
                if (el.Name.LocalName == localName) return el;
            }
            return null;
        }

        private static XElement FirstElementChild(XElement parent)
        {
            foreach (var el in parent.Elements()) return el;
            return null;
        }
    }
}
