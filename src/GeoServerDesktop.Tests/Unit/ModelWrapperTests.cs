using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>
/// 模型层（清单 B 节）双层 JSON 反序列化基线测试：每个 wrapper 各一例，
/// 并固化关键形态细节（entry/@key/$、@class、published、seedRequest 根、"abstrct" 拼写、
/// FeatureType.Enabled 非空 bool 等）。
/// </summary>
public class ModelWrapperTests
{
    private static T D<T>(string json) => JsonConvert.DeserializeObject<T>(json)!;

    // ---------- 资源列表 / 单体包装 ----------

    [Fact]
    public void WorkspaceListWrapper_DoubleLayer()
    {
        var w = D<WorkspaceListWrapper>("""{"workspaces":{"workspace":[{"name":"a","isolated":false,"href":"h"}]}}""");
        Assert.Equal("a", w.WorkspaceList.Workspaces[0].Name);
        Assert.False(w.WorkspaceList.Workspaces[0].Isolated);
    }

    [Fact]
    public void NamespaceListWrapper_DoubleLayer()
    {
        var w = D<NamespaceListWrapper>("""{"namespaces":{"namespace":[{"prefix":"p","uri":"u"}]}}""");
        Assert.Equal("p", w.NamespaceList.Namespaces[0].Prefix);
    }

    [Fact]
    public void DataStoreListWrapper_DoubleLayer()
    {
        var w = D<DataStoreListWrapper>("""{"dataStores":{"dataStore":[{"name":"d","type":"PostGIS","enabled":true}]}}""");
        Assert.Equal("PostGIS", w.DataStoreList.DataStores[0].Type);
    }

    [Fact]
    public void FeatureTypeListWrapper_DoubleLayer()
    {
        var w = D<FeatureTypeListWrapper>("""{"featureTypes":{"featureType":[{"name":"f","srs":"EPSG:4326","enabled":true}]}}""");
        Assert.True(w.FeatureTypeList.FeatureTypes[0].Enabled);
    }

    [Fact]
    public void CoverageStoreListWrapper_DoubleLayer()
    {
        var w = D<CoverageStoreListWrapper>("""{"coverageStores":{"coverageStore":[{"name":"cs","url":"file:x"}]}}""");
        Assert.Equal("file:x", w.CoverageStoreList.CoverageStores[0].Url);
    }

    [Fact]
    public void CoverageListWrapper_DoubleLayer()
    {
        var w = D<CoverageListWrapper>("""{"coverages":{"coverage":[{"name":"c","abstract":"ab"}]}}""");
        Assert.Equal("ab", w.CoverageList.Coverages[0].Abstract); // Coverage 用 "abstract" 键（三处不一致之一，E28 括注）
    }

    [Fact]
    public void LayerListWrapper_DoubleLayer()
    {
        var w = D<LayerListWrapper>("""{"layers":{"layer":[{"name":"l"}]}}""");
        Assert.Single(w.LayerList.Layers);
    }

    [Fact]
    public void LayerGroupListWrapper_DoubleLayer()
    {
        var w = D<LayerGroupListWrapper>("""{"layerGroups":{"layerGroup":[{"name":"g","abstractTxt":"x"}]}}""");
        Assert.Equal("x", w.LayerGroupList.LayerGroups[0].Abstract); // LayerGroup 用 "abstractTxt"（与 GeoServer LayerInfo 一致）
    }

    [Fact]
    public void StyleListWrapper_DoubleLayer()
    {
        var w = D<StyleListWrapper>("""{"styles":{"style":[{"name":"s","filename":"s.sld","languageVersion":{"version":"1.0"}}]}}""");
        Assert.Equal("1.0", w.StyleList.Styles[0].LanguageVersion.Version);
    }

    [Fact]
    public void WMSStoreListWrapper_DoubleLayer()
    {
        var w = D<WMSStoreListWrapper>("""{"wmsStores":{"wmsStore":[{"name":"w","capabilitiesURL":"u"}]}}""");
        Assert.Equal("u", w.WMSStoreList.WMSStores[0].CapabilitiesURL);
    }

