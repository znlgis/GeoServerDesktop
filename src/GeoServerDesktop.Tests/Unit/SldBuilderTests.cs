using System;
using System.Globalization;
using System.Xml.Linq;
using GeoServerDesktop.GeoServerClient.Sld;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>SldBuilder 离线单元测试：点/线/面生成、过滤操作符映射、XML 转义、InvariantCulture 数字、参数校验。</summary>
public class SldBuilderTests
{
    [Fact]
    public void Build_NullDocument_Throws() =>
        Assert.Throws<ArgumentNullException>(() => SldBuilder.Build(null!));

    [Fact]
    public void Build_MissingLayerName_Throws() =>
        Assert.Throws<ArgumentException>(() => SldBuilder.Build(new SldDocument()));

    [Fact]
    public void Build_MinimalDocument_WellFormedWithDefaults()
    {
        var xml = SldBuilder.Build(new SldDocument { LayerName = "poi" });

        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", xml);
        Assert.Contains("version=\"1.0.0\"", xml);
        Assert.Contains("xmlns=\"http://www.opengis.net/sld\"", xml);
        Assert.Contains("xmlns:ogc=\"http://www.opengis.net/ogc\"", xml);
        Assert.Contains("<NamedLayer><Name>poi</Name>", xml);
        Assert.Contains("<FeatureTypeStyle></FeatureTypeStyle>", xml);

        var doc = XDocument.Parse(xml); // 良构验证
        Assert.Equal("StyledLayerDescriptor", doc.Root!.Name.LocalName);
    }

    [Fact]
    public void Build_StyleName_WrittenWhenProvided()
    {
        var xml = SldBuilder.Build(new SldDocument { LayerName = "poi", StyleName = "my_style" });
        Assert.Contains("<UserStyle><Name>my_style</Name>", xml);
    }

    [Fact]
    public void Build_StyleName_OmittedWhenEmpty()
    {
        var xml = SldBuilder.Build(new SldDocument { LayerName = "poi" });
        Assert.Contains("<UserStyle><FeatureTypeStyle>", xml);
    }

    [Fact]
    public void Build_PointSymbolizer_FullFields()
    {
        var xml = SldBuilder.Build(DocWith(new SldPointSymbolizer
        {
            WellKnownName = "square",
            Size = 8,
            FillColor = "#FF0000",
            FillOpacity = 0.5,
            StrokeColor = "#000000",
            StrokeWidth = 1,
        }, ruleName: "big"));

        Assert.Contains("<Rule><Name>big</Name>", xml);
        Assert.Contains("<PointSymbolizer><Graphic><Mark><WellKnownName>square</WellKnownName>", xml);
        Assert.Contains("<CssParameter name=\"fill\">#FF0000</CssParameter>", xml);
        Assert.Contains("<CssParameter name=\"fill-opacity\">0.5</CssParameter>", xml);
        Assert.Contains("<CssParameter name=\"stroke\">#000000</CssParameter>", xml);
        Assert.Contains("<CssParameter name=\"stroke-width\">1</CssParameter>", xml);
        Assert.Contains("<Size>8</Size>", xml);
    }

    [Fact]
    public void Build_PointSymbolizer_DefaultWellKnownNameIsCircle()
    {
        var xml = SldBuilder.Build(DocWith(new SldPointSymbolizer()));
        Assert.Contains("<WellKnownName>circle</WellKnownName>", xml);
    }

    [Fact]
    public void Build_LineSymbolizer_StrokeOnly()
    {
        var xml = SldBuilder.Build(DocWith(new SldLineSymbolizer { StrokeColor = "#00FF00", StrokeWidth = 2 }));

        Assert.Contains("<LineSymbolizer><Stroke>", xml);
        Assert.Contains("<CssParameter name=\"stroke\">#00FF00</CssParameter>", xml);
        Assert.Contains("<CssParameter name=\"stroke-width\">2</CssParameter>", xml);
        Assert.DoesNotContain("<Fill>", xml);
    }

    [Fact]
    public void Build_PolygonSymbolizer_FillAndStroke()
    {
        var xml = SldBuilder.Build(DocWith(new SldPolygonSymbolizer
        {
            FillColor = "#AABBCC",
            FillOpacity = 0.25,
            StrokeColor = "#112233",
            StrokeWidth = 0.5,
        }));

        Assert.Contains("<PolygonSymbolizer>", xml);
        Assert.Contains("<CssParameter name=\"fill\">#AABBCC</CssParameter>", xml);
        Assert.Contains("<CssParameter name=\"fill-opacity\">0.25</CssParameter>", xml);
        Assert.Contains("<CssParameter name=\"stroke\">#112233</CssParameter>", xml);
        Assert.Contains("<CssParameter name=\"stroke-width\">0.5</CssParameter>", xml);
    }

    [Fact]
    public void Build_PolygonSymbolizer_OmitsFillWhenEmpty()
    {
        var xml = SldBuilder.Build(DocWith(new SldPolygonSymbolizer { StrokeColor = "#000000" }));
        Assert.DoesNotContain("<Fill>", xml);
        Assert.Contains("<Stroke>", xml);
    }

