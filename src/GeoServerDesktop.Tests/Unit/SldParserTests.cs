using GeoServerDesktop.GeoServerClient.Sld;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>SldParser 离线单元测试：Build→Parse 往返、失败路径（永不抛）、超子集警告。</summary>
public class SldParserTests
{
    // ---- 失败路径 ----

    [Fact]
    public void Parse_NullOrEmpty_ReturnsError()
    {
        var r1 = SldParser.Parse(null!);
        Assert.False(r1.Success);
        Assert.Contains("空", r1.Error);

        var r2 = SldParser.Parse("   ");
        Assert.False(r2.Success);
        Assert.Contains("空", r2.Error);
    }

    [Fact]
    public void Parse_MalformedXml_ReturnsError()
    {
        var r = SldParser.Parse("<not-closed");
        Assert.False(r.Success);
        Assert.Contains("XML 解析失败", r.Error);
    }

    [Fact]
    public void Parse_WrongRoot_ReturnsError()
    {
        var r = SldParser.Parse("<Foo/>");
        Assert.False(r.Success);
        Assert.Contains("StyledLayerDescriptor", r.Error);
    }

    [Fact]
    public void Parse_NoNamedLayer_ReturnsError()
    {
        var r = SldParser.Parse("<StyledLayerDescriptor version=\"1.0.0\"></StyledLayerDescriptor>");
        Assert.False(r.Success);
        Assert.Contains("NamedLayer", r.Error);
    }

    [Fact]
    public void Parse_NoUserStyle_ReturnsError()
    {
        // 引用式 NamedStyle 不在结构化编辑子集内
        var r = SldParser.Parse(
            "<StyledLayerDescriptor version=\"1.0.0\"><NamedLayer><Name>poi</Name>"
            + "<NamedStyle><Name>ref</Name></NamedStyle></NamedLayer></StyledLayerDescriptor>");
        Assert.False(r.Success);
        Assert.Contains("UserStyle", r.Error);
    }

    [Fact]
    public void Parse_NoFeatureTypeStyle_ReturnsError()
    {
        var r = SldParser.Parse(
            "<StyledLayerDescriptor version=\"1.0.0\"><NamedLayer><Name>poi</Name>"
            + "<UserStyle><Name>s</Name></UserStyle></NamedLayer></StyledLayerDescriptor>");
        Assert.False(r.Success);
        Assert.Contains("FeatureTypeStyle", r.Error);
    }

    // ---- 往返 ----

    [Fact]
    public void Parse_RoundTrip_PointLinePolygonWithFilters()
    {
        var original = new SldDocument { LayerName = "poi", StyleName = "poi_style" };
        original.Rules.Add(new SldRule
        {
            Name = "r1",
            Filter = new SldFilter { Property = "pop", Operator = ">", Value = "1000" },
            Symbolizer = new SldPointSymbolizer
            {
                WellKnownName = "square",
                Size = 8,
                FillColor = "#FF0000",
                FillOpacity = 0.5,
                StrokeColor = "#000000",
                StrokeWidth = 1,
            },
        });
        original.Rules.Add(new SldRule
        {
            Symbolizer = new SldLineSymbolizer { StrokeColor = "#00FF00", StrokeWidth = 2 },
        });
        original.Rules.Add(new SldRule
        {
            Filter = new SldFilter { Property = "name", Operator = "LIKE", Value = "A*" },
            Symbolizer = new SldPolygonSymbolizer
            {
                FillColor = "#0000FF",
                FillOpacity = 0.8,
                StrokeColor = "#FFFFFF",
                StrokeWidth = 0.5,
            },
        });

        var result = SldParser.Parse(SldBuilder.Build(original));

        Assert.True(result.Success);
        Assert.Empty(result.Warnings);

        var doc = result.Document!;
        Assert.Equal("poi", doc.LayerName);
        Assert.Equal("poi_style", doc.StyleName);
        Assert.Equal("1.0.0", doc.Version);
        Assert.Equal(3, doc.Rules.Count);

        var r1 = doc.Rules[0];
        Assert.Equal("r1", r1.Name);
        Assert.Equal("pop", r1.Filter!.Property);
        Assert.Equal(">", r1.Filter.Operator);
        Assert.Equal("1000", r1.Filter.Value);
        var p = Assert.IsType<SldPointSymbolizer>(r1.Symbolizer);
        Assert.Equal("square", p.WellKnownName);
        Assert.Equal(8d, p.Size!.Value);
        Assert.Equal("#FF0000", p.FillColor);
        Assert.Equal(0.5, p.FillOpacity!.Value);
        Assert.Equal("#000000", p.StrokeColor);
        Assert.Equal(1d, p.StrokeWidth!.Value);

        var r2 = doc.Rules[1];
        Assert.Null(r2.Filter);
        var l = Assert.IsType<SldLineSymbolizer>(r2.Symbolizer);
        Assert.Equal("#00FF00", l.StrokeColor);
        Assert.Equal(2d, l.StrokeWidth!.Value);

        var r3 = doc.Rules[2];
        Assert.Equal("LIKE", r3.Filter!.Operator);
        var poly = Assert.IsType<SldPolygonSymbolizer>(r3.Symbolizer);
        Assert.Equal("#0000FF", poly.FillColor);
        Assert.Equal(0.8, poly.FillOpacity!.Value);
        Assert.Equal("#FFFFFF", poly.StrokeColor);
        Assert.Equal(0.5, poly.StrokeWidth!.Value);
    }