    [Fact]
    public void WMSLayerListWrapper_DoubleLayer()
    {
        var w = D<WMSLayerListWrapper>("""{"wmsLayers":{"wmsLayer":[{"name":"wl"}]}}""");
        Assert.Single(w.WMSLayerList.WMSLayers);
    }

    [Fact]
    public void WMTSStoreListWrapper_DoubleLayer_AndE30PropertyNaming()
    {
        // E30：JSON 键 "capabilitiesURL" 映射到 WMTSStore.CapabilitiesUrl（小写 rl），与 WMSStore.CapabilitiesURL 属性名不一致
        var w = D<WMTSStoreListWrapper>("""{"wmtsStores":{"wmtsStore":[{"name":"wt","capabilitiesURL":"url"}]}}""");
        Assert.Equal("url", w.WMTSStoreList.WMTSStores[0].CapabilitiesUrl);
    }

    [Fact]
    public void WMTSLayerListWrapper_DoubleLayer()
    {
        var w = D<WMTSLayerListWrapper>("""{"wmtsLayers":{"wmtsLayer":[{"name":"wtl","advertised":false}]}}""");
        Assert.False(w.WMTSLayerList.WMTSLayers[0].Advertised);
    }

    // ---------- About / System ----------

    [Fact]
    public void VersionInfoWrapper_AboutResourceWithUppercaseVersionKey()
    {
        // ResourceInfo：@name / "Version"(大写V) / "Build-Timestamp" / "Git-Revision"
        var w = D<VersionInfoWrapper>("""{"about":{"resource":[{"@name":"GeoServer","Version":"2.24.0","Build-Timestamp":"T","Git-Revision":"R"}]}}""");
        Assert.Equal("GeoServer", w.About.Resources[0].Name);
        Assert.Equal("2.24.0", w.About.Resources[0].Version);
        Assert.Equal("T", w.About.Resources[0].BuildTimestamp);
        Assert.Equal("R", w.About.Resources[0].GitRevision);
    }

    [Fact]
    public void ManifestsWrapper_AboutResourceLowercaseKeys()
    {
        var w = D<ManifestsWrapper>("""{"about":{"resource":[{"name":"M","version":"1"}]}}""");
        Assert.Equal("M", w.About.Resources[0].Name);
    }

    [Fact]
    public void SystemStatusWrapper_MetricsMetricWithObjectValue()
    {
        var w = D<SystemStatusWrapper>("""{"metrics":{"metric":[{"available":true,"description":"d","name":"n","unit":"bytes","category":"c","identifier":"i","priority":2,"value":{"x":1}}]}}""");
        var m = w.Metrics.MetricArray[0];
        Assert.Equal("n", m.Name);
        Assert.Equal(2, m.Priority);
        Assert.NotNull(m.Value); // object 承接嵌套对象
    }

    // ---------- Importer / SC / Monitor ----------

    [Fact]
    public void ImportContextWrapper_ImportRootWithTasks()
    {
        var w = D<ImportContextWrapper>("""{"import":{"id":3,"state":"PUBLISHING","targetWorkspace":{"name":"ws"},"targetStore":{"@class":"dataStore","name":"st"},"tasks":[{"id":1,"state":"ERROR","data":{"type":"data","format":"shapezip"},"target":{"name":"t"},"progress":"50%"}]}}""");
        Assert.Equal(3, w.Import.Id);
        Assert.Equal("dataStore", w.Import.TargetStore.Class);
        Assert.Equal("50%", w.Import.Tasks[0].Progress);
    }

    [Fact]
    public void GranuleListWrapper_FeatureCollectionShape()
    {
        var w = D<GranuleListWrapper>("""{"features":[{"id":"1","fid":"granule.1","properties":{"k":"v"}}],"totalFeatures":7}""");
        Assert.Single(w.Granules);
        Assert.Equal(7, w.TotalFeatures);
    }

