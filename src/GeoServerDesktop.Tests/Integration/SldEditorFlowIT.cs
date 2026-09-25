using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Sld;
using GeoServerDesktop.Tests.Infrastructure;
using GeoServerDesktop.Tests.RealData;
using SkiaSharp;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// M3 SLD 编辑器全链路集成测试：结构化模型 → 生成 SLD → 保存（两步）→ 绑定图层 →
    /// WMS 出图像素验证（红→蓝更新）→ 解绑 → 删除；覆盖使用关系聚合与非法 SLD 双防线。
    /// 像素真值经裸 HttpClient（OgcProbe）独立回抄，不经被测库。
    /// 共享层 gdtest_poly / gdtest_lines 经 E2ePublisher 幂等就绪。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class SldEditorFlowIT : GeoServerTestBase
    {
        public SldEditorFlowIT(GeoServerFixture fx) : base(fx) { }

        private const string StyleName = TestEnv.Prefix + "_sld_flow";      // gdtest_sld_flow
        private const string LineStyleName = TestEnv.Prefix + "_sld_line";  // gdtest_sld_line
        private const string LayerName = TestEnv.Prefix + "_poly";          // gdtest_poly
        private const string LinesLayer = TestEnv.Prefix + "_lines";        // gdtest_lines

        [Fact]
        public async Task StructuredEdit_Save_Bind_WmsPixel_FullFlow()
        {
            if (!RequireGeoServer()) return;
            E2ePublisher.EnsurePublishedLayer(E2ePublisher.Ws, LayerName);
            using var f = Fx.Factory();
            var stSvc = f.CreateStyleService();
            var lySvc = f.CreateLayerService();
            string originalStyle = "polygon";
            try
            {
                await GsKit.WipeStyleAsync(Fx, StyleName);

                // 1) 结构化模型 → SLD（M3 生成器），本地校验通过
                var redSld = SldBuilder.Build(PolygonDoc(StyleName, "#FF0000"));
                Assert.True(SldValidator.Validate(redSld).IsValid);

                // 2) 保存（POST 元数据 + PUT SLD 两步）；读回可被编辑器解析器 round-trip
                await stSvc.CreateStyleAsync(StyleName, redSld);
                var fetched = await stSvc.GetStyleSldAsync(StyleName);
                Assert.Contains("#FF0000", fetched);
                var parsed = SldParser.Parse(fetched);
                Assert.True(parsed.Success, parsed.Error);
                // 实测（3.0.1）：读回时 GeoServer 将 NamedLayer/UserStyle 名规范化重写为 "Default Styler"
                //（颜色等样式体保留）；编辑器加载已有样式时样式名以 REST 资源名为准，不信任 SLD 内名字
                Assert.Equal("Default Styler", parsed.Document!.LayerName);
                Assert.Equal("Default Styler", parsed.Document.StyleName);
                Assert.Equal("#FF0000",
                    Assert.IsType<SldPolygonSymbolizer>(parsed.Document.Rules[0].Symbolizer).FillColor);

                // 3) 绑定图层
                var layer = await lySvc.GetLayerAsync(LayerName);
                Assert.NotNull(layer);
                originalStyle = layer!.DefaultStyle?.Name ?? "polygon";
                await lySvc.UpdateLayerAsync(LayerName, BuildLayerUpdate(layer, StyleName));
                var bound = await lySvc.GetLayerAsync(LayerName);
                Assert.Equal(StyleName, bound!.DefaultStyle!.Name);

                // 4) 使用关系聚合：绑定后样式被引用
                // 实测：/rest/layers.json 的 name 为 qualified 名（workspace:layer），Layers 记录该全名
                var usages = await f.CreateStyleUsageService().GetStyleUsageAsync();
                var usage = usages.FirstOrDefault(u => u.StyleName == StyleName);
                Assert.NotNull(usage);
                Assert.True(usage!.IsUsed);
                Assert.Contains(E2ePublisher.Ws + ":" + LayerName, usage.Layers);

                // 5) WMS 像素验证：红
                var red = WmsPixelStats(LayerName, StyleName);
                Assert.True(red.red > 0, $"红色像素应为正：red={red.red} distinct={red.distinct}");
                Assert.True(red.distinct > 1, $"防白屏：distinct={red.distinct}");

                // 6) 编辑（换蓝色）→ 更新 → 像素验证：蓝
                await stSvc.UpdateStyleAsync(StyleName, SldBuilder.Build(PolygonDoc(StyleName, "#0000FF")));
                var blue = WmsPixelStats(LayerName, StyleName);
                Assert.True(blue.blue > 0, $"蓝色像素应为正：blue={blue.blue} distinct={blue.distinct}");
                Assert.True(blue.distinct > 1, $"防白屏：distinct={blue.distinct}");

                // 7) 解绑 → 删除 → 404
                await lySvc.UpdateLayerAsync(LayerName, BuildLayerUpdate(layer, originalStyle));
                await stSvc.DeleteStyleAsync(StyleName, purge: true);
                var exGone = await Record.ExceptionAsync(() => stSvc.GetStyleAsync(StyleName));
                GsKit.AssertRequest(exGone, 404, "删除后 GET 样式");
            }
            finally
            {
                // 兜底：先尽力解绑再删
                try
                {
                    var l = await lySvc.GetLayerAsync(LayerName);
                    if (l?.DefaultStyle?.Name == StyleName)
                        await lySvc.UpdateLayerAsync(LayerName, BuildLayerUpdate(l, originalStyle));
                }
                catch { }
                await GsKit.WipeStyleAsync(Fx, StyleName);
            }
        }

        [Fact]
        public async Task LineSymbolizer_Save_Bind_WmsPixel()
        {
            if (!RequireGeoServer()) return;
            E2ePublisher.EnsurePublishedLayer(E2ePublisher.Ws, LinesLayer);
            using var f = Fx.Factory();
            var stSvc = f.CreateStyleService();
            var lySvc = f.CreateLayerService();
            string originalStyle = "line";
            try
            {
                await GsKit.WipeStyleAsync(Fx, LineStyleName);

                var lineSld = SldBuilder.Build(LineDoc(LineStyleName, "#FF0000", 3));
                Assert.True(SldValidator.Validate(lineSld).IsValid);
                await stSvc.CreateStyleAsync(LineStyleName, lineSld);

                var layer = await lySvc.GetLayerAsync(LinesLayer);
                Assert.NotNull(layer);
                originalStyle = layer!.DefaultStyle?.Name ?? "line";
                await lySvc.UpdateLayerAsync(LinesLayer, BuildLayerUpdate(layer, LineStyleName));
                var bound = await lySvc.GetLayerAsync(LinesLayer);
                Assert.Equal(LineStyleName, bound!.DefaultStyle!.Name);

                var stats = WmsPixelStats(LinesLayer, LineStyleName);
                Assert.True(stats.red > 0, $"线样式红色像素应为正：red={stats.red} distinct={stats.distinct}");
                Assert.True(stats.distinct > 1, $"防白屏：distinct={stats.distinct}");

                // 解绑 → 删除
                await lySvc.UpdateLayerAsync(LinesLayer, BuildLayerUpdate(layer, originalStyle));
                await stSvc.DeleteStyleAsync(LineStyleName, purge: true);
            }
            finally
            {
                try
                {
                    var l = await lySvc.GetLayerAsync(LinesLayer);
                    if (l?.DefaultStyle?.Name == LineStyleName)
                        await lySvc.UpdateLayerAsync(LinesLayer, BuildLayerUpdate(l, originalStyle));
                }
                catch { }
                await GsKit.WipeStyleAsync(Fx, LineStyleName);
            }
        }

        [Fact]
        public async Task InvalidSld_RejectedByLocalValidator_And_Server()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var stSvc = f.CreateStyleService();
            try
            {
                await GsKit.WipeStyleAsync(Fx, StyleName);

                const string invalid =
                    "<?xml version=\"1.0\"?><StyledLayerDescriptor version=\"1.0.0\"><NotValid/></StyledLayerDescriptor>";

                // 本地防线：编辑器"应用前校验"拒绝
                Assert.False(SldValidator.Validate(invalid).IsValid);

                // 服务端防线：先建合法样式，再绕过本地校验直接 PUT 非法内容 → 400（上传即校验）
                await stSvc.CreateStyleAsync(StyleName, SldBuilder.Build(PolygonDoc(StyleName, "#FF0000")));
                var ex = await Record.ExceptionAsync(() => stSvc.UpdateStyleAsync(StyleName, invalid));
                GsKit.AssertRequest(ex, 400, "上传非法 SLD");

                // 拒绝后旧内容未被破坏
                var sld = await stSvc.GetStyleSldAsync(StyleName);
                Assert.Contains("#FF0000", sld);
            }
            finally
            {
                await GsKit.WipeStyleAsync(Fx, StyleName);
            }
        }

        // ---- 辅助 ----

        /// <summary>纯色面文档（Fill + Stroke 同色）。</summary>
        private static SldDocument PolygonDoc(string styleName, string color)
        {
            var doc = new SldDocument { LayerName = styleName };
            doc.Rules.Add(new SldRule
            {
                Symbolizer = new SldPolygonSymbolizer
                {
                    FillColor = color,
                    FillOpacity = 1,
                    StrokeColor = color,
                    StrokeWidth = 1,
                },
            });
            return doc;
        }

        /// <summary>纯色线文档。</summary>
        private static SldDocument LineDoc(string styleName, string color, double width)
        {
            var doc = new SldDocument { LayerName = styleName };
            doc.Rules.Add(new SldRule
            {
                Symbolizer = new SldLineSymbolizer { StrokeColor = color, StrokeWidth = width },
            });
            return doc;
        }

        /// <summary>WMS GetMap 像素统计（与 L3 WmsRedPixels 同技术：红/蓝像素 + distinct 防白屏）。</summary>
        private static (int red, int blue, int distinct) WmsPixelStats(string layer, string styleName)
        {
            var r = OgcProbe.Get(OgcProbe.Wms("request=GetMap", "version=1.1.1", "layers=" + layer,
                "styles=" + styleName, "bbox=0,0,11,6", "width=220", "height=120", "srs=EPSG:4326", "format=image/png"));
            Assert.True(r.Ok, $"GetMap 应成功：HTTP {r.Status}");
            using var bmp = SKBitmap.Decode(r.Bytes);
            Assert.NotNull(bmp);
            int red = 0, blue = 0;
            var distinct = new HashSet<uint>();
            for (int y = 0; y < bmp!.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    var p = bmp.GetPixel(x, y);
                    distinct.Add(((uint)p.Red << 16) | ((uint)p.Green << 8) | p.Blue);
                    if (p.Red > 150 && p.Green < 80 && p.Blue < 80) red++;
                    if (p.Blue > 150 && p.Red < 80 && p.Green < 80) blue++;
                }
            }
            return (red, blue, distinct.Count);
        }

        /// <summary>PUT layer 必须回传填实的 resource 默认风格引用（null 引用 → 500 NPE 同族缺陷）。</summary>
        private static Layer BuildLayerUpdate(Layer orig, string defaultStyleName) => new Layer
        {
            Name = orig.Name,
            Type = orig.Type,
            DefaultStyle = new StyleReference { Name = defaultStyleName, Href = "" },
            Resource = new ResourceReference
            {
                Class = orig.Resource?.Class ?? "featureType",
                Name = orig.Resource?.Name ?? "",
                Href = "",
            },
            Href = "",
        };
    }
}
