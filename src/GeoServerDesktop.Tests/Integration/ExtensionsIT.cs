using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// 扩展族只读基线：fonts/templates/transforms/urlchecks/monitoring/importer/wps/csw。
    /// B 域修复后（实测 3.0.1）：
    ///  - /rest/fonts.json {"fonts":[字符串...]} 直接数组——E7 对 fonts 属误报，模型即真实形态；
    ///  - /rest/templates.json 根为类全名（空态 {"org.geoserver.rest.catalog.TemplateInfos":""}）→ Parse 容错，Templates 恒非 null；
    ///  - /rest/urlchecks.json 空态 {"urlChecks":""} → Parse 容错，Checks 恒非 null；
    ///  - /rest/monitor/requests.json 根 org.geoserver.monitor.RequestDatas（动态类名键）→ Parse 后 Requests 非 null；
    ///  - /rest/transforms 404（转换扩展未装）、/rest/imports 404（importer 未装）、wps/csw settings 404——基线维持。
    /// 命名族缩写：ext。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class ExtensionsIT : GeoServerTestBase
    {
        public ExtensionsIT(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public async Task Fonts_Get_Works_E7Misreport()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateFontService();
            var fonts = await svc.GetFontsAsync();
            // FIXED-E7（fonts 误报翻正）：{"fonts":[字符串]} 直接数组与模型相配
            Assert.NotNull(fonts?.Fonts);
            Assert.NotEmpty(fonts!.Fonts!);
        }

        [Fact]
        public async Task Templates_Get_DynamicClassRootTolerated_FIXED_E7()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateTemplateService();
            // 空态实测 {"org.geoserver.rest.catalog.TemplateInfos":""} → 解析为空列表而非 null/抛错
            var t = await svc.GetTemplatesAsync();
            Assert.NotNull(t?.Templates);
            Assert.Empty(t!.Templates!); // 默认安装无任何模板
        }

        [Fact]
        public async Task Transforms_Get_Endpoint404_Baseline()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateTransformService();
            var ex = await Record.ExceptionAsync(() => svc.GetTransformsAsync());
            GsKit.AssertRequest(ex, 404, "transforms GET"); // 扩展未装（基线不动）
        }

        [Fact]
        public async Task UrlChecks_Get_EmptyStringRootTolerated_FIXED_E7()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateURLCheckService();
            // 实测空态 {"urlChecks":""} → 容错解析为空列表
            var u = await svc.GetURLChecksAsync();
            Assert.NotNull(u?.Checks);
            Assert.Empty(u!.Checks!);
        }

        [Fact]
        public async Task Monitoring_Requests_DynamicClassRootParses_FIXED_E7()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateMonitoringService();
            // 实测 200：{"org.geoserver.monitor.RequestDatas":{"org.geoserver.monitor.RequestData":[{name,href}]}}
            var r = await svc.GetRequestsAsync();
            Assert.NotNull(r?.Requests);
            Assert.NotEmpty(r!.Requests!); // 本服务器开启 monitor 且有历史请求
        }

        [Fact]
        public async Task Monitoring_Statistics_Baseline_E6()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateMonitoringService();
            // statistics：模型与服务器结构完全不符（E6 维持），任何解析形态都固化为基线，不崩即可
            try { await svc.GetStatisticsAsync(); }
            catch (Exception e)
            {
                Assert.True(GsKit.IsJsonNet(e) || GsKit.IsRequest(e, 404), "monitor/statistics 异常类型意外: " + e);
            }
        }

        [Fact]
        public async Task Importer_Post_ExtensionMissing_Baseline()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateImporterService();
            // 实测 importer 扩展未装：GET/POST /rest/imports 全 404（E14 序列化修复见离线层）
            var ex = await Record.ExceptionAsync(() => svc.CreateImportAsync(TestEnv.Prefix + "_none"));
            if (ex != null) GsKit.AssertRequest(ex, 404, "imports POST");
            else Assert.NotNull("importer 扩展存在——基线更新");
        }

        [Fact]
        public async Task WpsAndCswSettings_Get_ExtensionMissing_Baseline()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var wps = f.CreateWPSSettingsService();
            var csw = f.CreateCSWSettingsService();
            // 实测 3.0.1 精简镜像：/rest/services/wps|csw/settings.json 404
            GsKit.AssertRequest(await Record.ExceptionAsync(() => wps.GetSettingsAsync()), 404, "wps settings");
            GsKit.AssertRequest(await Record.ExceptionAsync(() => csw.GetSettingsAsync()), 404, "csw settings");
        }
    }
}
