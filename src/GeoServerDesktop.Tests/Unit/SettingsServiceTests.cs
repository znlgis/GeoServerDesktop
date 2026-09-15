using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>SettingsService（GeoServer 全局设置）离线单元测试。含 E17 contact 嵌套结构风险点。</summary>
public class SettingsServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly SettingsService _svc;

    public SettingsServiceTests() => _svc = new SettingsService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new SettingsService(null!));

    [Fact]
    public async Task GetGlobalSettingsAsync_ParsesRealGlobalRoot()
    {
        // FIXED（E17 复核族）：3.0.1 GET /rest/settings.json 实测根为 {"global":{"settings":{...}}}，
        // 原模型根 "settings" 与真实形态不符导致 Settings 恒 null。现按真实形态解析。
        _fake.RespondGet("""{"global":{"settings":{"id":"SettingsInfoImpl-1","charset":"UTF-8","numDecimals":3,"onlineResource":"http://o","verbose":false,"verboseExceptions":true,"contact":{"contactPerson":"Nuke","contactEmail":"n@e"},"proxyBaseUrl":"http://p","loggingLocation":"L","globalServices":false,"maxFeatures":100,"localWorkspaceIncludesPrefix":false,"useHeadersProxyURL":false},"updateSequence":5791,"webUIMode":"DEFAULT","metadata":{"entry":{"@key":"logRequestsEnabled","$":"false"}}}}""");

        var s = await _svc.GetGlobalSettingsAsync();

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/settings.json", _fake.Last.Path);
        Assert.Equal("UTF-8", s.Settings.Charset);
        Assert.Equal("SettingsInfoImpl-1", s.Settings.Id);
        Assert.Equal(3, s.Settings.NumDecimals);
        Assert.True(s.Settings.VerboseExceptions);
        Assert.Equal("Nuke", s.Settings.Contact.ContactPerson);
        Assert.Equal("http://p", s.Settings.ProxyBaseUrl);
        Assert.Equal(100, s.Settings.MaxFeatures);
        Assert.False(s.Settings.LocalWorkspaceIncludesPrefix);
        Assert.NotNull(s.Global.ExtensionData); // 模型外键（updateSequence/webUIMode/metadata）经 ExtensionData 捕获
    }

    [Fact]
    public async Task UpdateGlobalSettingsAsync_PutsWrappedUnderGlobalRootAndKeepsExtensionData()
    {
        // 整包 PUT 语义：GET 快照 → 改目标字段 → PUT 必须把模型外键原样带回（防丢字段）
        _fake.RespondGet("""{"global":{"settings":{"charset":"UTF-8","numDecimals":8,"metadata":{"entry":{"@key":"logRequestsEnabled","$":"false"}}},"updateSequence":42,"webUIMode":"DEFAULT"}}""");
        var snap = await _svc.GetGlobalSettingsAsync();
        snap.Settings.Charset = "ISO-8859-1";

        await _svc.UpdateGlobalSettingsAsync(snap);

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/settings", _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        var settings = body["global"]?["settings"];
        Assert.Equal("ISO-8859-1", (string?)settings?["charset"]);
        Assert.Equal(8, (int?)settings?["numDecimals"]);                 // 未动的字段保留
        Assert.Equal("false", (string?)settings?["metadata"]?["entry"]?["$"]); // ExtensionData 原样回写
        Assert.Equal(42, (int?)body["global"]?["updateSequence"]);
        Assert.Equal("DEFAULT", (string?)body["global"]?["webUIMode"]);
    }

    [Fact]
    public async Task UpdateGlobalSettingsAsync_NewObjectOmitsNullFields()
    {
        // 请求体 null 省略（根因修复族）：新建局部设置只发显式字段
        await _svc.UpdateGlobalSettingsAsync(new GlobalSettings { Settings = new Settings { Charset = "ISO-8859-1" } });

        Assert.Equal("PUT", _fake.Last!.Method);
        var body = Json.P(_fake.Last.Body);
        var settings = body["global"]?["settings"];
        Assert.Equal("ISO-8859-1", (string?)settings?["charset"]);
        Assert.Null(settings?["numDecimals"]); // null 字段不再以 JSON null 形式发送
        Assert.Null(settings?["contact"]);
    }

    [Fact]
    public async Task GetContactInfoAsync_RealGeoServerFlatShapeParses_FIXED_E17()
    {
        // FIXED-E17（判为误报 + 模型补齐）：实测 3.0.1 GET /rest/settings/contact.json 就是
        // {"contact":{...单层平铺字段...}}（address* 为普通字符串字段名，无嵌套 address 对象）。
        // 原"双层结构导致 JsonReaderException/字段全 null"的担忧基于错误形态假设；
        // 现补齐 GET 实际出现的 onlineResource/welcome 键，读写字段完整。
        _fake.RespondGet("""{"contact":{"addressCity":"Alexandria","addressCountry":"Roman Empire","addressState":"Egypt","addressType":"Work","contactEmail":"c@o","contactOrganization":"OSGeo","contactPerson":"P","contactPosition":"Chief","onlineResource":"https://www.osgeo.org/","welcome":"GeoServer publishes data."}}""");

        var w = await _svc.GetContactInfoAsync();

        Assert.Equal("/rest/settings/contact.json", _fake.Last!.Path);
        Assert.Equal("P", w.Contact.ContactPerson);
        Assert.Equal("Alexandria", w.Contact.AddressCity);
        Assert.Equal("https://www.osgeo.org/", w.Contact.OnlineResource);
        Assert.Equal("GeoServer publishes data.", w.Contact.Welcome);
    }

    [Fact]
    public async Task UpdateContactInfoAsync_PutsSameFlatShapeAsGet_FIXED_E17()
    {
        // 写路径与 GET 同构：{"contact":{平铺字段}}，实测 PUT 200 且 GET 回读一致（往返见 SystemIT）
        await _svc.UpdateContactInfoAsync(new ContactInfo { ContactPerson = "B", ContactEmail = "b@x", OnlineResource = "http://r", AddressCity = "C" });

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/settings/contact", _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("B", (string?)body["contact"]?["contactPerson"]);
        Assert.Equal("b@x", (string?)body["contact"]?["contactEmail"]);
        Assert.Equal("http://r", (string?)body["contact"]?["onlineResource"]);
        Assert.Equal("C", (string?)body["contact"]?["addressCity"]);
        Assert.Null(body["contact"]?["contactPosition"]); // null 字段省略（读写往返由 ExtensionData 兜底其余键）
    }
}