    [Fact]
    public void Parse_ToleratesNamespacePrefixes()
    {
        const string xml = """
<?xml version="1.0" encoding="UTF-8"?>
<sld:StyledLayerDescriptor version="1.0.0" xmlns:sld="http://www.opengis.net/sld" xmlns:ogc="http://www.opengis.net/ogc">
  <sld:NamedLayer><sld:Name>poi</sld:Name>
    <sld:UserStyle><sld:Name>s</sld:Name>
      <sld:FeatureTypeStyle>
        <sld:Rule>
          <ogc:Filter><ogc:PropertyIsEqualTo><ogc:PropertyName>k</ogc:PropertyName><ogc:Literal>v</ogc:Literal></ogc:PropertyIsEqualTo></ogc:Filter>
          <sld:LineSymbolizer><sld:Stroke><sld:CssParameter name="stroke">#123456</sld:CssParameter></sld:Stroke></sld:LineSymbolizer>
        </sld:Rule>
      </sld:FeatureTypeStyle>
    </sld:UserStyle>
  </sld:NamedLayer>
</sld:StyledLayerDescriptor>
""";

        var r = SldParser.Parse(xml);

        Assert.True(r.Success);
        Assert.Equal("poi", r.Document!.LayerName);
        Assert.Equal("=", r.Document.Rules[0].Filter!.Operator);
        Assert.Equal("#123456", Assert.IsType<SldLineSymbolizer>(r.Document.Rules[0].Symbolizer).StrokeColor);
    }

    [Fact]
    public void Parse_ReadsVersionAttribute()
    {
        var r = SldParser.Parse(
            "<StyledLayerDescriptor version=\"1.1.0\"><NamedLayer><Name>a</Name><UserStyle><FeatureTypeStyle>"
            + "<Rule><LineSymbolizer/></Rule>"
            + "</FeatureTypeStyle></UserStyle></NamedLayer></StyledLayerDescriptor>");

        Assert.True(r.Success);
        Assert.Equal("1.1.0", r.Document!.Version);
    }

    // ---- 超子集警告 ----

    [Fact]
    public void Parse_MultipleNamedLayers_WarnsAndParsesFirst()
    {
        var r = SldParser.Parse(
            "<StyledLayerDescriptor version=\"1.0.0\">"
            + "<NamedLayer><Name>a</Name><UserStyle><FeatureTypeStyle><Rule><LineSymbolizer/></Rule></FeatureTypeStyle></UserStyle></NamedLayer>"
            + "<NamedLayer><Name>b</Name><UserStyle><FeatureTypeStyle><Rule><LineSymbolizer/></Rule></FeatureTypeStyle></UserStyle></NamedLayer>"
            + "</StyledLayerDescriptor>");

        Assert.True(r.Success);
        Assert.Equal("a", r.Document!.LayerName);
        Assert.Contains(r.Warnings, w => w.Contains("多个 NamedLayer"));
    }

    [Fact]
    public void Parse_MultipleUserStyles_WarnsAndParsesFirst()
    {
        var r = SldParser.Parse(
            "<StyledLayerDescriptor version=\"1.0.0\"><NamedLayer><Name>a</Name>"
            + "<UserStyle><Name>s1</Name><FeatureTypeStyle><Rule><LineSymbolizer/></Rule></FeatureTypeStyle></UserStyle>"
            + "<UserStyle><Name>s2</Name><FeatureTypeStyle><Rule><LineSymbolizer/></Rule></FeatureTypeStyle></UserStyle>"
            + "</NamedLayer></StyledLayerDescriptor>");

        Assert.True(r.Success);
        Assert.Equal("s1", r.Document!.StyleName);
        Assert.Contains(r.Warnings, w => w.Contains("多个 UserStyle"));
    }

