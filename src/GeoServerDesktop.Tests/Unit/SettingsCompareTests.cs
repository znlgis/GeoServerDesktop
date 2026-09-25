using System;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Migration;
using GeoServerDesktop.Tests.Infrastructure;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>
/// SettingsCompare / SettingsCompareService 离线单元测试（M4 设置同步）：
/// 域路径与根键、叶子级差异（Changed/Added/Removed）、volatile 键排除、数组整体叶子语义、
/// 选择性应用（合并回目标整包、未勾选键原样保留、删除路径对称）、读写 HTTP 三元断言。
/// </summary>
public class SettingsCompareTests
{
    [Fact]
    public void DomainPaths_AndRootKeys()
    {
        Assert.Equal("/rest/settings.json", SettingsCompare.PathFor(SettingsDomain.Global));
        Assert.Equal("/rest/settings", SettingsCompare.PutPathFor(SettingsDomain.Global));
        Assert.Equal("global", SettingsCompare.RootKeyFor(SettingsDomain.Global));

        Assert.Equal("/rest/services/wms/settings.json", SettingsCompare.PathFor(SettingsDomain.Wms));
        Assert.Equal("wms", SettingsCompare.RootKeyFor(SettingsDomain.Wms));
        Assert.Equal("wfs", SettingsCompare.RootKeyFor(SettingsDomain.Wfs));
        Assert.Equal("wcs", SettingsCompare.RootKeyFor(SettingsDomain.Wcs));
        Assert.Equal("wmts", SettingsCompare.RootKeyFor(SettingsDomain.Wmts));
    }

    [Fact]
    public void Compare_Identical_NoItems()
    {
        var json = "{\"global\":{\"settings\":{\"charset\":\"UTF-8\",\"verbose\":true}}}";
        var diff = SettingsCompare.Compare(SettingsDomain.Global, json, json);
        Assert.True(diff.IsIdentical);
    }