    [Theory]
    [InlineData("=", "PropertyIsEqualTo")]
    [InlineData("<>", "PropertyIsNotEqualTo")]
    [InlineData("!=", "PropertyIsNotEqualTo")]
    [InlineData(">", "PropertyIsGreaterThan")]
    [InlineData("<", "PropertyIsLessThan")]
    [InlineData(">=", "PropertyIsGreaterThanOrEqualTo")]
    [InlineData("<=", "PropertyIsLessThanOrEqualTo")]
    [InlineData("LIKE", "PropertyIsLike")]
    [InlineData("like", "PropertyIsLike")]
    public void Build_FilterOperator_MapsToOgcTag(string op, string tag)
    {
        var xml = SldBuilder.Build(DocWith(
            new SldLineSymbolizer { StrokeColor = "#000000" },
            new SldFilter { Property = "pop", Operator = op, Value = "1000" }));

        Assert.Contains("<ogc:Filter><ogc:" + tag, xml);
        Assert.Contains("<ogc:PropertyName>pop</ogc:PropertyName>", xml);
        Assert.Contains("<ogc:Literal>1000</ogc:Literal>", xml);
    }

    [Fact]
    public void Build_FilterLike_IncludesWildcardAttributes()
    {
        var xml = SldBuilder.Build(DocWith(
            new SldLineSymbolizer { StrokeColor = "#000000" },
            new SldFilter { Property = "name", Operator = "LIKE", Value = "A*" }));

        Assert.Contains("<ogc:PropertyIsLike wildCard=\"*\" singleChar=\"?\" escapeChar=\"!\">", xml);
    }

    [Fact]
    public void Build_FilterOperator_DefaultEquals_WhenEmpty()
    {
        var xml = SldBuilder.Build(DocWith(
            new SldLineSymbolizer { StrokeColor = "#000000" },
            new SldFilter { Property = "pop", Operator = "", Value = "1" }));

        Assert.Contains("<ogc:PropertyIsEqualTo>", xml);
    }

    [Fact]
    public void Build_UnsupportedFilterOperator_Throws()
    {
        var doc = DocWith(
            new SldLineSymbolizer { StrokeColor = "#000000" },
            new SldFilter { Property = "pop", Operator = "BETWEEN", Value = "1" });

        var ex = Assert.Throws<ArgumentException>(() => SldBuilder.Build(doc));
        Assert.Contains("BETWEEN", ex.Message);
    }

    [Fact]
    public void Build_NullSymbolizer_Throws()
    {
        var doc = new SldDocument { LayerName = "poi" };
        doc.Rules.Add(new SldRule { Symbolizer = null });

        var ex = Assert.Throws<ArgumentException>(() => SldBuilder.Build(doc));
        Assert.Contains("未知符号化器", ex.Message);
    }

    [Fact]
    public void Build_NullRule_Skipped()
    {
        var doc = new SldDocument { LayerName = "poi" };
        doc.Rules.Add(null);
        doc.Rules.Add(new SldRule { Symbolizer = new SldPointSymbolizer() });

        var xml = SldBuilder.Build(doc);

        Assert.Contains("<PointSymbolizer>", xml);
        Assert.Equal(1, CountOf(xml, "<Rule>"));
    }

    [Fact]
    public void Build_EscapesXmlSpecialCharacters()
    {
        var xml = SldBuilder.Build(new SldDocument
        {
            LayerName = "a&b<c>d\"e",
            StyleName = "s&s",
            Rules =
            {
                new SldRule
                {
                    Symbolizer = new SldLineSymbolizer { StrokeColor = "#000000" },
                    Filter = new SldFilter { Property = "p", Operator = "=", Value = "x&y<z" },
                },
            },
        });

        Assert.Contains("<Name>a&amp;b&lt;c&gt;d&quot;e</Name>", xml);
        Assert.Contains("<Name>s&amp;s</Name>", xml);
        Assert.Contains("<ogc:Literal>x&amp;y&lt;z</ogc:Literal>", xml);
        XDocument.Parse(xml); // 转义正确则良构
    }

    [Fact]
    public void Build_UsesInvariantCultureNumbers_UnderCommaDecimalCulture()
    {
        var prev = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var xml = SldBuilder.Build(DocWith(new SldLineSymbolizer { StrokeColor = "#000000", StrokeWidth = 2.5 }));
            Assert.Contains("<CssParameter name=\"stroke-width\">2.5</CssParameter>", xml);
        }
        finally
        {
            CultureInfo.CurrentCulture = prev;
        }
    }

    private static SldDocument DocWith(SldSymbolizer symbolizer, SldFilter? filter = null, string? ruleName = null)
    {
        var doc = new SldDocument { LayerName = "poi" };
        doc.Rules.Add(new SldRule { Name = ruleName, Filter = filter, Symbolizer = symbolizer });
        return doc;
    }

    private static int CountOf(string text, string token)
    {
        int count = 0, idx = 0;
        while ((idx = text.IndexOf(token, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += token.Length;
        }
        return count;
    }
}
