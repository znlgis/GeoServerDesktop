using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>FontService 离线单元测试。FIXED-E7（fonts 属误报翻正）：实测 /rest/fonts.json 返回
/// {"fonts":["字符串",...]} 直接数组，与 List&lt;string&gt; 模型恰配、无解析缺陷。</summary>
public class FontServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly FontService _svc;

    public FontServiceTests() => _svc = new FontService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new FontService(null!));

    [Fact]
    public async Task GetFontsAsync_RealDirectStringArrayParses_FIXED_E7_MISREPORT()
    {
        _fake.RespondGet("""{"fonts":["Arial","Serif"]}""");
        var w = await _svc.GetFontsAsync();
        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/fonts.json", _fake.Last.Path);
        Assert.Equal(2, w.Fonts.Count);
    }

    [Fact]
    public async Task UploadFontAsync_PutsRawBytesOctetStreamUnderFontName()
    {
        var ttf = new byte[] { 0x00, 0x01, 0x00, 0x00 };
        await _svc.UploadFontAsync("MyFont.ttf", ttf);
        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/fonts/MyFont.ttf", _fake.Last.Path);
        Assert.Equal("application/octet-stream", _fake.Last.ContentType);
        Assert.Equal(ttf, _fake.Last.RawBody);
    }
}