    [Fact]
    public void MonitorRequestListWrapper_DynamicClassRoot_FIXED_E7()
    {
        // FIXED-E7：3.0.1 实测 {"org.geoserver.monitor.RequestDatas":{"org.geoserver.monitor.RequestData":[{name,href}]}}
        var w = MonitorRequestListWrapper.Parse("""{"org.geoserver.monitor.RequestDatas":{"org.geoserver.monitor.RequestData":[{"name":42,"href":"http://x/42.json"}]}}""");
        Assert.Single(w.Requests);
        Assert.Equal(42, w.Requests[0].Id);
        Assert.Equal("http://x/42.json", w.Requests[0].Href);
        Assert.Empty(MonitorRequestListWrapper.Parse("").Requests);
    }

    // ---------- 设置类自根包装 ----------

    [Fact]
    public void LoggingSettings_SelfRootedLogging()
    {
        var s = D<LoggingSettings>("""{"logging":{"level":"L","location":"loc","stdOutLogging":true,"fileLogging":false}}""");
        Assert.Equal("L", s.Logging.Level);
        Assert.True(s.Logging.StdOutLogging);
    }

    [Fact]
    public void GlobalSettings_RealGlobalRootWithPassthrough_FIXED()
    {
        // 实测 3.0.1 GET /rest/settings.json 根为 {"global":{"settings":{...}}}；
        // GlobalSettings.Settings 为透传访问器（原根 "settings" 形态与真实不符，Settings 恒 null 的
        // 基线随 E17 族修复翻转）。global 层的 globalServices 等键经 ExtensionData 捕获。
        var s = D<GlobalSettings>("""{"global":{"settings":{"charset":"UTF-8","verboseExceptions":false,"maxFeatures":50},"globalServices":true,"updateSequence":7}}""");
        Assert.Equal("UTF-8", s.Settings.Charset);
        Assert.False(s.Settings.VerboseExceptions);
        Assert.Equal(50, s.Settings.MaxFeatures);
        Assert.True((bool)s.Global.ExtensionData["globalServices"]);
        Assert.Equal(7, (int)s.Global.ExtensionData["updateSequence"]);

        // 便捷访问器写入自动建 global 包装
        var w = new GlobalSettings { Settings = new Settings { Charset = "UTF-8" } };
        Assert.NotNull(w.Global);
        Assert.Same(w.Global.Settings, w.Settings);
    }

    [Fact]
    public void ContactInfoWrapper_ContactRoot_CurrentModelShape()
    {
        // 模型单层平铺即真实形态（FIXED-E17 复核：3.0.1 GET 无双层嵌套，address* 为普通字段名）
        var w = D<ContactInfoWrapper>("""{"contact":{"contactPerson":"P","contactOrganization":"O","contactPosition":"Q","addressType":"A","address":"st","addressCity":"c","addressState":"s","addressPostalCode":"100000","addressCountry":"CN","contactVoice":"1","contactFacsimile":"2","contactEmail":"e@x"}}""");
        Assert.Equal("P", w.Contact.ContactPerson);
        Assert.Equal("CN", w.Contact.AddressCountry);
    }

    [Fact]
    public void WmsSettings_SelfRootedWmsWithAbstrctSpelling_KNOWN_ISSUE_E28_PIN()
    {
        // "abstrct"（缺 a）是 GeoServer 官方 ServiceDTO 的已知拼写错误，本库有意对齐——测试固化按源码现状：
        // 只有 "abstrct" 键能填充 Abstract；正确拼写 "abstract" 反而被忽略。
        var s = D<WMSSettings>("""{"wms":{"enabled":true,"abstrct":"legacy-wms","citeCompliant":false,"verbose":true}}""");
        Assert.Equal("legacy-wms", s.WMS.Abstract);

        var wrong = D<WMSSettings>("""{"wms":{"abstract":"proper-spelling"}}""");
        Assert.Null(wrong.WMS.Abstract); // 正确拼写不映射（当前行为）
    }

