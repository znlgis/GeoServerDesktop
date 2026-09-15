using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>ImporterService 离线单元测试。FIXED-E14：请求体统一复用 GeoServerJson.Request
/// （NullValueHandling.Ignore），targetStore 缺省不再发送 "targetStore":null。
/// 注：3.0.1 默认镜像未装 importer 扩展（集成层 404 基线不变）。无 Run/Commit 方法。</summary>
public class ImporterServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly ImporterService _svc;

    public ImporterServiceTests() => _svc = new ImporterService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new ImporterService(null!));

    [Fact]
    public async Task CreateImportAsync_NullTargetStoreOmitted_FIXED_E14()
    {
        // E14 修复：null 字段不再进入请求体（此前 GeoServer 可能因显式 null 拒绝）
        _fake.RespondPost("""{"import":{"id":7,"state":"PENDING","targetWorkspace":{"name":"topp"},"targetStore":null,"tasks":[]}}""");

        var w = await _svc.CreateImportAsync("topp");

        Assert.Equal("POST", _fake.Last!.Method);
        Assert.Equal("/rest/imports", _fake.Last.Path);
        var body = Json.P(_fake.Last.Body);
        Assert.Equal("topp", (string?)body["import"]?["targetWorkspace"]?["name"]);
        Assert.Null(body["import"]!["targetStore"]); // 键整体缺省（而非 null 值）

        // 响应解析（ImportContextWrapper）不受影响
        Assert.Equal(7, w.Import.Id);
        Assert.Equal("PENDING", w.Import.State);
        Assert.Equal("topp", w.Import.TargetWorkspace.Name);
        Assert.Null(w.Import.TargetStore);
    }

    [Fact]
    public async Task CreateImportAsync_WithTargetStorePostsWrapped()
    {
        _fake.RespondPost("""{"import":{"id":1,"state":"NEW"}}""");
        await _svc.CreateImportAsync("topp", "shapes");

        var body = Json.P(_fake.Last!.Body);
        Assert.Equal("shapes", (string?)body["import"]?["targetStore"]?["name"]);
    }

    [Fact]
    public async Task GetImportAsync_ParsesTasksAndTarget()
    {
        _fake.RespondGet("""{"import":{"id":2,"state":"READY","targetWorkspace":{"name":"ws"},"targetStore":{"@class":"dataStore","name":"st"},"tasks":[{"id":0,"state":"NEW","data":{"type":"data","format":"shapezip","location":"http://l"},"target":{"name":"layer"},"progress":""}]}}""");

        var w = await _svc.GetImportAsync(2);

        Assert.Equal("GET", _fake.Last!.Method);
        Assert.Equal("/rest/imports/2.json", _fake.Last.Path);
        Assert.Equal("READY", w.Import.State);
        Assert.Equal("shapezip", w.Import.Tasks[0].Data.Format);
        Assert.Equal("layer", w.Import.Tasks[0].Target.Name);
    }

    [Fact]
    public async Task DeleteImportAsync_DeletesNoSuffix()
    {
        await _svc.DeleteImportAsync(9);

        Assert.Equal("DELETE", _fake.Last!.Method);
        Assert.Equal("/rest/imports/9", _fake.Last.Path);
    }

    [Fact]
    public async Task GetImportTasksAsync_ReturnsRawString_KNOWN_ISSUE_E14()
    {
        // KNOWN-ISSUE E14 另一半（任务列表只回原始字符串）维持基线：扩展未装、无真实样例可固化解析器。
        _fake.RespondGet("""{"tasks":{"task":[{"id":0}]}}""");
        var raw = await _svc.GetImportTasksAsync(3);

        Assert.Equal("/rest/imports/3/tasks.json", _fake.Last!.Path);
        Assert.Equal("""{"tasks":{"task":[{"id":0}]}}""", raw);
    }

    [Fact]
    public async Task UploadDataAsync_PutsRawBytesUnderTaskDataEndpoint()
    {
        var zip = new byte[] { 80, 75, 3, 4 };
        await _svc.UploadDataAsync(3, 0, zip, "application/zip");

        Assert.Equal("PUT", _fake.Last!.Method);
        Assert.Equal("/rest/imports/3/tasks/0/data", _fake.Last.Path);
        Assert.Equal("application/zip", _fake.Last.ContentType);
        Assert.Equal(zip, _fake.Last.RawBody);
    }

    [Fact]
    public void NoRunOrCommitMethods_CurrentSurfacePin()
    {
        // 清单：ImporterService 无 Run/Commit 方法（缺功能，导入无法经客户端提交执行）
        Assert.Null(typeof(ImporterService).GetMethod("RunImportAsync"));
        Assert.Null(typeof(ImporterService).GetMethod("CommitImportAsync"));
    }
}