    [Fact]
    public void Compare_DetectsChanged_Added_Removed()
    {
        var source = @"{""global"":{""settings"":{
            ""charset"":""UTF-8"",
            ""contact"":{""addressCity"":""SRC"",""contactEmail"":""a@b""},
            ""onlyInSource"":42}}}";
        var target = @"{""global"":{""settings"":{
            ""charset"":""GBK"",
            ""contact"":{""addressCity"":""TGT""},
            ""onlyInTarget"":{""a"":1}}}}";

        var diff = SettingsCompare.Compare(SettingsDomain.Global, source, target);

        var charset = diff.Items.Single(i => i.Path == "settings.charset");
        Assert.Equal(SettingsDiffKind.Changed, charset.Kind);
        Assert.Equal("\"UTF-8\"", charset.SourceValue);
        Assert.Equal("\"GBK\"", charset.TargetValue);

        var city = diff.Items.Single(i => i.Path == "settings.contact.addressCity");
        Assert.Equal("\"SRC\"", city.SourceValue);
        Assert.Equal("\"TGT\"", city.TargetValue);

        // contact 两侧都存在但 email 仅源侧 → AddedInSource
        var email = diff.Items.Single(i => i.Path == "settings.contact.contactEmail");
        Assert.Equal(SettingsDiffKind.AddedInSource, email.Kind);

        var onlySource = diff.Items.Single(i => i.Path == "settings.onlyInSource");
        Assert.Equal(SettingsDiffKind.AddedInSource, onlySource.Kind);
        Assert.Equal("42", onlySource.SourceValue);
        Assert.Null(onlySource.TargetValue);

        // 目标独有对象展开为叶子级 RemovedInSource
        var onlyTarget = diff.Items.Single(i => i.Path == "settings.onlyInTarget.a");
        Assert.Equal(SettingsDiffKind.RemovedInSource, onlyTarget.Kind);
        Assert.Null(onlyTarget.SourceValue);
        Assert.Equal("1", onlyTarget.TargetValue);

        // 目标独有子对象缺失键（contact 无 email 对应）不重复计
        Assert.DoesNotContain(diff.Items, i => i.Path == "settings.contact" );
    }

    [Fact]
    public void Compare_ExcludesVolatileKeys()
    {
        var source = "{\"global\":{\"updateSequence\":7,\"settings\":{\"id\":\"9\",\"charset\":\"UTF-8\"}}}";
        var target = "{\"global\":{\"updateSequence\":3,\"settings\":{\"id\":\"2\",\"charset\":\"UTF-8\"}}}";

        var diff = SettingsCompare.Compare(SettingsDomain.Global, source, target);

        Assert.True(diff.IsIdentical);
    }

    [Fact]
    public void Compare_ArraysTreatedAsWholeLeaf()
    {
        var source = "{\"wms\":{\"service\":{\"srs\":[\"EPSG:4326\",\"EPSG:3857\"]}}}";
        var target = "{\"wms\":{\"service\":{\"srs\":[\"EPSG:4326\"]}}}";

        var diff = SettingsCompare.Compare(SettingsDomain.Wms, source, target);

        var item = Assert.Single(diff.Items);
        Assert.Equal("service.srs", item.Path);
        Assert.Equal(SettingsDiffKind.Changed, item.Kind);
        Assert.Equal("[\"EPSG:4326\",\"EPSG:3857\"]", item.SourceValue);
    }

    [Fact]
    public void Compare_UnwrappedRoot_FallsBackToDocument()
    {
        // 无根包装的裸对象（防御路径）
        var diff = SettingsCompare.Compare(SettingsDomain.Global, "{\"charset\":\"A\"}", "{\"charset\":\"B\"}");
        var item = Assert.Single(diff.Items);
        Assert.Equal("charset", item.Path);
    }

    [Fact]
    public void Apply_ChangedPath_MergesIntoTargetWholePayload()
    {
        var source = "{\"global\":{\"settings\":{\"charset\":\"UTF-8\"}}}";
        var target = "{\"global\":{\"settings\":{\"charset\":\"GBK\",\"verbose\":true}}}";

        var payload = SettingsCompare.Apply(SettingsDomain.Global, source, target, new[] { "settings.charset" });

        var parsed = JObject.Parse(payload);
        Assert.Equal("UTF-8", (string)parsed["global"]["settings"]["charset"]);
        Assert.Equal(true, (bool?)parsed["global"]["settings"]["verbose"]); // 未勾选键保留
    }

    [Fact]
    public void Apply_AddedAndNestedPaths()
    {
        var source = "{\"global\":{\"settings\":{\"onlyInSource\":42,\"contact\":{\"addressCity\":\"SRC\"}}}}";
        var target = "{\"global\":{\"settings\":{\"contact\":{\"addressCity\":\"TGT\"}}}}";

        var payload = SettingsCompare.Apply(SettingsDomain.Global, source, target,
            new[] { "settings.onlyInSource", "settings.contact.addressCity" });

        var parsed = JObject.Parse(payload)["global"]["settings"];
        Assert.Equal(42, (int?)parsed["onlyInSource"]);
        Assert.Equal("SRC", (string)parsed["contact"]["addressCity"]);
    }

    [Fact]
    public void Apply_RemovedInSource_DeletesLeaf()
    {
        var source = "{\"global\":{\"settings\":{}}}";
        var target = "{\"global\":{\"settings\":{\"onlyInTarget\":{\"a\":1},\"charset\":\"UTF-8\"}}}";

        var payload = SettingsCompare.Apply(SettingsDomain.Global, source, target, new[] { "settings.onlyInTarget.a" });

        var parsed = JObject.Parse(payload)["global"]["settings"];
        Assert.Null(parsed["onlyInTarget"]);
        Assert.Equal("UTF-8", (string)parsed["charset"]);
    }

    [Fact]
    public void Apply_EmptyPaths_TargetUnchangedShape()
    {
        var target = "{\"global\":{\"settings\":{\"charset\":\"GBK\"}}}";
        var payload = SettingsCompare.Apply(SettingsDomain.Global, "{\"global\":{\"settings\":{\"charset\":\"UTF-8\"}}}",
            target, Array.Empty<string>());
        Assert.Equal("GBK", (string)JObject.Parse(payload)["global"]["settings"]["charset"]);
    }

    [Fact]
    public async Task Service_ReadRaw_UsesDomainPath()
    {
        var fake = new RecordingFakeClient();
        var svc = new SettingsCompareService(fake);

        await svc.ReadRawAsync(SettingsDomain.Wms);

        Assert.Equal("GET", fake.Last.Method);
        Assert.Equal("/rest/services/wms/settings.json", fake.Last.Path);
    }

    [Fact]
    public async Task Service_Apply_MergesOntoLiveTargetAndPutsWholePackage()
    {
        var fake = new RecordingFakeClient();
        fake.RespondGet("{\"global\":{\"settings\":{\"charset\":\"GBK\",\"verbose\":true}}}");
        var svc = new SettingsCompareService(fake);

        await svc.ApplyAsync("{\"global\":{\"settings\":{\"charset\":\"UTF-8\"}}}",
            SettingsDomain.Global, new[] { "settings.charset" });

        Assert.Equal("PUT", fake.Last.Method);
        Assert.Equal("/rest/settings", fake.Last.Path);
        var parsed = JObject.Parse(fake.Last.Body)["global"]["settings"];
        Assert.Equal("UTF-8", (string)parsed["charset"]);
        Assert.Equal(true, (bool?)parsed["verbose"]);
    }

    [Fact]
    public async Task CompareAsync_ReadsBothSides()
    {
        var sourceFake = new RecordingFakeClient();
        sourceFake.RespondGet("{\"global\":{\"settings\":{\"charset\":\"UTF-8\"}}}");
        var targetFake = new RecordingFakeClient();
        targetFake.RespondGet("{\"global\":{\"settings\":{\"charset\":\"GBK\"}}}");

        var diff = await SettingsCompareService.CompareAsync(
            new SettingsCompareService(sourceFake), new SettingsCompareService(targetFake), SettingsDomain.Global);

        var item = Assert.Single(diff.Items);
        Assert.Equal("settings.charset", item.Path);
        Assert.Equal("\"UTF-8\"", item.SourceValue);
        Assert.Equal("\"GBK\"", item.TargetValue);
    }

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new SettingsCompareService(null!));
}
