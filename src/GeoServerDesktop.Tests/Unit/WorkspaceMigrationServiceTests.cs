using System;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Migration;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>
/// WorkspaceMigrationService 离线单元测试（M4）：导出全量资源遍历的 URL 序列与清单内容、
/// SLD 入包、单项失败降级为警告；导入按依赖顺序回放（ws → namespace → store → style →
/// featureType → 图层绑定 → layerGroup）、引用重写（ws/namespace/store 限定名、连接参数
/// namespace URI）、服务端字段剥离（href/dateCreated/_default/Id）、幂等跳过、归档校验。
/// </summary>
public class WorkspaceMigrationServiceTests
{
    private const string PolySld = "<sld><polygon red/></sld>";
    private const string BlueSld = "<sld><polygon blue/></sld>";
    private const string GreenSld = "<sld><polygon green/></sld>";

    private static PathFakeClient ExportFake()
    {
        return new PathFakeClient()
            .RespondGet("/rest/about/version.json", "{\"version\":{\"release\":\"3.0.1\"}}")
            .RespondGet("/rest/namespaces/m4src.json", "{\"namespace\":{\"prefix\":\"m4src\",\"uri\":\"http://m4src\",\"isolated\":false}}")
            .RespondGet("/rest/workspaces/m4src/datastores.json", "{\"dataStores\":{\"dataStore\":[{\"name\":\"ds1\"}]}}")
            .RespondGet("/rest/workspaces/m4src/datastores/ds1.json",
                "{\"dataStore\":{\"name\":\"ds1\",\"type\":\"Shapefile\",\"enabled\":true," +
                "\"workspace\":{\"name\":\"m4src\"}," +
                "\"connectionParameters\":{\"entry\":[{\"@key\":\"url\",\"$\":\"file:gdtest_data\"},{\"@key\":\"namespace\",\"$\":\"http://m4src\"}]}," +
                "\"_default\":false,\"dateCreated\":\"2026-09-25 03:44:24.488 UTC\",\"href\":\"http://h/ds1.json\"}}")
            .RespondGet("/rest/workspaces/m4src/datastores/ds1/featuretypes.json", "{\"featureTypes\":{\"featureType\":[{\"name\":\"roads\"}]}}")
            .RespondGet("/rest/workspaces/m4src/datastores/ds1/featuretypes/roads.json",
                "{\"featureType\":{\"name\":\"roads\",\"nativeName\":\"roads_shp\",\"srs\":\"EPSG:4326\"," +
                "\"store\":{\"name\":\"m4src:ds1\"},\"namespace\":{\"name\":\"m4src\"}," +
                "\"title\":\"Roads\",\"href\":\"http://h/ft.json\"}}")
            .RespondGet("/rest/workspaces/m4src/coveragestores.json", "{\"coverageStores\":{\"coverageStore\":[]}}")
            .RespondGet("/rest/workspaces/m4src/styles.json", "{\"styles\":{\"style\":[{\"name\":\"local\"}]}}")
            .RespondGet("/rest/workspaces/m4src/styles/local.sld", PolySld)
            .NotFoundGet("/rest/styles/local.json")
            .RespondGet("/rest/workspaces/m4src/layers.json", "{\"layers\":{\"layer\":[{\"name\":\"roads\"},{\"name\":\"bad\"}]}}")
            .RespondGet("/rest/workspaces/m4src/layers/roads.json",
                "{\"layer\":{\"name\":\"roads\",\"type\":\"VECTOR\"," +
                "\"defaultStyle\":{\"name\":\"blue\",\"href\":\"http://h/rest/styles/blue.json\"}," +
                "\"resource\":{\"@class\":\"featureType\",\"name\":\"m4src:roads\"}}}")
            .ThrowGet("/rest/workspaces/m4src/layers/bad.json", new InvalidOperationException("boom"))
            .RespondGet("/rest/styles/blue.sld", BlueSld)
            .NotFoundGet("/rest/styles/blue.json")
            .RespondGet("/rest/workspaces/m4src/layergroups.json", "{\"layerGroups\":{\"layerGroup\":[]}}");
    }

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new WorkspaceMigrationService(null!));

    [Fact]
    public async Task Export_TraversesWorkspaceResources_AndCapturesSldsWithBindings()
    {
        var fake = ExportFake();
        var svc = new WorkspaceMigrationService(fake);

        var result = await svc.ExportWorkspaceAsync("m4src");

        // 清单摘要
        Assert.Equal("m4src", result.Workspace);
        Assert.Equal("m4src", result.Manifest.NamespacePrefix);
        Assert.Equal("http://m4src", result.Manifest.NamespaceUri);
        Assert.Equal("3.0.1", result.Manifest.SourceVersion);

        var ds = Assert.Single(result.Manifest.DataStores);
        Assert.Equal("ds1", ds.Name);
        Assert.Equal("Shapefile", ds.Type);

        var ft = Assert.Single(result.Manifest.FeatureTypes);
        Assert.Equal("roads", ft.Name);
        Assert.Equal("ds1", ft.Store);
        Assert.Equal("roads_shp", ft.NativeName);

        // 样式：工作空间 local + 被引用全局 blue（SLD 入包）；green 未被引用不出现
        Assert.Equal(2, result.Manifest.Styles.Count);
        var local = result.Manifest.Styles.Single(s => !s.Global);
        Assert.Equal("styles/m4src_local.sld", local.SldPath);
        var blue = result.Manifest.Styles.Single(s => s.Global);
        Assert.Equal("styles/blue.sld", blue.SldPath);

        // 图层绑定：roads→blue（全局）；bad 详情失败仅记警告
        var binding = Assert.Single(result.Manifest.LayerBindings);
        Assert.Equal("m4src:roads", binding.QualifiedLayer);
        Assert.Equal("blue", binding.Style);
        Assert.False(binding.WorkspaceStyle);
        Assert.Contains(result.Manifest.Warnings, w => w.Contains("layer bad"));

        // 归档 ZIP 成员
        using var zip = new ZipArchive(new System.IO.MemoryStream(result.Archive), ZipArchiveMode.Read);
        var names = zip.Entries.Select(e => e.FullName).OrderBy(n => n).ToArray();
        Assert.Equal(
            new[] { "manifest.json", "styles/blue.sld", "styles/m4src_local.sld" },
            names);
        Assert.Equal(PolySld, WorkspaceMigrationService.ReadArchiveMemberText(result.Archive, "styles/m4src_local.sld"));

        // 导出 GET 序列（命名空间 → 存储 → 资源 → 样式 → 图层 → 图层组）
        Assert.Equal("/rest/namespaces/m4src.json", fake.Requests[1].Path);
        Assert.Equal("/rest/workspaces/m4src/datastores.json", fake.Requests[2].Path);
        Assert.Equal("/rest/workspaces/m4src/datastores/ds1/featuretypes.json", fake.Requests[4].Path);
        Assert.Equal("/rest/workspaces/m4src/styles.json", fake.Requests[7].Path);
        Assert.Equal("/rest/workspaces/m4src/styles/local.sld", fake.Requests[8].Path);
        Assert.Equal("/rest/styles/local.json", fake.Requests[9].Path); // 同名全局样式存在性探测
    }

    [Fact]
    public async Task Export_ThenReadManifest_RoundTrips()
    {
        var svc = new WorkspaceMigrationService(ExportFake());
        var result = await svc.ExportWorkspaceAsync("m4src");

        var manifest = WorkspaceMigrationService.ReadManifest(result.Archive);
        Assert.Equal(WorkspaceManifest.CurrentSchemaVersion, manifest.SchemaVersion);
        Assert.Single(manifest.DataStores);
        Assert.Equal("roads", manifest.FeatureTypes[0].Name);
        Assert.NotNull(manifest.FeatureTypes[0].Raw);
    }

    [Fact]
    public async Task Import_FreshTarget_ReplaysInDependencyOrder_WithRewrite()
    {
        var svcE = new WorkspaceMigrationService(ExportFake());
        var export = await svcE.ExportWorkspaceAsync("m4src");

        var fake = new PathFakeClient();
        fake.SetDefault("GET", "{}"); // 目标实例一切资源不存在 → 404
        foreach (var p in new[]
        {
            "/rest/workspaces/m4tgt.json",
            "/rest/namespaces/m4tgt.json",
            "/rest/workspaces/m4tgt/datastores/ds1.json",
            "/rest/workspaces/m4tgt/datastores/ds1/featuretypes/roads.json",
            "/rest/styles/blue.json",
            "/rest/workspaces/m4tgt/styles/local.json",
            "/rest/workspaces/m4tgt/layergroups/m4src.json",
        })
        {
            fake.NotFoundGet(p);
        }

        var svc = new WorkspaceMigrationService(fake);
        var result = await svc.ImportWorkspaceAsync(new WorkspaceImportRequest
        {
            Archive = export.Archive,
            TargetWorkspace = "m4tgt",
        });

        Assert.True(result.Success, result.FailureSummary);

        // 步骤顺序：workspace → namespace → dataStore → style → featureType → layerBinding
        var steps = result.Items.Select(i => i.Step).ToArray();
        Assert.Equal(
            new[] { "workspace", "namespace", "dataStore", "style", "style", "featureType", "layerBinding" },
            steps);
        Assert.All(result.Items, i => Assert.Equal(MigrationItemStatus.Created, i.Status));

        var posts = fake.All("POST");
        // 1) 工作空间 2) 命名空间（占位 URI 不匹配目标 URI → 显式创建）3) 存储
        // 4) 工作空间样式 local 5) 全局样式 blue 6) featureType
        Assert.Equal("/rest/workspaces", posts[0].Path);
        Assert.Contains("\"name\":\"m4tgt\"", posts[0].Body);
        Assert.Equal("/rest/namespaces", posts[1].Path);
        Assert.Equal("/rest/workspaces/m4tgt/datastores", posts[2].Path);

        // 存储载荷：引用重写 + 服务端字段剥离
        Assert.Contains("\"workspace\":{\"name\":\"m4tgt\"}", posts[2].Body);
        Assert.Contains("\"$\":\"http://m4tgt\"", posts[2].Body); // namespace 连接参数 URI 重写
        Assert.Contains("file:gdtest_data", posts[2].Body);
        Assert.DoesNotContain("href", posts[2].Body);
        Assert.DoesNotContain("dateCreated", posts[2].Body);
        Assert.DoesNotContain("_default", posts[2].Body);

        Assert.Equal("/rest/workspaces/m4tgt/styles", posts[3].Path);
        Assert.Contains("\"filename\":\"local.sld\"", posts[3].Body);
        Assert.Equal("/rest/styles", posts[4].Path);
        Assert.Contains("\"filename\":\"blue.sld\"", posts[4].Body);
        Assert.Equal("/rest/workspaces/m4tgt/datastores/ds1/featuretypes", posts[5].Path);

        // featureType：剥离 namespace/store/Id/href，保留 nativeName
        Assert.DoesNotContain("\"namespace\"", posts[5].Body);
        Assert.DoesNotContain("\"store\"", posts[5].Body);
        Assert.Contains("\"nativeName\":\"roads_shp\"", posts[5].Body);

        // SLD 内容 PUT（两步样式）
        var puts = fake.All("PUT");
        var sldPut = puts.Single(p => p.Path == "/rest/styles/blue");
        Assert.Equal(BlueSld, sldPut.Body);
        Assert.StartsWith("application/vnd.ogc.sld+xml", sldPut.ContentType);

        // 图层绑定：qualified 名重写为目标工作空间
        var bind = puts.Single(p => p.Path == "/rest/layers/m4tgt%3Aroads");
        Assert.Contains("\"name\":\"m4tgt:roads\"", bind.Body);
        Assert.Contains("\"defaultStyle\":{\"name\":\"blue\"}", bind.Body);
    }

    [Fact]
    public async Task Import_ExistingStore_SkippedWhenNoOverwrite()
    {
        var svcE = new WorkspaceMigrationService(ExportFake());
        var export = await svcE.ExportWorkspaceAsync("m4src");

        var fake = new PathFakeClient();
        fake.SetDefault("GET", "{}");
        foreach (var p in new[]
        {
            "/rest/workspaces/m4tgt.json",
            "/rest/namespaces/m4tgt.json",
            "/rest/styles/blue.json",
            "/rest/workspaces/m4tgt/styles/local.json",
            "/rest/workspaces/m4tgt/datastores/ds1/featuretypes/roads.json",
            "/rest/workspaces/m4tgt/layergroups/m4src.json",
        })
        {
            fake.NotFoundGet(p);
        }
        fake.RespondGet("/rest/workspaces/m4tgt/datastores/ds1.json", "{\"dataStore\":{\"name\":\"ds1\"}}");

        var svc = new WorkspaceMigrationService(fake);
        var result = await svc.ImportWorkspaceAsync(new WorkspaceImportRequest
        {
            Archive = export.Archive,
            TargetWorkspace = "m4tgt",
        });

        var store = result.Items.Single(i => i.Step == "dataStore");
        Assert.Equal(MigrationItemStatus.Skipped, store.Status);
        Assert.DoesNotContain(fake.All("POST"), p => p.Path == "/rest/workspaces/m4tgt/datastores");
        Assert.DoesNotContain(fake.All("PUT"), p => p.Path == "/rest/workspaces/m4tgt/datastores/ds1");
    }

    [Fact]
    public async Task Import_SameWorkspaceName_KeepsNamespaceUri()
    {
        var svcE = new WorkspaceMigrationService(ExportFake());
        var export = await svcE.ExportWorkspaceAsync("m4src");

        var fake = new PathFakeClient();
        fake.SetDefault("GET", "{}");
        foreach (var p in new[]
        {
            "/rest/workspaces/m4src.json",
            "/rest/namespaces/m4src.json",
            "/rest/workspaces/m4src/datastores/ds1.json",
            "/rest/workspaces/m4src/datastores/ds1/featuretypes/roads.json",
            "/rest/styles/blue.json",
            "/rest/workspaces/m4src/styles/local.json",
            "/rest/workspaces/m4src/layergroups/m4src.json",
        })
        {
            fake.NotFoundGet(p);
        }

        var svc = new WorkspaceMigrationService(fake);
        var result = await svc.ImportWorkspaceAsync(new WorkspaceImportRequest { Archive = export.Archive });

        Assert.True(result.Success, result.FailureSummary);
        var post = fake.All("POST").Single(p => p.Path == "/rest/workspaces/m4src/datastores");
        Assert.Contains("http://m4src", post.Body); // 源 URI 保留
    }

    [Fact]
    public async Task Import_LayerGroupReferences_Rewritten()
    {
        var manifest = new WorkspaceManifest
        {
            Workspace = "m4src",
            NamespacePrefix = "m4src",
            NamespaceUri = "http://m4src",
        };
        manifest.LayerGroups.Add(new LayerGroupEntry
        {
            Name = "grp",
            Raw = Newtonsoft.Json.Linq.JObject.Parse(
                "{\"name\":\"m4src\",\"title\":\"m4src\"," +
                "\"publishStep\":{\"type\":\"namedLayers\",\"namedLayers\":[{\"name\":{\"name\":\"m4src:roads\"}}]}," +
                "\"layerDependencies\":[{\"layers\":[{\"name\":\"m4src:roads\"}]}]}"),
        });
        var archive = BuildArchiveFrom(manifest);

        var fake = new PathFakeClient();
        fake.SetDefault("GET", "{}");
        fake.NotFoundGet("/rest/workspaces/m4tgt.json");
        fake.NotFoundGet("/rest/namespaces/m4tgt.json");
        fake.NotFoundGet("/rest/workspaces/m4tgt/layergroups/grp.json");

        var svc = new WorkspaceMigrationService(fake);
        var result = await svc.ImportWorkspaceAsync(new WorkspaceImportRequest
        {
            Archive = archive,
            TargetWorkspace = "m4tgt",
        });

        Assert.True(result.Success, result.FailureSummary);
        var post = fake.All("POST").Single(p => p.Path == "/rest/workspaces/m4tgt/layergroups");
        Assert.Contains("m4tgt:roads", post.Body);   // 限定名引用重写
        Assert.DoesNotContain("m4src:roads", post.Body);
    }

    [Fact]
    public void ReadManifest_InvalidArchive_Throws()
    {
        Assert.Throws<ArgumentException>(() => WorkspaceMigrationService.ReadManifest(null!));
        Assert.Throws<InvalidDataException>(() => WorkspaceMigrationService.ReadManifest(new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public async Task Import_TooNewSchema_Fails()
    {
        var manifest = new WorkspaceManifest { Workspace = "m4src", SchemaVersion = 99 };
        var svc = new WorkspaceMigrationService(new PathFakeClient());

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            svc.ImportWorkspaceAsync(new WorkspaceImportRequest { Archive = BuildArchiveFrom(manifest) }));
    }

    [Fact]
    public async Task Import_EmptyArchive_Throws()
    {
        var svc = new WorkspaceMigrationService(new PathFakeClient());
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.ImportWorkspaceAsync(new WorkspaceImportRequest()));
    }

    /// <summary>测试专用归档构造（与库内 BuildArchive 同格式：manifest.json 单成员）。</summary>
    private static byte[] BuildArchiveFrom(WorkspaceManifest manifest)
    {
        using var buffer = new System.IO.MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, true))
        {
            var entry = zip.CreateEntry("manifest.json");
            using var writer = new System.IO.StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(Newtonsoft.Json.JsonConvert.SerializeObject(manifest));
        }
        return buffer.ToArray();
    }
}
