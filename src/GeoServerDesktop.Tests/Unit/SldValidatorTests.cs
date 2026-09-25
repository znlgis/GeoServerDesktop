using GeoServerDesktop.GeoServerClient.Sld;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>SldValidator 离线单元测试：XML 结构校验 + 结构化文档校验（应用前快速反馈）。</summary>
public class SldValidatorTests
{
    // ---- Validate(xml) ----

    [Fact]
    public void Validate_EmptyXml_Error()
    {
        var r = SldValidator.Validate("  ");
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("空"));
    }

    [Fact]
    public void Validate_MalformedXml_Error()
    {
        var r = SldValidator.Validate("<broken");
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("XML 解析失败"));
    }

    [Fact]
    public void Validate_WrongRoot_Error()
    {
        var r = SldValidator.Validate("<Foo/>");
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("StyledLayerDescriptor"));
    }

    [Fact]
    public void Validate_MissingNamedLayer_Error()
    {
        var r = SldValidator.Validate("<StyledLayerDescriptor version=\"1.0.0\"></StyledLayerDescriptor>");
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("缺少 NamedLayer"));
    }

    [Fact]
    public void Validate_MissingUserStyle_Error()
    {
        var r = SldValidator.Validate(
            "<StyledLayerDescriptor version=\"1.0.0\"><NamedLayer><Name>a</Name></NamedLayer></StyledLayerDescriptor>");
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("缺少 UserStyle"));
    }

    [Fact]
    public void Validate_MissingFeatureTypeStyle_Error()
    {
        var r = SldValidator.Validate(
            "<StyledLayerDescriptor version=\"1.0.0\"><NamedLayer><Name>a</Name>"
            + "<UserStyle><Name>s</Name></UserStyle></NamedLayer></StyledLayerDescriptor>");
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("缺少 FeatureTypeStyle"));
    }

    [Fact]
    public void Validate_MissingRule_Error()
    {
        var r = SldValidator.Validate(
            "<StyledLayerDescriptor version=\"1.0.0\"><NamedLayer><Name>a</Name>"
            + "<UserStyle><FeatureTypeStyle></FeatureTypeStyle></UserStyle></NamedLayer></StyledLayerDescriptor>");
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("缺少 Rule"));
    }

    [Fact]
    public void Validate_RuleMissingSymbolizer_Error()
    {
        var r = SldValidator.Validate(
            "<StyledLayerDescriptor version=\"1.0.0\"><NamedLayer><Name>a</Name>"
            + "<UserStyle><FeatureTypeStyle><Rule><Name>r1</Name></Rule></FeatureTypeStyle></UserStyle>"
            + "</NamedLayer></StyledLayerDescriptor>");
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("规则 1 缺少符号化器"));
    }

    [Fact]
    public void Validate_MultipleRulesMissingSymbolizers_AllReported()
    {
        var r = SldValidator.Validate(
            "<StyledLayerDescriptor version=\"1.0.0\"><NamedLayer><Name>a</Name>"
            + "<UserStyle><FeatureTypeStyle><Rule/><Rule/></FeatureTypeStyle></UserStyle>"
            + "</NamedLayer></StyledLayerDescriptor>");
        Assert.False(r.IsValid);
        Assert.Equal(2, r.Errors.Count);
        Assert.Contains(r.Errors, e => e.Contains("规则 1"));
        Assert.Contains(r.Errors, e => e.Contains("规则 2"));
    }

    [Fact]
    public void Validate_BuilderOutput_IsValid()
    {
        var doc = new SldDocument { LayerName = "poi" };
        doc.Rules.Add(new SldRule
        {
            Filter = new SldFilter { Property = "pop", Operator = ">", Value = "100" },
            Symbolizer = new SldPolygonSymbolizer { FillColor = "#FF0000", StrokeColor = "#000000" },
        });

        var r = SldValidator.Validate(SldBuilder.Build(doc));

        Assert.True(r.IsValid);
        Assert.Empty(r.Errors);
    }

    [Fact]
    public void Validate_TextSymbolizer_CountsAsSymbolizer()
    {
        // 校验器接受服务端支持的 Text/Raster 符号化器（解析子集外但 SLD 合法）
        var r = SldValidator.Validate(
            "<StyledLayerDescriptor version=\"1.0.0\"><NamedLayer><Name>a</Name>"
            + "<UserStyle><FeatureTypeStyle><Rule><TextSymbolizer/></Rule></FeatureTypeStyle></UserStyle>"
            + "</NamedLayer></StyledLayerDescriptor>");
        Assert.True(r.IsValid);
    }

    // ---- ValidateDocument(model) ----

    [Fact]
    public void ValidateDocument_Null_Error()
    {
        var r = SldValidator.ValidateDocument(null!);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("文档为空"));
    }

    [Fact]
    public void ValidateDocument_NoLayerNameAndNoRules_TwoErrors()
    {
        var r = SldValidator.ValidateDocument(new SldDocument());
        Assert.False(r.IsValid);
        Assert.Equal(2, r.Errors.Count);
        Assert.Contains(r.Errors, e => e.Contains("LayerName"));
        Assert.Contains(r.Errors, e => e.Contains("至少需要一条规则"));
    }

    [Fact]
    public void ValidateDocument_NullRule_Error()
    {
        var doc = new SldDocument { LayerName = "poi" };
        doc.Rules.Add(null);

        var r = SldValidator.ValidateDocument(doc);

        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("规则 1 为空"));
    }

    [Fact]
    public void ValidateDocument_MissingSymbolizer_Error()
    {
        var doc = new SldDocument { LayerName = "poi" };
        doc.Rules.Add(new SldRule { Symbolizer = null });

        var r = SldValidator.ValidateDocument(doc);

        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("规则 1 缺少符号化器"));
    }

    [Fact]
    public void ValidateDocument_FilterErrors_AllReported()
    {
        var doc = new SldDocument { LayerName = "poi" };
        doc.Rules.Add(new SldRule
        {
            Symbolizer = new SldLineSymbolizer(),
            Filter = new SldFilter { Property = "", Operator = "BETWEEN", Value = "" },
        });

        var r = SldValidator.ValidateDocument(doc);

        Assert.False(r.IsValid);
        Assert.Equal(3, r.Errors.Count);
        Assert.Contains(r.Errors, e => e.Contains("缺少属性名"));
        Assert.Contains(r.Errors, e => e.Contains("缺少值"));
        Assert.Contains(r.Errors, e => e.Contains("BETWEEN"));
    }

    [Fact]
    public void ValidateDocument_EmptyOperator_Allowed()
    {
        // 空操作符等价默认 "="（与 SldBuilder 一致）
        var doc = new SldDocument { LayerName = "poi" };
        doc.Rules.Add(new SldRule
        {
            Symbolizer = new SldLineSymbolizer(),
            Filter = new SldFilter { Property = "p", Operator = null, Value = "v" },
        });

        Assert.True(SldValidator.ValidateDocument(doc).IsValid);
    }

    [Fact]
    public void ValidateDocument_ValidDocument_IsValid()
    {
        var doc = new SldDocument { LayerName = "poi", StyleName = "s" };
        doc.Rules.Add(new SldRule
        {
            Symbolizer = new SldPointSymbolizer { WellKnownName = "circle", Size = 6 },
        });

        var r = SldValidator.ValidateDocument(doc);

        Assert.True(r.IsValid);
        Assert.Empty(r.Errors);
    }
}