    [Fact]
    public void ServiceSettings_RootsForEachOgcService()
    {
        Assert.Equal("w", D<WMSSettings>("""{"wms":{"name":"w"}}""").WMS.Name);
        Assert.Equal("f", D<WFSSettings>("""{"wfs":{"name":"f"}}""").WFS.Name);
        Assert.Equal("c", D<WCSSettings>("""{"wcs":{"name":"c"}}""").WCS.Name);
        Assert.Equal("t", D<WMTSSettings>("""{"wmts":{"name":"t"}}""").WMTS.Name); // 模型根 "wmts"（服务端实际为 "settings"，E15）
        Assert.Equal("p", D<WPSSettings>("""{"wps":{"name":"p"}}""").WPS.Name);
        Assert.Equal("s", D<CSWSettings>("""{"csw":{"name":"s"}}""").CSW.Name);
    }

    // ---------- 安全类 ----------

    [Fact]
    public void SecurityWrappers_301Shapes_FIXED_E7()
    {
        var u = D<UserWrapper>("""{"user":{"userName":"admin","password":"p","enabled":true,"groups":["g"]}}""");
        Assert.Equal("admin", u.User.UserName);
        Assert.Equal("p", u.User.Password);

        var g = D<GroupWrapper>("""{"group":{"groupName":"grp","enabled":false,"users":["a"]}}""");
        Assert.Equal("grp", g.Group.GroupName);

        // FIXED-E7：users 为对象数组（3.0.1 实测 {"users":[{"userName","enabled","password"}]}）
        var ul = D<UserListWrapper>("""{"users":[{"userName":"a","enabled":true},{"userName":"b","enabled":false}]}""");
        Assert.Equal(2, ul.Users.Count);
        Assert.Equal(new[] { "a", "b" }, ul.UserNames);
        var gl = D<GroupListWrapper>("""{"groups":["x"]}""");
        Assert.Single(gl.Groups);
        var rl = D<RoleListWrapper>("""{"roles":["ADMIN"]}""");
        Assert.Single(rl.Roles);

        // FIXED-E7：服务列表双层 {"userGroupServices":{"userGroupService":[{name,href}]}} → Parse
        var ugs = UserGroupServiceList.Parse("""{"userGroupServices":{"userGroupService":[{"name":"default","href":"h"}]}}""");
        Assert.Equal("default", Assert.Single(ugs.Services));
        Assert.Equal("h", ugs.References[0].Href);

        // FIXED-E1：authfilters 双层 + name/href 摘要
        var afl = AuthenticationFilterListWrapper.Parse("""{"authfilters":{"authfilter":[{"name":"basic","href":"h1"},{"name":"anonymous","href":"h2"}]}}""");
        Assert.Equal(2, afl.Filters.Count);
        Assert.Equal("basic", afl.Filters[0].Name);

        // FIXED-E2：authproviders 动态 Java 类名键 map
        var apl = AuthenticationProviderListWrapper.Parse("""{"authproviders":{"org.geoserver.security.config.MemoryAuthenticationProviderConfig":{"name":"memory","className":"c1"}}}""");
        Assert.Single(apl.Providers);
        Assert.Equal("memory", apl.Providers[0].Name);
        Assert.Equal("org.geoserver.security.config.MemoryAuthenticationProviderConfig", apl.Providers[0].ConfigClassName);

        // FIXED-E3：filterchain 列表 {"filterchain":{"filters":[{"@name",...}]}}
        var fcl = FilterChainListWrapper.Parse("""{"filterchain":{"filters":[{"@name":"web","@path":"/web/**","@disabled":false,"filter":["a","b"]}]}}""");
        Assert.Equal("web", Assert.Single(fcl.Chains).Name);

        Assert.Equal("ADMIN", D<RoleWrapper>("""{"role":{"role":"ADMIN","parentRole":"ROOT","properties":{"k":"v"}}}""").Role.RoleName); // "role"→RoleName
    }

    [Fact]
    public void AclAndKeystore_301Shapes_FIXED_E19_E4()
    {
        // FIXED-E19：catalog 模式形态 {"mode":"HIDE"}
        var mode = SecurityACL.Parse("""{"mode":"HIDE"}""");
        Assert.Equal("HIDE", mode.Mode);
        Assert.Null(mode.Rules);
        // FIXED-E19：扁平 map（key=资源模式，value=逗号分隔角色）转译为 Rules
        var map = SecurityACL.Parse("""{"*.*.r":"*","*.*.w":"GROUP_ADMIN,ADMIN"}""");
        Assert.Equal(2, map.Rules.Count);
        Assert.Equal("GROUP_ADMIN,ADMIN", map.Rules[1].Access);

        // FIXED-E4：keystore 3.0.1 无 REST 端点（模型仅作兼容保留；服务层抛 NotSupportedException）
        var ks = D<KeystoreInfo>("""{"type":"jks","provider":"SUN","aliases":[{"alias":"a","type":"t","algorithm":"alg"}]}""");
        Assert.Equal("a", ks.Aliases[0].Alias);
    }

