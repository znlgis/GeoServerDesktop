using System;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Import;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>
/// ImportWizardService 离线单元测试：参数模板、PostGIS 连接探测（经 GeoServer 试连 + 清理）、
/// 三类数据源发布编排（幂等、请求体形态与实测基线一致）。
/// </summary>
public class ImportWizardServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly ImportWizardService _svc;

    public ImportWizardServiceTests() => _svc = new ImportWizardService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new ImportWizardService(null!));

    // ---------- 参数模板 ----------

    [Fact]
    public void Templates_Shapefile_UrlRequired()
    {
        var fields = StoreParameterTemplates.ForDataStore(ImportDataSourceKind.ShapefileDirectory);

        var url = fields.Single(f => f.Key == "url");
        Assert.True(url.Required);
        Assert.Contains("file:", url.Example);
        Assert.DoesNotContain(fields, f => f.Key == "host");
    }

    [Fact]
    public void Templates_Postgis_MeasuredKeys()
    {
        var fields = StoreParameterTemplates.ForDataStore(ImportDataSourceKind.Unknown);   // 非 shapefile → PostGIS 模板

        Assert.Contains(fields, f => f.Key == "dbtype" && f.DefaultValue == "postgis");
        foreach (var key in new[] { "host", "port", "database", "user", "passwd" })
            Assert.Contains(fields, f => f.Key == key && f.Required);
    }

    [Fact]
    public void Templates_Coverage_UrlRequired()
    {
        var fields = StoreParameterTemplates.ForCoverageStore();

        Assert.Single(fields);
        Assert.Equal("url", fields[0].Key);
        Assert.True(fields[0].Required);
    }

    [Fact]
    public void BuildShapefileConnectionParameters_UrlAndOptionalNamespace()
    {
        var p1 = StoreParameterTemplates.BuildShapefileConnectionParameters("file:gdtest_data");
        Assert.Single(p1.Entries);
        Assert.Equal("url", p1.Entries[0].Key);

        var p2 = StoreParameterTemplates.BuildShapefileConnectionParameters("file:gdtest_data", "http://ns");
        Assert.Equal(2, p2.Entries.Length);
        Assert.Equal("namespace", p2.Entries[1].Key);
    }

    [Fact]
    public void BuildPostgisConnectionParameters_UsesPasswdKey()
    {
        var p = StoreParameterTemplates.BuildPostgisConnectionParameters(new PostgisConnectionParameters
        {
            Host = "postgis",
            Port = 5433,
            Database = "db",
            User = "u",
            Password = "pw",
        });

        Assert.Equal("postgis", p.Entries.Single(e => e.Key == "host").Value);
        Assert.Equal("5433", p.Entries.Single(e => e.Key == "port").Value);
        Assert.Equal("pw", p.Entries.Single(e => e.Key == "passwd").Value);
        Assert.Equal("postgis", p.Entries.Single(e => e.Key == "dbtype").Value);
    }

    // ---------- PostGIS 探测 ----------

    [Fact]
    public async Task ProbePostgis_Success_CreatesReadsAndCleansUp()
    {
        var r = await _svc.ProbePostgisConnectionAsync("ws1", new PostgisConnectionParameters
        {
            Host = "postgis",
            Database = "postgres",
            User = "postgres",
            Password = "postgres",
        });

        Assert.True(r.Success);
        var post = _fake.Requests.First(x => x.Method == "POST");
        Assert.Equal("/rest/workspaces/ws1/datastores", post.Path);
        var body = Json.P(post.Body);
        Assert.Equal("PostGIS", (string?)body["dataStore"]!["type"]);
        Assert.Equal("postgis", (string?)body["dataStore"]!["connectionParameters"]!["entry"]![0]!["$"]);

        var get = _fake.Requests.First(x => x.Method == "GET");
        Assert.Contains("/featuretypes.json", get.Path);

        var del = _fake.Requests.First(x => x.Method == "DELETE");
        Assert.StartsWith("/rest/workspaces/ws1/datastores/_probe_pg_", del.Path);
    }

    [Fact]
    public async Task ProbePostgis_CreateFails_ReturnsHttpDetail()
    {
        _fake.EnqueueThrow("POST", new GeoServerRequestException("boom", 500, "connection refused"));

        var r = await _svc.ProbePostgisConnectionAsync("ws1", new PostgisConnectionParameters { Host = "h" });

        Assert.False(r.Success);
        Assert.Contains("HTTP 500", r.Message);
        Assert.Contains("connection refused", r.Message);
    }

    [Fact]
    public async Task ProbePostgis_ReadFails_ReturnsFalse()
    {
        _fake.EnqueueThrow("GET", new GeoServerRequestException("read", 500, "db down"));

        var r = await _svc.ProbePostgisConnectionAsync("ws1", new PostgisConnectionParameters { Host = "h" });

        Assert.False(r.Success);
        Assert.Contains("HTTP 500", r.Message);
    }

    // ---------- 发布编排 ----------

    private static ImportSourceRequest ShapefileRequest() => new ImportSourceRequest
    {
        Kind = ImportDataSourceKind.ShapefileDirectory,
        Workspace = "ws1",
        LayerName = "poly",
        FileRef = "file:gdtest_data",
    };

    [Fact]
    public async Task PublishShapefile_CreatesStoreAndFeatureType()
    {
        _fake.RespondGetThrows(new GeoServerRequestException("nf", 404, ""));   // datastore 不存在
        _fake.RespondGetThrows(new GeoServerRequestException("nf", 404, ""));   // featuretype 不存在

        var r = await _svc.PublishShapefileAsync(ShapefileRequest());

        Assert.True(r.Success, r.Message);
        Assert.Equal("poly", r.StoreName);
        Assert.Equal("ws1:poly", r.QualifiedName);

        var posts = _fake.Requests.Where(x => x.Method == "POST").ToList();
        Assert.Equal(2, posts.Count);

        var storeBody = Json.P(posts[0].Body);
        Assert.Equal("Shapefile", (string?)storeBody["dataStore"]!["type"]);
        Assert.Equal("file:gdtest_data", (string?)storeBody["dataStore"]!["connectionParameters"]!["entry"]![0]!["$"]);

        Assert.Equal("/rest/workspaces/ws1/datastores/poly/featuretypes", posts[1].Path);
        var ftBody = Json.P(posts[1].Body);
        Assert.Equal("poly", (string?)ftBody["featureType"]!["name"]);
        Assert.Equal("poly", (string?)ftBody["featureType"]!["nativeName"]);
        Assert.Equal(true, (bool?)ftBody["featureType"]!["enabled"]);
        Assert.Equal("ws1", (string?)ftBody["featureType"]!["namespace"]!["name"]);
    }

    [Fact]
    public async Task PublishShapefile_ExistingStore_SkipsStoreCreate()
    {
        _fake.Enqueue("GET", "{}");                                             // datastore 已存在
        _fake.RespondGetThrows(new GeoServerRequestException("nf", 404, ""));   // featuretype 不存在

        var r = await _svc.PublishShapefileAsync(ShapefileRequest());

        Assert.True(r.Success, r.Message);
        var posts = _fake.Requests.Where(x => x.Method == "POST").ToList();
        Assert.Single(posts);
        Assert.EndsWith("/featuretypes", posts[0].Path);
    }

    [Fact]
    public async Task PublishShapefile_NativeNameSeparatedFromPublishName()
    {
        _fake.RespondGetThrows(new GeoServerRequestException("nf", 404, ""));
        _fake.RespondGetThrows(new GeoServerRequestException("nf", 404, ""));

        var r = await _svc.PublishShapefileAsync(new ImportSourceRequest
        {
            Kind = ImportDataSourceKind.ShapefileDirectory,
            Workspace = "ws1",
            LayerName = "wiz_poly",
            NativeName = "gdtest_poly",
            FileRef = "file:gdtest_data",
        });

        Assert.True(r.Success, r.Message);
        var ftPost = _fake.Requests.Last(x => x.Path.EndsWith("/featuretypes"));
        var ftBody = Json.P(ftPost.Body);
        Assert.Equal("wiz_poly", (string?)ftBody["featureType"]!["name"]);
        Assert.Equal("gdtest_poly", (string?)ftBody["featureType"]!["nativeName"]);
    }

    [Fact]
    public async Task PublishShapefile_ExistsProbeFails_PropagatesError()
    {
        _fake.EnqueueThrow("GET", new GeoServerRequestException("boom", 500, "server error"));   // datastore 探测 5xx

        var r = await _svc.PublishShapefileAsync(ShapefileRequest());

        Assert.False(r.Success);
        Assert.Contains("500", r.Message);
        Assert.DoesNotContain(_fake.Requests, x => x.Method == "POST");   // 不误判为“不存在”而重复创建
    }

    [Fact]
    public async Task PublishShapefile_StoreCreateFails_ReturnsFailure()
    {
        _fake.RespondGetThrows(new GeoServerRequestException("nf", 404, ""));
        _fake.EnqueueThrow("POST", new GeoServerRequestException("bad", 500, "Error looking up file"));

        var r = await _svc.PublishShapefileAsync(ShapefileRequest());

        Assert.False(r.Success);
        Assert.Contains("HTTP 500", r.Message);
    }

    [Fact]
    public async Task PublishGeoTiff_CreatesCoverageStoreAndCoverage()
    {
        _fake.RespondGetThrows(new GeoServerRequestException("nf", 404, ""));
        _fake.RespondGetThrows(new GeoServerRequestException("nf", 404, ""));

        var r = await _svc.PublishGeoTiffAsync(new ImportSourceRequest
        {
            Kind = ImportDataSourceKind.GeoTiffFile,
            Workspace = "ws1",
            LayerName = "dem",
            FileRef = "file:gdtest_data/gdtest_dem.tif",
        });

        Assert.True(r.Success, r.Message);
        var posts = _fake.Requests.Where(x => x.Method == "POST").ToList();
        Assert.Equal(2, posts.Count);
        Assert.Equal("/rest/workspaces/ws1/coveragestores", posts[0].Path);
        var csBody = Json.P(posts[0].Body);
        Assert.Equal("GeoTIFF", (string?)csBody["coverageStore"]!["type"]);
        Assert.Equal("file:gdtest_data/gdtest_dem.tif", (string?)csBody["coverageStore"]!["url"]);
        Assert.Equal("/rest/workspaces/ws1/coveragestores/dem/coverages", posts[1].Path);
    }

    [Fact]
    public async Task PublishGeoTiff_NativeNameSeparatedFromPublishName()
    {
        _fake.RespondGetThrows(new GeoServerRequestException("nf", 404, ""));
        _fake.RespondGetThrows(new GeoServerRequestException("nf", 404, ""));

        var r = await _svc.PublishGeoTiffAsync(new ImportSourceRequest
        {
            Kind = ImportDataSourceKind.GeoTiffFile,
            Workspace = "ws1",
            LayerName = "wiz_dem",
            NativeName = "gdtest_dem",
            FileRef = "file:gdtest_data/gdtest_dem.tif",
        });

        Assert.True(r.Success, r.Message);
        var covPost = _fake.Requests.Last(x => x.Path.EndsWith("/coverages"));
        var covBody = Json.P(covPost.Body);
        Assert.Equal("wiz_dem", (string?)covBody["coverage"]!["name"]);
        Assert.Equal("gdtest_dem", (string?)covBody["coverage"]!["nativeName"]);
    }

    [Fact]
    public async Task PublishPostgis_MissingConnectionParameters_Fails()
    {
        var r = await _svc.PublishPostgisAsync(new ImportSourceRequest
        {
            Kind = ImportDataSourceKind.Unknown,
            Workspace = "ws1",
            LayerName = "tbl",
        });

        Assert.False(r.Success);
        Assert.Contains("PostGIS", r.Message);
    }

    [Fact]
    public async Task PublishAsync_DispatchesByKind()
    {
        _fake.RespondGetThrows(new GeoServerRequestException("nf", 404, ""));
        _fake.RespondGetThrows(new GeoServerRequestException("nf", 404, ""));

        var r = await _svc.PublishAsync(new ImportSourceRequest
        {
            Kind = ImportDataSourceKind.GeoTiffFile,
            Workspace = "ws1",
            LayerName = "dem2",
            FileRef = "file:x.tif",
        });

        Assert.True(r.Success, r.Message);
        Assert.Contains(_fake.Requests, x => x.Path == "/rest/workspaces/ws1/coveragestores");
    }

    // ---------- ImporterService 扩展探测 ----------

    [Fact]
    public async Task ImporterProbe_404_NotInstalled()
    {
        var importer = new ImporterService(_fake);
        _fake.RespondGetThrows(new GeoServerRequestException("nf", 404, ""));

        var a = await importer.ProbeAvailabilityAsync();

        Assert.Equal(ImporterAvailabilityState.NotInstalled, a.State);
        Assert.False(a.IsAvailable);
        Assert.Contains("内置发布向导", a.Message);
        Assert.Equal("/rest/imports.json", _fake.Last!.Path);
    }

    [Fact]
    public async Task ImporterProbe_Ok_Available()
    {
        var importer = new ImporterService(_fake);
        _fake.RespondGet("""{"imports":[]}""");

        var a = await importer.ProbeAvailabilityAsync();

        Assert.Equal(ImporterAvailabilityState.Available, a.State);
        Assert.True(a.IsAvailable);
    }

    [Fact]
    public async Task ImporterProbe_OtherError_Unknown()
    {
        var importer = new ImporterService(_fake);
        _fake.RespondGetThrows(new GeoServerRequestException("err", 500, "boom"));

        var a = await importer.ProbeAvailabilityAsync();

        Assert.Equal(ImporterAvailabilityState.Unknown, a.State);
        Assert.Contains("HTTP 500", a.Message);
    }
}
