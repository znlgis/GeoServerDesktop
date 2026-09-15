using System;
using System.Linq;
using GeoServerDesktop.Tests.Infrastructure;
using GeoServerDesktop.Tests.RealData;

namespace GeoServerDesktop.Tests.RealDataTests
{
    /// <summary>
    /// 任务1：发布链路冒烟（库路径）。用 GeoServerClient 发布 gdtest_poly（工作空间→shapefile 存储→
    /// featuretype(Enabled=true)→自动 layer），随后用裸 HttpClient 独立通道确认图层可被 GetCapabilities 列出。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class PublishSmokeTests : RealDataTestBase
    {
        public PublishSmokeTests(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public void LibraryPublish_ExposedOnIndependentChannel()
        {
            if (!RequireReady()) return;

            // 库发布已由基类 EnsurePublished 完成；此处用独立通道（REST + GetCapabilities）交叉确认确实对外可见。
            var layer = E2ePublishHelper.Ws + ":" + E2ePublishHelper.PolyLayer;
            var rest = OgcProbe.Get($"{TestEnv.RestBase}/rest/layers/{layer}.json");
            var caps = OgcProbe.Get(OgcProbe.Wms("request=GetCapabilities", "version=1.1.1"));

            var r1 = Check.Cond(rest.Ok && rest.Text != null && rest.Text.Contains("\"layer\""),
                "Publish/rest-layer", "REST 独立通道可取 gdtest_ws_e2e:gdtest_poly",
                $"GET rest/layers/{layer}.json → HTTP {rest.Status}");
            var r2 = Check.Cond(caps.Ok && caps.Text != null && caps.Text.Contains(layer),
                "Publish/wms-caps", "WMS GetCapabilities 列出该图层",
                $"{layer} 不在 WMS 能力表（HTTP {caps.Status}）");

            Check.ThrowOnFail(r1, r2);
        }
    }
}