    [Fact]
    public void Parse_MultipleFeatureTypeStyles_WarnsAndParsesFirst()
    {
        var r = SldParser.Parse(
            "<StyledLayerDescriptor version=\"1.0.0\"><NamedLayer><Name>a</Name><UserStyle>"
            + "<FeatureTypeStyle><Rule><LineSymbolizer/></Rule></FeatureTypeStyle>"
            + "<FeatureTypeStyle><Rule><PointSymbolizer/></Rule></FeatureTypeStyle>"
            + "</UserStyle></NamedLayer></StyledLayerDescriptor>");

        Assert.True(r.Success);
        Assert.Single(r.Document!.Rules);
        Assert.IsType<SldLineSymbolizer>(r.Document.Rules[0].Symbolizer);
        Assert.Contains(r.Warnings, w => w.Contains("多个 FeatureTypeStyle"));
    }

    [Fact]
    public void Parse_TextSymbolizerOnly_WarnsAndSkipsRule()
    {
        var r = SldParser.Parse(
            "<StyledLayerDescriptor version=\"1.0.0\"><NamedLayer><Name>a</Name><UserStyle><FeatureTypeStyle>"
            + "<Rule><TextSymbolizer><Label>name</Label></TextSymbolizer></Rule>"
            + "</FeatureTypeStyle></UserStyle></NamedLayer></StyledLayerDescriptor>");

        Assert.True(r.Success);
        Assert.Empty(r.Document!.Rules);
        Assert.Contains(r.Warnings, w => w.Contains("TextSymbolizer"));
        Assert.Contains(r.Warnings, w => w.Contains("已跳过"));
    }

    [Fact]
    public void Parse_MultipleSymbolizers_WarnsAndKeepsFirst()
    {
        var r = SldParser.Parse(
            "<StyledLayerDescriptor version=\"1.0.0\"><NamedLayer><Name>a</Name><UserStyle><FeatureTypeStyle>"
            + "<Rule><PointSymbolizer/><LineSymbolizer/></Rule>"
            + "</FeatureTypeStyle></UserStyle></NamedLayer></StyledLayerDescriptor>");

        Assert.True(r.Success);
        Assert.Single(r.Document!.Rules);
        Assert.IsType<SldPointSymbolizer>(r.Document.Rules[0].Symbolizer);
        Assert.Contains(r.Warnings, w => w.Contains("多个符号化器"));
    }

    [Fact]
    public void Parse_ScaleDenominator_WarnsAndIgnores()
    {
        var r = SldParser.Parse(
            "<StyledLayerDescriptor version=\"1.0.0\"><NamedLayer><Name>a</Name><UserStyle><FeatureTypeStyle>"
            + "<Rule><ScaleDenominator>1000</ScaleDenominator><LineSymbolizer/></Rule>"
            + "</FeatureTypeStyle></UserStyle></NamedLayer></StyledLayerDescriptor>");

        Assert.True(r.Success);
        Assert.Single(r.Document!.Rules);
        Assert.Contains(r.Warnings, w => w.Contains("ScaleDenominator"));
    }

    [Fact]
    public void Parse_UnsupportedFilterType_WarnsAndIgnoresFilter()
    {
        var r = SldParser.Parse(
            "<StyledLayerDescriptor version=\"1.0.0\" xmlns:ogc=\"http://www.opengis.net/ogc\">"
            + "<NamedLayer><Name>a</Name><UserStyle><FeatureTypeStyle>"
            + "<Rule><ogc:Filter><ogc:PropertyIsNull><ogc:PropertyName>k</ogc:PropertyName></ogc:PropertyIsNull></ogc:Filter>"
            + "<LineSymbolizer/></Rule>"
            + "</FeatureTypeStyle></UserStyle></NamedLayer></StyledLayerDescriptor>");

        Assert.True(r.Success);
        Assert.Single(r.Document!.Rules);
        Assert.Null(r.Document.Rules[0].Filter);
        Assert.Contains(r.Warnings, w => w.Contains("PropertyIsNull"));
    }

    [Fact]
    public void Parse_FilterMissingLiteral_WarnsAndIgnores()
    {
        var r = SldParser.Parse(
            "<StyledLayerDescriptor version=\"1.0.0\" xmlns:ogc=\"http://www.opengis.net/ogc\">"
            + "<NamedLayer><Name>a</Name><UserStyle><FeatureTypeStyle>"
            + "<Rule><ogc:Filter><ogc:PropertyIsEqualTo><ogc:PropertyName>k</ogc:PropertyName></ogc:PropertyIsEqualTo></ogc:Filter>"
            + "<LineSymbolizer/></Rule>"
            + "</FeatureTypeStyle></UserStyle></NamedLayer></StyledLayerDescriptor>");

        Assert.True(r.Success);
        Assert.Null(r.Document!.Rules[0].Filter);
        Assert.Contains(r.Warnings, w => w.Contains("缺少 PropertyName/Literal"));
    }
}