    // ---------- GWC ----------

    [Fact]
    public void GwcWrappers_RawArrayAndDynamicRoot_FIXED_E8_E9_E11()
    {
        // FIXED-E8：GWC layers/gridsets/blobstores 列表实测为顶层裸 JSON 数组（无包装键）
        Assert.Single(D<GWCLayerListWrapper>("""["ws:l"]""").Layers);
        Assert.Single(D<GridsetListWrapper>("""["EPSG:4326"]""").GridSets);
        Assert.Single(D<BlobstoreListWrapper>("""["diskcache"]""").Blobstores);
        // FIXED-E9：单体 gridset 根为大写 S 的 "gridSet"
        var g = Gridset.ParseSingle("""{"gridSet":{"name":"Custom","srs":{"number":4326},"extent":{"coords":[-180.0,-90.0,180.0,90.0]}}}""");
        Assert.Equal("Custom", g.Name);
        Assert.Equal(4326, g.SRS.Number);
        // FIXED-E11：单体 blobstore 根为实现类名
        Assert.Equal("b", Blobstore.ParseSingle("""{"FileBlobStore":{"id":"b","enabled":true,"baseDirectory":"/d"}}""").Id);
        Assert.Equal("b", D<BlobstoreWrapper>("""{"blobstore":{"id":"b","enabled":true,"properties":{"p":1}}}""").Blobstore.Id); // 旧包装形态兼容保留
    }

    [Fact]
    public void SeedRequest_EmitsGwcXml_FIXED_E12()
    {
        // FIXED-E12：GWC SeedController 实测仅收 XStream XML（zoomStart/zoomStop 必填）；模型提供 ToXmlBody
        var xml = new SeedRequest
        {
            Config = new SeedRequestConfig { Name = "n", GridSetId = "EPSG:4326", ZoomStart = 1, ZoomStop = 2, Format = "image/png", Type = "seed", ThreadCount = 4 }
        }.ToXmlBody();
        Assert.Contains("<seedRequest>", xml);
        Assert.Contains("<name>n</name>", xml);
        Assert.Contains("<zoomStart>1</zoomStart>", xml);
        Assert.Contains("<zoomStop>2</zoomStop>", xml);
        Assert.Contains("<type>seed</type>", xml);
        Assert.Contains("<threadCount>4</threadCount>", xml);
    }

    // ---------- 其他列表包装（模型自身期望的扁平形态） ----------

    [Fact]
    public void OtherListWrappers_FlatShapes_CurrentModelExpectation()
    {
        // FIXED-E7（fonts 误报翻正）：实测 /rest/fonts.json 就是 {"fonts":[字符串...]} 直接数组
        Assert.Single(D<FontListWrapper>("""{"fonts":["Arial"]}""").Fonts);
        // FIXED-E7：templates 根为类全名（空态 ""），走容错 Parse
        Assert.Single(TemplateListWrapper.Parse("""{"org.geoserver.rest.catalog.TemplateInfos":{"org.geoserver.rest.catalog.TemplateInfo":[{"name":"t","href":"h"}]}}""").Templates);
        Assert.Empty(TemplateListWrapper.Parse("""{"org.geoserver.rest.catalog.TemplateInfos":""}""").Templates);
        // FIXED-E7：urlchecks 根为 urlChecks（空态 ""、有值 {"entry":[{"string":[k,v]}]}），走 Parse
        Assert.Equal("blockLocal", Assert.Single(URLCheckListWrapper.Parse("""{"urlChecks":{"entry":[{"string":["blockLocal","false"]}]}}""").Checks));
        Assert.Empty(URLCheckListWrapper.Parse("""{"urlChecks":""}""").Checks);
        Assert.Single(D<TransformListWrapper>("""{"transforms":["x"]}""").Transforms);
        Assert.Single(D<CoverageViewListWrapper>("""{"coverageViews":["cv"]}""").CoverageViews);
    }

