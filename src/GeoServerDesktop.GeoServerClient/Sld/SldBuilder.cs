using System;
using System.Globalization;
using System.Text;

namespace GeoServerDesktop.GeoServerClient.Sld
{
    /// <summary>
    /// SLD 生成器：结构化模型 → SLD 1.0.0 XML
    /// （经典 sld/ogc 命名空间；GeoServer 3.0.1 实测：点/线/面 + ogc:Filter 均被接受）。
    /// </summary>
    public static class SldBuilder
    {
        /// <summary>
        /// 生成 SLD XML。LayerName 必填；Rules 为空时生成空 FeatureTypeStyle（由服务端/校验器判定合法性）。
        /// </summary>
        /// <param name="document">结构化文档。</param>
        /// <returns>SLD XML 字符串（UTF-8 声明 + 经典命名空间）。</returns>
        public static string Build(SldDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrEmpty(document.LayerName))
                throw new ArgumentException("LayerName 必填（NamedLayer/Name）", nameof(document));

            var sb = new StringBuilder(1024);
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<StyledLayerDescriptor version=\"")
              .Append(Escape(string.IsNullOrEmpty(document.Version) ? "1.0.0" : document.Version))
              .Append("\" xmlns=\"http://www.opengis.net/sld\" xmlns:ogc=\"http://www.opengis.net/ogc\">");
            sb.Append("<NamedLayer><Name>").Append(Escape(document.LayerName)).Append("</Name>");
            sb.Append("<UserStyle>");
            if (!string.IsNullOrEmpty(document.StyleName))
                sb.Append("<Name>").Append(Escape(document.StyleName)).Append("</Name>");
            sb.Append("<FeatureTypeStyle>");
            if (document.Rules != null)
            {
                foreach (var rule in document.Rules)
                {
                    if (rule != null) AppendRule(sb, rule);
                }
            }
            sb.Append("</FeatureTypeStyle></UserStyle></NamedLayer></StyledLayerDescriptor>");
            return sb.ToString();
        }

        private static void AppendRule(StringBuilder sb, SldRule rule)
        {
            sb.Append("<Rule>");
            if (!string.IsNullOrEmpty(rule.Name))
                sb.Append("<Name>").Append(Escape(rule.Name)).Append("</Name>");
            if (rule.Filter != null) AppendFilter(sb, rule.Filter);
            AppendSymbolizer(sb, rule.Symbolizer);
            sb.Append("</Rule>");
        }

        private static void AppendFilter(StringBuilder sb, SldFilter filter)
        {
            string op = string.IsNullOrEmpty(filter.Operator) ? "=" : filter.Operator.Trim().ToUpperInvariant();
            string tag;
            switch (op)
            {
                case "=": tag = "PropertyIsEqualTo"; break;
                case "<>":
                case "!=": tag = "PropertyIsNotEqualTo"; break;
                case ">": tag = "PropertyIsGreaterThan"; break;
                case "<": tag = "PropertyIsLessThan"; break;
                case ">=": tag = "PropertyIsGreaterThanOrEqualTo"; break;
                case "<=": tag = "PropertyIsLessThanOrEqualTo"; break;
                case "LIKE": tag = "PropertyIsLike"; break;
                default: throw new ArgumentException("不支持的过滤操作符：" + filter.Operator);
            }

            sb.Append("<ogc:Filter><ogc:").Append(tag);
            if (tag == "PropertyIsLike")
                sb.Append(" wildCard=\"*\" singleChar=\"?\" escapeChar=\"!\"");
            sb.Append("><ogc:PropertyName>").Append(Escape(filter.Property)).Append("</ogc:PropertyName>");
            sb.Append("<ogc:Literal>").Append(Escape(filter.Value)).Append("</ogc:Literal>");
            sb.Append("</ogc:").Append(tag).Append("></ogc:Filter>");
        }

        private static void AppendSymbolizer(StringBuilder sb, SldSymbolizer symbolizer)
        {
            var point = symbolizer as SldPointSymbolizer;
            if (point != null) { AppendPoint(sb, point); return; }
            var line = symbolizer as SldLineSymbolizer;
            if (line != null) { AppendLine(sb, line); return; }
            var polygon = symbolizer as SldPolygonSymbolizer;
            if (polygon != null) { AppendPolygon(sb, polygon); return; }
            throw new ArgumentException("未知符号化器类型：" +
                (symbolizer == null ? "(null)" : symbolizer.GetType().Name));
        }

        private static void AppendPoint(StringBuilder sb, SldPointSymbolizer p)
        {
            sb.Append("<PointSymbolizer><Graphic><Mark><WellKnownName>")
              .Append(Escape(string.IsNullOrEmpty(p.WellKnownName) ? "circle" : p.WellKnownName))
              .Append("</WellKnownName>");
            AppendFill(sb, p.FillColor, p.FillOpacity);
            AppendStroke(sb, p.StrokeColor, p.StrokeWidth);
            sb.Append("</Mark>");
            if (p.Size.HasValue) sb.Append("<Size>").Append(Num(p.Size.Value)).Append("</Size>");
            sb.Append("</Graphic></PointSymbolizer>");
        }

        private static void AppendLine(StringBuilder sb, SldLineSymbolizer l)
        {
            sb.Append("<LineSymbolizer>");
            AppendStroke(sb, l.StrokeColor, l.StrokeWidth);
            sb.Append("</LineSymbolizer>");
        }

        private static void AppendPolygon(StringBuilder sb, SldPolygonSymbolizer p)
        {
            sb.Append("<PolygonSymbolizer>");
            AppendFill(sb, p.FillColor, p.FillOpacity);
            AppendStroke(sb, p.StrokeColor, p.StrokeWidth);
            sb.Append("</PolygonSymbolizer>");
        }

        private static void AppendFill(StringBuilder sb, string color, double? opacity)
        {
            if (string.IsNullOrEmpty(color) && !opacity.HasValue) return;
            sb.Append("<Fill>");
            if (!string.IsNullOrEmpty(color))
                sb.Append("<CssParameter name=\"fill\">").Append(Escape(color)).Append("</CssParameter>");
            if (opacity.HasValue)
                sb.Append("<CssParameter name=\"fill-opacity\">").Append(Num(opacity.Value)).Append("</CssParameter>");
            sb.Append("</Fill>");
        }

        private static void AppendStroke(StringBuilder sb, string color, double? width)
        {
            if (string.IsNullOrEmpty(color) && !width.HasValue) return;
            sb.Append("<Stroke>");
            if (!string.IsNullOrEmpty(color))
                sb.Append("<CssParameter name=\"stroke\">").Append(Escape(color)).Append("</CssParameter>");
            if (width.HasValue)
                sb.Append("<CssParameter name=\"stroke-width\">").Append(Num(width.Value)).Append("</CssParameter>");
            sb.Append("</Stroke>");
        }

        private static string Num(double value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}
