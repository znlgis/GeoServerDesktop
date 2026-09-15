using System;
using GeoServerDesktop.GeoServerClient.Services;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>PreviewService 离线单元测试（非 REST、纯 URL 拼接）。FIXED-E38 回归固化：workspace Trim+转义、srs null 回退默认。</summary>
public class PreviewServiceTests
{
    private const string BaseUrl = "http://localhost:8080/geoserver";

    [Fact]
    public void Ctor_TrimsTrailingSlashes()
    {
        // 构造时 TrimEnd('/')：尾斜杠被裁掉，URL 拼接不出现双斜杠
        var svc = new PreviewService("http://host:8080/geoserver///");
        Assert.Equal("http://host:8080/geoserver/wms?service=WMS&version=1.1.0&request=GetCapabilities",
            svc.GetCapabilitiesUrl());
    }

    [Fact]
    public void Ctor_NullBaseUrl_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new PreviewService(null!));

    [Fact]
    public void GetWmsUrl_EscapesEveryValue()
    {
        var svc = new PreviewService(BaseUrl);

        var url = svc.GetWmsUrl("topp", "states", "EPSG:4326", "-180,-90,180,90");

        // 每个参数值都经 Uri.EscapeDataString（: , / 均被转义），参数名小写、顺序固定
        Assert.Equal(BaseUrl + "/wms?service=WMS&version=1.1.0&request=GetMap"
            + "&layers=topp%3Astates&srs=EPSG%3A4326&bbox=-180%2C-90%2C180%2C90"
            + "&width=800&height=600&format=image%2Fpng", url);
    }

    [Fact]
    public void GetWmsUrl_BlankWorkspace_LayersHasNoColonPrefix()
    {
        var svc = new PreviewService(BaseUrl);

        var url = svc.GetWmsUrl("   ", "states", "EPSG:4326", "0,0,1,1");

        Assert.Contains("layers=states&", url + "&"); // 无 "ws:" 前缀
        Assert.DoesNotContain("%3A", url.Split("layers=")[1].Split('&')[0]);
    }

    [Fact]
    public void GetWmsUrl_UsesCustomWidthHeightFormat()
    {
        var svc = new PreviewService(BaseUrl);

        var url = svc.GetWmsUrl("ws", "ly", "EPSG:3857", "1,2,3,4", width: 1024, height: 768, format: "image/jpeg");

        Assert.Contains("width=1024", url);
        Assert.Contains("height=768", url);
        Assert.Contains("format=image%2Fjpeg", url);
    }

    [Fact]
    public void GetWmsUrl_NullSrs_FallsBackToDefault_FIXED_E38()
    {
        // FIXED-E38：原 srs=null 时 Uri.EscapeDataString(null) 抛 ArgumentNullException；
        // 现 null/空白 srs 回退 EPSG:4326，bbox=null 按空串处理，均不再抛。
        var svc = new PreviewService(BaseUrl);

        var url = svc.GetWmsUrl("ws", "ly", null!, "0,0,1,1");
        Assert.Contains("srs=EPSG%3A4326", url);

        url = svc.GetWmsUrl("ws", "ly", "   ", null!);
        Assert.Contains("srs=EPSG%3A4326", url);
        Assert.Contains("bbox=&", url + "&"); // bbox null → 空值
    }

    [Fact]
    public void GetCapabilitiesUrl_WithoutWorkspace_UsesRootWmsPath()
    {
        var svc = new PreviewService(BaseUrl);
        Assert.Equal(BaseUrl + "/wms?service=WMS&version=1.1.0&request=GetCapabilities", svc.GetCapabilitiesUrl());
        Assert.Equal(svc.GetCapabilitiesUrl(), svc.GetCapabilitiesUrl("   ")); // 空白 workspace 同无参形态
    }

    [Fact]
    public void GetCapabilitiesUrl_WithWorkspace_TrimsAndEscapesSegment_FIXED_E38()
    {
        // FIXED-E38：workspace 段先 Trim 再 Uri.EscapeDataString（含空格/中文产生合法 URL）
        var svc = new PreviewService(BaseUrl);
        Assert.Equal(BaseUrl + "/ws/wms?service=WMS&version=1.1.0&request=GetCapabilities", svc.GetCapabilitiesUrl("ws"));

        Assert.Equal(BaseUrl + "/my%20ws/wms?service=WMS&version=1.1.0&request=GetCapabilities", svc.GetCapabilitiesUrl("my ws"));
        // 前后空白被 Trim，仅内部空格转义
        Assert.Equal(BaseUrl + "/my%20ws/wms?service=WMS&version=1.1.0&request=GetCapabilities", svc.GetCapabilitiesUrl("  my ws  "));
    }
}