    // ---------- 关键资源形态 ----------

    [Fact]
    public void ConnectionParameters_EntryKeyDollarXmlStyleJson()
    {
        // entry 数组 + "@key" / "$" 形态（与 GeoServer XML 风格 JSON 一致）
        var cp = D<ConnectionParameters>("""{"entry":[{"@key":"host","$":"db"},{"@key":"port","$":"5432"},{"@key":"dbname","$":"gis"}]}""");
        Assert.Equal(3, cp.Entries.Length);
        Assert.Equal("host", cp.Entries[0].Key);
        Assert.Equal("db", cp.Entries[0].Value);
        Assert.Equal("5432", cp.Entries[1].Value);
    }

    [Fact]
    public void Layer_ResourceAtClassAttribute()
    {
        var w = D<LayerWrapper>("""{"layer":{"resource":{"@class":"coverage","name":"c","href":"h"}}}""");
        Assert.Equal("coverage", w.Layer.Resource.Class); // @class 区分资源类型
    }

    [Fact]
    public void LayerGroup_PublishablesPublishedArray()
    {
        var w = D<LayerGroupWrapper>("""{"layerGroup":{"publishables":{"published":[{"@type":"layer","name":"a"},{"@type":"layerGroup","name":"g"}]}}}""");
        Assert.Equal(2, w.LayerGroup.Publishables.Published.Length);
        Assert.Equal("layerGroup", w.LayerGroup.Publishables.Published[1].Type);
    }

    [Fact]
    public void FeatureType_EnabledIsNullableBoolDefaultTrue_FIXED_E27()
    {
        // FIXED-E27：FeatureType.Enabled 改为 bool? 且构造函数默认 true——直调创建不再默认禁用。
        // 反序列化经同一构造函数，响应缺 "enabled" 键时保持默认 true（对照组 bool? 资源仍为 null）。
        var bare = D<FeatureType>("{}");
        Assert.True(bare.Enabled);

        var ft = D<FeatureType>("""{"name":"f"}""");
        Assert.True(ft.Enabled);

        var explicitFalse = D<FeatureType>("""{"name":"f","enabled":false}""");
        Assert.False(explicitFalse.Enabled); // 显式 false 仍如实还原

        var cov = D<Coverage>("{}");
        Assert.Null(cov.Enabled); // 对照组：Coverage.Enabled 为 bool?

        var ds = D<DataStore>("{}");
        Assert.Null(ds.Enabled);  // 对照组：DataStore.Enabled 为 bool?
    }

    [Fact]
    public void BoundingBox_AllLowercaseAxisKeys()
    {
        // E29：minx/maxx/miny/maxy 全小写键与 GeoServer JSON 一致
        var bb = D<BoundingBox>("""{"minx":-1,"maxx":2,"miny":-3,"maxy":4,"crs":"EPSG:4326"}""");
        Assert.Equal(-1, bb.MinX);
        Assert.Equal(4, bb.MaxY);
        Assert.Equal("EPSG:4326", bb.Crs);
    }

    [Fact]
    public void CrsStringConverter_AcceptsStringAndReferenceObject()
    {
        // CrsStringConverter：GeoServer 3.0.1 的 coverage nativeCRS / 边界框 crs 可能为
        // {"@class":"projected","$":"..."} 引用对象（原 KNOWN-ISSUE），统一宽容为字符串。
        var bb = D<BoundingBox>("""{"minx":0,"maxx":1,"miny":0,"maxy":1,"crs":{"@class":"projected","$":"EPSG:32754"}}""");
        Assert.Equal("EPSG:32754", bb.Crs);

        var cov = D<Coverage>("""{"name":"c","nativeCRS":{"@class":"projected","$":"PROJCS[WGS 84 / UTM zone 54S]"}}""");
        Assert.Equal("PROJCS[WGS 84 / UTM zone 54S]", cov.NativeCRS);

        // 字符串形态原样返回（回归：原有行为不变）
        var bb2 = D<BoundingBox>("""{"crs":"EPSG:4326"}""");
        Assert.Equal("EPSG:4326", bb2.Crs);
    }

