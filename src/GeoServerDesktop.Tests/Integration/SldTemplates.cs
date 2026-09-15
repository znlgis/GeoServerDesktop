namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// 集成测试用规范 SLD 模板（纯色 PolygonSymbolizer 填充）。
    /// 实测（GeoServer 3.0.1）：非规范 SLD（如 NamedLayer/UserLayer 无样式体）PUT .sld 会 400，
    /// 必须提供合法的 PolygonSymbolizer + Fill/online_resource 结构。
    /// </summary>
    public static class SldTemplates
    {
        /// <summary>10x10 纯红填充（#FF0000）面样式。</summary>
        public const string Red = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<StyledLayerDescriptor version=""1.0.0"" xmlns=""http://www.opengis.net/sld"" xmlns:ogc=""http://www.opengis.net/ogc"" xmlns:xlink=""http://www.w3.org/1999/xlink"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://www.opengis.net/sld http://schemas.opengis.net/sld/1.0.0/StyledLayerDescriptor.xsd"">
  <NamedLayer>
    <Name>gdtest</Name>
    <UserStyle>
      <Title>gdtest red</Title>
      <FeatureTypeStyle>
        <Rule>
          <PolygonSymbolizer>
            <Fill>
              <CssParameter name=""fill"">#FF0000</CssParameter>
            </Fill>
          </PolygonSymbolizer>
        </Rule>
      </FeatureTypeStyle>
    </UserStyle>
  </NamedLayer>
</StyledLayerDescriptor>";

        /// <summary>纯蓝填充（#0000FF）面样式（PUT 更新用）。</summary>
        public const string Blue = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<StyledLayerDescriptor version=""1.0.0"" xmlns=""http://www.opengis.net/sld"" xmlns:ogc=""http://www.opengis.net/ogc"" xmlns:xlink=""http://www.w3.org/1999/xlink"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://www.opengis.net/sld http://schemas.opengis.net/sld/1.0.0/StyledLayerDescriptor.xsd"">
  <NamedLayer>
    <Name>gdtest</Name>
    <UserStyle>
      <Title>gdtest blue</Title>
      <FeatureTypeStyle>
        <Rule>
          <PolygonSymbolizer>
            <Fill>
              <CssParameter name=""fill"">#0000FF</CssParameter>
            </Fill>
          </PolygonSymbolizer>
        </Rule>
      </FeatureTypeStyle>
    </UserStyle>
  </NamedLayer>
</StyledLayerDescriptor>";
    }
}
