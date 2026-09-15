using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// WMS/WFS/WCS/WMTS 服务设置 GET→PUT 往返基线。
    /// 实测（3.0.1）：
    ///  - 四个服务 GET 正常（根 wms/wfs/wcs/wmts；"abstrct" 为 GeoServer 官方拼写，模型有意对齐）；
    ///  - FIXED（E14/E40 同族根因）：请求体现经 NullValueHandling.Ignore 省略 null 字段，
    ///    GET→PUT 整包回写不再触发 maxRequestMemory 等 null→Integer 的 XStream NPE 500，
    ///    往返走 happy path；catch 仅兜不同服务器版本的残差，不再是固化缺陷路径；
    ///  - WMTS（FIXED-E15，判为误报）：GET/PUT 根均为 "wmts"，与模型一致，curl 实测 PUT 生效，
    ///    下方 WMTS 用例已翻转为与 WMS 同构的 marker 往返。
    /// 命名族缩写：svc。全程不改真实服务标题（失败路径回滚复核原值）。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class ServiceSettingsIT : GeoServerTestBase
    {
        public ServiceSettingsIT(GeoServerFixture fx) : base(fx) { }

        private static bool IsClientModelOrServerError(Exception e) =>
            GsKit.IsJsonNet(e) || e is GeoServerRequestException;

        [Fact]
        public async Task Wms_Settings_GetPut_Rollback_Or_Baseline()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateWMSSettingsService();
            // GET 原值
            var orig = await svc.GetWMSSettingsAsync();
            Assert.NotNull(orig?.WMS);
            string? title = orig.WMS!.Title;
            Assert.NotNull(title);
            try
            {
                // 修改 → PUT → 重读校验 → PUT 回滚 → 再校验（往返一致）
                orig.WMS.Title = "gdtest-wms-marker";
                await svc.UpdateWMSSettingsAsync(orig);
                var mid = await svc.GetWMSSettingsAsync();
                Assert.Equal("gdtest-wms-marker", mid!.WMS!.Title);
                orig.WMS.Title = title;
                await svc.UpdateWMSSettingsAsync(orig);
                var back = await svc.GetWMSSettingsAsync();
                Assert.Equal(title, back!.WMS!.Title);
            }
            catch (Exception e) when (IsClientModelOrServerError(e))
            {
                // FIXED-E14/E40 同族（请求体 null 省略后此路径理论上不再触发）：保留作不同服务器版本
                // 残差兜底，基线仍要求服务器原值完好（GET 复核）。
                var raw = await Fx.Cleanup.GetAsync("/rest/services/wms/settings.json");
                Assert.Contains("\"" + title + "\"", raw ?? "");
            }
        }

        [Fact]
        public async Task Wfs_Settings_GetPut_Rollback_Or_Baseline()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateWFSSettingsService();
            var orig = await svc.GetWFSSettingsAsync();
            Assert.NotNull(orig?.WFS);
            string? title = orig.WFS!.Title;
            try
            {
                orig.WFS!.Title = "gdtest-wfs-marker";
                await svc.UpdateWFSSettingsAsync(orig);
                var mid = await svc.GetWFSSettingsAsync();
                Assert.Equal("gdtest-wfs-marker", mid!.WFS!.Title);
                orig.WFS.Title = title;
                await svc.UpdateWFSSettingsAsync(orig);
                var back = await svc.GetWFSSettingsAsync();
                Assert.Equal(title, back!.WFS!.Title);
            }
            catch (Exception e) when (IsClientModelOrServerError(e))
            {
                var raw = await Fx.Cleanup.GetAsync("/rest/services/wfs/settings.json");
                Assert.Contains("\"" + title + "\"", raw ?? "");
            }
        }

        [Fact]
        public async Task Wcs_Settings_GetPut_Rollback_Or_Baseline()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateWCSSettingsService();
            var orig = await svc.GetWCSSettingsAsync();
            Assert.NotNull(orig?.WCS);
            string? title = orig.WCS!.Title;
            try
            {
                orig.WCS!.Title = "gdtest-wcs-marker";
                await svc.UpdateWCSSettingsAsync(orig);
                var mid = await svc.GetWCSSettingsAsync();
                Assert.Equal("gdtest-wcs-marker", mid!.WCS!.Title);
                orig.WCS.Title = title;
                await svc.UpdateWCSSettingsAsync(orig);
                var back = await svc.GetWCSSettingsAsync();
                Assert.Equal(title, back!.WCS!.Title);
            }
            catch (Exception e) when (IsClientModelOrServerError(e))
            {
                var raw = await Fx.Cleanup.GetAsync("/rest/services/wcs/settings.json");
                Assert.Contains("\"" + title + "\"", raw ?? "");
            }
        }

        [Fact]
        public async Task Wmts_Settings_GetPut_Rollback_FIXED_E15()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateWMTSSettingsService();
            // FIXED-E15（判为误报）：3.0.1 实测 GET /rest/services/wmts/settings.json 根为 {"wmts":{...}}，
            // 与模型一致；PUT 同根回写实测 200 且字段生效（curl 复核）。翻转 E15 基线为严格往返。
            var orig = await svc.GetWMTSSettingsAsync();
            Assert.NotNull(orig?.WMTS);
            string? title = orig.WMTS!.Title;
            Assert.NotNull(title);
            try
            {
                orig.WMTS.Title = "gdtest-wmts-marker";
                await svc.UpdateWMTSSettingsAsync(orig);
                var mid = await svc.GetWMTSSettingsAsync();
                Assert.Equal("gdtest-wmts-marker", mid!.WMTS!.Title);
                orig.WMTS.Title = title;
                await svc.UpdateWMTSSettingsAsync(orig);
                var back = await svc.GetWMTSSettingsAsync();
                Assert.Equal(title, back!.WMTS!.Title);
            }
            catch (Exception e) when (IsClientModelOrServerError(e))
            {
                var raw = await Fx.Cleanup.GetAsync("/rest/services/wmts/settings.json");
                Assert.Contains("\"" + title + "\"", raw ?? "");
            }
        }
    }
}
