using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// GeoWebCache REST 族集成测试（GWC 2.0.1 内置于 GeoServer 3.0.1，B 域修复后基线）。
    /// FIXED-E8/E9/E10/E11/E13 实测翻转：
    ///  - layers/gridsets/blobstores 列表端点为顶层裸 JSON 数组（模型 RawStringArrayConverter 承接）；
    ///  - 单体 gridset 根 {"gridSet":{...}}（大写 S）；单体 layer 根 {"GeoServerLayer":{...}}；单体 blobstore 根为类名；
    ///  - diskquota 恒 XML（读侧映射到模型；写侧同格式，本类只读不写）；
    ///  - /gwc/rest/settings 实测 404：GWC 2.0.1 无该 REST 端点（gwc-rest jar 仅有 layers/gridsets/blobstores/
    ///    diskquota/seed/truncate/reload 等控制器）——固化为"无 REST 端点"基线；
    ///  - seed/truncate/masstruncate 为写操作，只读类不触发（离线单测已固化 XML 契约）。
    /// 本类只读，不做任何写/清缓存操作。命名族缩写：gwc。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class GWCRestIT : GeoServerTestBase
    {
        public GWCRestIT(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public async Task Gridsets_List_RawArrayParses_FIXED_E8()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateGridsetService();
            var w = await svc.GetGridsetsAsync();
            Assert.NotNull(w?.GridSets);
            Assert.Contains(w!.GridSets!, g => g.Contains("Quad"));
        }

        [Fact]
        public async Task Gridsets_Single_GridSetRootParses_FIXED_E9()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateGridsetService();
            // 实测 {"gridSet":{...}} 大写 S 根 + extent.coords；层名含 ":" 已转义
            var g = await svc.GetGridsetAsync("EPSG:4326");
            Assert.NotNull(g);
            Assert.Equal("EPSG:4326", g!.Name);
            Assert.Equal(4326, g.SRS!.Number);
            Assert.Equal(4, g.Extent!.Coords!.Length);
        }

        [Fact]
        public async Task GwcLayers_List_RawArrayParses_FIXED_E8()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateGWCLayerService();
            var w = await svc.GetLayersAsync();
            Assert.NotNull(w?.Layers); // 默认安装可能为空数组，但必须可解析
        }

        [Fact]
        public async Task GwcLayers_Single_GeoServerLayerRootParses_FIXED_E9()
        {
            if (!RequireGeoServer()) return;
            E2ePublisher.EnsurePublishedLayer(E2ePublisher.Ws, TestEnv.Prefix + "_poly");
            using var f = Fx.Factory();
            var svc = f.CreateGWCLayerService();
            var list = await svc.GetLayersAsync();
            var name = E2ePublisher.PolyLayerQualified;
            if (list?.Layers == null || !list.Layers.Contains(name))
            {
                // 图层尚未在 GWC 注册时 GET 实测回 500（"Failed to get layer..."）——容错为不崩即可
                var ex = await Record.ExceptionAsync(() => svc.GetLayerAsync(name));
                if (ex != null) Assert.True(ex is GeoServerRequestException || GsKit.IsJsonNet(ex), "GWC layer GET 异常类型意外: " + ex);
                return;
            }
            var l = await svc.GetLayerAsync(name);
            Assert.Equal(name, l?.Name);
            Assert.NotNull(l!.GridSubsets);
        }

        [Fact]
        public async Task DiskQuota_Get_XmlMappedToModel_FIXED_E13()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateDiskQuotaService();
            // 实测恒 XML（org.geowebcache.diskquota.DiskQuotaConfig）→ XDocument 映射
            var c = await svc.GetDiskQuotaAsync();
            Assert.NotNull(c);
            Assert.True(c!.Enabled);
            Assert.Equal("SECONDS", c.CacheCleanUpUnits);
            Assert.NotNull(c.GlobalQuota);
            Assert.True(c.GlobalQuota!.Bytes > 0);
        }

        [Fact]
        public async Task Blobstores_List_RawArrayParses_FIXED_E8()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateBlobstoreService();
            var w = await svc.GetBlobstoresAsync();
            Assert.NotNull(w?.Blobstores); // 默认安装 [] 也须可解析
        }

        [Fact]
        public async Task GwcSettings_NoRestEndpoint_Baseline()
        {
            if (!RequireGeoServer()) return;
            // /gwc/rest/settings 实测 404（gwc-rest 无 SettingsController）——GWC 默认设置无 REST 端点
            var status = await GsKit.RawStatusAsync(Fx, "/gwc/rest/settings");
            Assert.Equal(404, status);
        }
    }
}
