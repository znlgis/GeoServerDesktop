using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace GeoServerDesktop.GeoServerClient.Sld
{
    /// <summary>
    /// SLD 校验结果：错误列表；IsValid = 无错误。
    /// </summary>
    public sealed class SldValidationResult
    {
        /// <summary>是否通过校验（无错误）。</summary>
        public bool IsValid
        {
            get { return Errors.Count == 0; }
        }

        /// <summary>错误列表（为空即通过）。</summary>
        public List<string> Errors { get; } = new List<string>();
    }

    /// <summary>
    /// SLD 校验器（应用前本地校验）：
    ///   1) Validate(xml)：源码模式——XML 良构 + SLD 结构完整性（StyledLayerDescriptor→NamedLayer→UserStyle→FeatureTypeStyle→Rule→符号化器）；
    ///   2) ValidateDocument(model)：结构化模式——模型必填项与过滤条件合法性。
    /// 本地校验是"快速反馈"；服务端上传（GeoServer）仍是最终校验（非法 SLD 实测 400）。
    /// </summary>
    public static class SldValidator
    {
        /// <summary>
        /// 校验 SLD XML 的结构完整性。
        /// </summary>
        /// <param name="xml">SLD XML 文本。</param>
        /// <returns>校验结果。</returns>
        public static SldValidationResult Validate(string xml)
        {
            var result = new SldValidationResult();
            if (string.IsNullOrWhiteSpace(xml))
            {
                result.Errors.Add("SLD 内容为空");
                return result;
            }

            XDocument doc;
            try
            {
                doc = XDocument.Parse(xml);
            }
            catch (XmlException ex)
            {
                result.Errors.Add("XML 解析失败：" + ex.Message);
                return result;
            }

            var root = doc.Root;
            if (root == null || root.Name.LocalName != "StyledLayerDescriptor")
            {
                result.Errors.Add("根元素不是 StyledLayerDescriptor");
                return result;
            }

            var namedLayers = Children(root, "NamedLayer");
            if (namedLayers.Count == 0)
            {
                result.Errors.Add("缺少 NamedLayer");
                return result;
            }

            var userStyles = Children(namedLayers[0], "UserStyle");
            if (userStyles.Count == 0)
            {
                result.Errors.Add("NamedLayer 缺少 UserStyle");
                return result;
            }

            var ftsList = Children(userStyles[0], "FeatureTypeStyle");
            if (ftsList.Count == 0)
            {
                result.Errors.Add("UserStyle 缺少 FeatureTypeStyle");
                return result;
            }

            var rules = Children(ftsList[0], "Rule");
            if (rules.Count == 0)
            {
                result.Errors.Add("FeatureTypeStyle 缺少 Rule");
                return result;
            }

            int index = 0;
            foreach (var rule in rules)
            {
                index++;
                bool hasSymbolizer = false;
                foreach (var el in rule.Elements())
                {
                    var name = el.Name.LocalName;
                    if (name == "PointSymbolizer" || name == "LineSymbolizer" || name == "PolygonSymbolizer"
                        || name == "TextSymbolizer" || name == "RasterSymbolizer")
                    {
                        hasSymbolizer = true;
                        break;
                    }
                }
                if (!hasSymbolizer)
                    result.Errors.Add("规则 " + index + " 缺少符号化器");
            }
            return result;
        }

        /// <summary>
        /// 校验结构化文档（编辑器保存前）：必填项 + 过滤条件合法性。
        /// </summary>
        /// <param name="document">结构化文档。</param>
        /// <returns>校验结果。</returns>
        public static SldValidationResult ValidateDocument(SldDocument document)
        {
            var result = new SldValidationResult();
            if (document == null)
            {
                result.Errors.Add("文档为空");
                return result;
            }
            if (string.IsNullOrWhiteSpace(document.LayerName))
                result.Errors.Add("样式名（LayerName）必填");

            if (document.Rules == null || document.Rules.Count == 0)
            {
                result.Errors.Add("至少需要一条规则");
                return result;
            }

            for (int i = 0; i < document.Rules.Count; i++)
            {
                var rule = document.Rules[i];
                int n = i + 1;
                if (rule == null)
                {
                    result.Errors.Add("规则 " + n + " 为空");
                    continue;
                }
                if (rule.Symbolizer == null)
                    result.Errors.Add("规则 " + n + " 缺少符号化器");

                if (rule.Filter != null)
                {
                    if (string.IsNullOrWhiteSpace(rule.Filter.Property))
                        result.Errors.Add("规则 " + n + " 过滤条件缺少属性名");
                    if (string.IsNullOrWhiteSpace(rule.Filter.Value))
                        result.Errors.Add("规则 " + n + " 过滤条件缺少值");
                    if (!IsSupportedOperator(rule.Filter.Operator))
                        result.Errors.Add("规则 " + n + " 过滤操作符不支持：" + rule.Filter.Operator);
                }
            }
            return result;
        }

        /// <summary>与 SldBuilder 支持的操作符集合一致（空操作符默认 "="）。</summary>
        private static bool IsSupportedOperator(string op)
        {
            if (string.IsNullOrEmpty(op)) return true;
            switch (op.Trim().ToUpperInvariant())
            {
                case "=":
                case "<>":
                case "!=":
                case ">":
                case "<":
                case ">=":
                case "<=":
                case "LIKE":
                    return true;
                default:
                    return false;
            }
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
    }
}