    [Fact]
    public void GwcLayer_GridSubsetAndExtent()
    {
        var l = D<GWCLayer>("""{"name":"l","enabled":true,"mimeFormats":["image/png"],"gridSubsets":[{"gridSetName":"EPSG:900913","zoomStart":0,"zoomStop":10,"extent":{"coords":[0,0,1,1]}}],"metaWidthHeight":[4,4],"expireCache":30,"expireClients":60}""");
        Assert.Equal(4, l.GridSubsets[0].Extent.Coords.Length);
        Assert.Equal(30, l.ExpireCache);
    }

    [Fact]
    public void DiskQuotaAndGridset_FlatModels()
    {
        var q = D<DiskQuotaConfig>("""{"enabled":false,"diskBlockSize":1024,"cacheCleanUpFrequency":1,"cacheCleanUpUnits":"DAYS","maxConcurrentCleanUps":3,"globalQuota":{"value":2.5,"units":"TiB"},"layerQuotas":[{"layer":"l","quota":{"value":1,"units":"GiB"}}]}""");
        Assert.Equal("DAYS", q.CacheCleanUpUnits);
        Assert.Equal(2.5, q.GlobalQuota.Value);

        var g = D<Gridset>("""{"name":"g","srs":{"number":3857},"extent":{"coords":[-1,-1,1,1]},"alignTopLeft":true,"resolutions":[1.0,0.5],"metersPerUnit":1.0,"pixelSize":0.26458,"scaleNames":["a","b"],"tileHeight":512,"tileWidth":512,"yCoordinateFirst":true}""");
        Assert.Equal(3857, g.SRS.Number);
        Assert.True(g.YCoordinateFirst);
    }

    [Fact]
    public void FilterChainAndAuthEntities_FlatModels_FIXED_E1_E3()
    {
        // FIXED-E3：3.0.1 filterchain 元素字段为 XML 风格 @ 前缀键（@name/@path/@class/@disabled/...）
        var fc = D<FilterChain>("""{"@name":"c","@path":"/rest/**","filter":["a","b"],"@allowSessionCreation":false,"@ssl":true,"@matchHTTPMethod":false}""");
        Assert.Equal("c", fc.Name);
        Assert.Equal("/rest/**", fc.Pattern);
        Assert.True(fc.RequireSSL);
        Assert.Equal(new[] { "a", "b" }, fc.Filters);

        // FIXED-E1：单体 authfilter 根为配置类全名（{"<FQN>":{...}}），走 ParseSingle
        var f = AuthenticationFilter.ParseSingle("""{"org.geoserver.security.config.BasicAuthenticationFilterConfig":{"id":"x","name":"basic","className":"C","useRememberMe":true}}""");
        Assert.Equal("basic", f.Name);
        Assert.Equal("C", f.ClassName);
        Assert.Equal("org.geoserver.security.config.BasicAuthenticationFilterConfig", f.ConfigClassName);
        Assert.True((bool)f.Config["useRememberMe"]);

        var pw = Json.P(JsonConvert.SerializeObject(new PasswordChangeRequest { NewPassword = "n" }));
        Assert.Equal("n", (string?)pw["newPassword"]); // 无包装根（E16 序列化侧）
    }

    [Fact]
    public void ExtensionModels_TransformTemplateUrlCheck()
    {
        Assert.Equal("<x/>", D<Transform>("""{"name":"t","xslt":"<x/>","sourceFormat":"s","outputFormat":"o"}""").XSLT);
        Assert.Equal("c", D<Template>("""{"name":"n","content":"c","type":"header"}""").Content);
        var uc = D<URLCheck>("""{"name":"n","description":"d","enabled":true,"urlPattern":"file:.*","checkType":"DENY"}""");
        Assert.Equal("DENY", uc.CheckType);
    }
}
