using System.Collections.Generic;

namespace GeoServerDesktop.GeoServerClient.Sld
{
    /// <summary>
    /// SLD 文档（编辑器子集：单图层、点/线/面符号化器、规则过滤表达式）。
    /// 与 SldBuilder/SldParser 成对：模型 ↔ SLD 1.0.0 XML 双向转换。
    /// </summary>
    public sealed class SldDocument
    {
        /// <summary>SLD 版本（生成时写入；默认 1.0.0，实测 GeoServer 3.0.1 可渲染）。</summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>NamedLayer 名称（GeoServer 约定：与样式名一致）。</summary>
        public string LayerName { get; set; }

        /// <summary>UserStyle 名称（可选，为空时生成省略）。</summary>
        public string StyleName { get; set; }

        /// <summary>规则列表（按序生成）。</summary>
        public List<SldRule> Rules { get; set; } = new List<SldRule>();
    }

    /// <summary>
    /// SLD 规则：可选过滤表达式 + 一个符号化器（编辑器子集）。
    /// </summary>
    public sealed class SldRule
    {
        /// <summary>规则名称（可选）。</summary>
        public string Name { get; set; }

        /// <summary>过滤条件（可选；为 null 表示无过滤，匹配全部要素）。</summary>
        public SldFilter Filter { get; set; }

        /// <summary>符号化器（点/线/面之一）。</summary>
        public SldSymbolizer Symbolizer { get; set; }
    }

    /// <summary>
    /// 结构化过滤条件：属性 + 操作符 + 值（生成 ogc:Filter 标准结构）。
    /// </summary>
    public sealed class SldFilter
    {
        /// <summary>属性名（ogc:PropertyName）。</summary>
        public string Property { get; set; }

        /// <summary>操作符：=、&lt;&gt;、&gt;、&lt;、&gt;=、&lt;=、LIKE（大小写不敏感）。</summary>
        public string Operator { get; set; }

        /// <summary>字面值（ogc:Literal）。</summary>
        public string Value { get; set; }
    }

    /// <summary>符号化器基类（编辑器子集：点/线/面）。</summary>
    public abstract class SldSymbolizer
    {
    }

    /// <summary>点符号化器：标记形状 + 尺寸 + 填充/描边。</summary>
    public sealed class SldPointSymbolizer : SldSymbolizer
    {
        /// <summary>标记形状（WellKnownName）：circle/square/triangle/star/cross 等。</summary>
        public string WellKnownName { get; set; }

        /// <summary>尺寸（像素）。</summary>
        public double? Size { get; set; }

        /// <summary>填充色（#RRGGBB）。</summary>
        public string FillColor { get; set; }

        /// <summary>填充透明度（0-1）。</summary>
        public double? FillOpacity { get; set; }

        /// <summary>描边色（#RRGGBB）。</summary>
        public string StrokeColor { get; set; }

        /// <summary>描边宽度（像素）。</summary>
        public double? StrokeWidth { get; set; }
    }

    /// <summary>线符号化器：描边色 + 宽度。</summary>
    public sealed class SldLineSymbolizer : SldSymbolizer
    {
        /// <summary>描边色（#RRGGBB）。</summary>
        public string StrokeColor { get; set; }

        /// <summary>描边宽度（像素）。</summary>
        public double? StrokeWidth { get; set; }
    }

    /// <summary>面符号化器：填充 + 描边。</summary>
    public sealed class SldPolygonSymbolizer : SldSymbolizer
    {
        /// <summary>填充色（#RRGGBB）。</summary>
        public string FillColor { get; set; }

        /// <summary>填充透明度（0-1）。</summary>
        public double? FillOpacity { get; set; }

        /// <summary>描边色（#RRGGBB）。</summary>
        public string StrokeColor { get; set; }

        /// <summary>描边宽度（像素）。</summary>
        public double? StrokeWidth { get; set; }
    }
}
