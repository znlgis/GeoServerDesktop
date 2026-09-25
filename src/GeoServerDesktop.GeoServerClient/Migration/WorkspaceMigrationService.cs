using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeoServerDesktop.GeoServerClient.Migration
{
    /// <summary>
    /// 工作空间导入/导出服务（M4）：GeoServer 实例间迁移工具。
    /// 导出：REST 读工作空间全量资源（namespace/存储/资源/样式/图层绑定/图层组）→ 内存 ZIP 归档
    /// （manifest.json + styles/*.sld），资源详情按 REST 原样 JSON 保存。
    /// 导入：解包 → 按依赖顺序回放到目标实例（ws → namespace → store → style → featureType/coverage
    /// → 图层绑定 → layerGroup），回写时做引用重写并剥离服务端管理字段；幂等（已存在资源默认跳过，
    /// OverwriteExisting 时更新）。
    ///
    /// 实测依据（GeoServer 3.0.1）：
    /// ① 创建工作空间会自动生成同名命名空间，但 URI 是占位形态（http://workspace）——迁移需要保留
    ///    源 URI 时仍要显式建/改命名空间；recurse=true 删除工作空间会连带回收其孤立命名空间。
    /// ② 创建 featureType/coverage 会自动发布同名图层，默认样式为内置 polygon/raster——因此清单只存
    ///    图层"默认样式绑定差异"，回放后用 PUT layer（局部更新实测生效）还原。
    /// ③ PUT/POST 请求体含 null 引用字段会触发 XStream NPE（500），回放前剥离 href/id/dateCreated/
    ///    dateModified/_default 等服务端字段；store/namespace 的限定名引用整体替换。
    /// </summary>
    public class WorkspaceMigrationService : ServiceBase, IWorkspaceMigrationService
    {
        private const string ManifestName = "manifest.json";

        /// <summary>需要剥离的服务端管理键（任何层级）。注意不含 nativeCRS/nativeBoundingBox 等真实配置数据。</summary>
        private static readonly string[] StripKeys =
        {
            "href", "dateCreated", "dateModified", "_default",
        };


        /// <summary>
        /// 初始化 WorkspaceMigrationService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public WorkspaceMigrationService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        // ================= 导出 =================

        /// <summary>
        /// 导出工作空间为迁移归档（内存 ZIP 字节 + 清单）。
        /// </summary>
        /// <param name="workspaceName">工作空间名。</param>
        /// <returns>导出结果。</returns>
        public async Task<WorkspaceExportResult> ExportWorkspaceAsync(string workspaceName)
        {
            if (string.IsNullOrEmpty(workspaceName)) throw new ArgumentException("缺少工作空间名", nameof(workspaceName));

            var manifest = new WorkspaceManifest
            {
                SchemaVersion = WorkspaceManifest.CurrentSchemaVersion,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Workspace = workspaceName,
                NamespacePrefix = workspaceName,
            };

            manifest.SourceVersion = await TryGetVersionAsync();

            // 命名空间：3.0.1 无 /rest/workspaces/{ws}/namespace 反查端点（实测 404），
            // 按惯例前缀=工作空间名，直接 GET /rest/namespaces/{prefix}.json；缺失为正常状态
            // （部分部署/recurse 删除后会出现），导入端负责补建占位 URI。
            try
            {
                var nsJson = await Http.GetAsync("/rest/namespaces/" + EscPath(workspaceName) + ".json");
                var ns = Root(nsJson, "namespace");
                if (ns != null)
                {
                    manifest.NamespacePrefix = (string)ns["prefix"] ?? workspaceName;
                    manifest.NamespaceUri = (string)ns["uri"];
                }
            }
            catch (GeoServerRequestException ex) when (ex.StatusCode == 404)
            {
                // 正常缺失：不记警告
            }
            catch (Exception ex)
            {
                manifest.Warnings.Add("namespace: " + Describe(ex));
            }

            var slds = new Dictionary<string, string>(StringComparer.Ordinal);

            // 数据存储 + 其下要素类型
            foreach (var store in await ListRootAsync("/rest/workspaces/" + EscPath(workspaceName) + "/datastores.json",
                "dataStores", "dataStore", manifest, "datastores"))
            {
                string name = (string)store["name"];
                if (string.IsNullOrEmpty(name)) continue;
                var detail = await GetDetailAsync("/rest/workspaces/" + EscPath(workspaceName) + "/datastores/" + EscPath(name) + ".json",
                    "dataStore", manifest, "dataStore " + name);
                if (detail == null) continue;
                manifest.DataStores.Add(new DataStoreEntry
                {
                    Name = name,
                    Type = (string)detail["type"],
                    Raw = detail,
                });

                foreach (var ftRef in await ListRootAsync(
                    "/rest/workspaces/" + EscPath(workspaceName) + "/datastores/" + EscPath(name) + "/featuretypes.json",
                    "featureTypes", "featureType", manifest, "featuretypes@" + name))
                {
                    string ftName = (string)ftRef["name"];
                    if (string.IsNullOrEmpty(ftName)) continue;
                    var ft = await GetDetailAsync(
                        "/rest/workspaces/" + EscPath(workspaceName) + "/datastores/" + EscPath(name) + "/featuretypes/" + EscPath(ftName) + ".json",
                        "featureType", manifest, "featureType " + name + ":" + ftName);
                    if (ft == null) continue;
                    manifest.FeatureTypes.Add(new FeatureTypeEntry
                    {
                        Name = ftName,
                        Store = name,
                        NativeName = (string)ft["nativeName"],
                        Raw = ft,
                    });
                }
            }

            // 覆盖存储 + 其下覆盖度
            foreach (var store in await ListRootAsync("/rest/workspaces/" + EscPath(workspaceName) + "/coveragestores.json",
                "coverageStores", "coverageStore", manifest, "coveragestores"))
            {
                string name = (string)store["name"];
                if (string.IsNullOrEmpty(name)) continue;
                var detail = await GetDetailAsync("/rest/workspaces/" + EscPath(workspaceName) + "/coveragestores/" + EscPath(name) + ".json",
                    "coverageStore", manifest, "coverageStore " + name);
                if (detail == null) continue;
                manifest.CoverageStores.Add(new CoverageStoreEntry
                {
                    Name = name,
                    Type = (string)detail["type"],
                    Url = (string)detail["url"],
                    Raw = detail,
                });

                foreach (var covRef in await ListRootAsync(
                    "/rest/workspaces/" + EscPath(workspaceName) + "/coveragestores/" + EscPath(name) + "/coverages.json",
                    "coverages", "coverage", manifest, "coverages@" + name))
                {
                    string covName = (string)covRef["name"];
                    if (string.IsNullOrEmpty(covName)) continue;
                    var cov = await GetDetailAsync(
                        "/rest/workspaces/" + EscPath(workspaceName) + "/coveragestores/" + EscPath(name) + "/coverages/" + EscPath(covName) + ".json",
                        "coverage", manifest, "coverage " + name + ":" + covName);
                    if (cov == null) continue;
                    manifest.Coverages.Add(new CoverageEntry
                    {
                        Name = covName,
                        Store = name,
                        NativeName = (string)cov["nativeName"],
                        Raw = cov,
                    });
                }
            }

            // 工作空间样式（SLD 内容入包；导入端同名全局样式是否存在决定注册层，避免同名歧义）
            foreach (var styleRef in await ListRootAsync("/rest/workspaces/" + EscPath(workspaceName) + "/styles.json",
                "styles", "style", manifest, "styles"))
            {
                string name = (string)styleRef["name"];
                if (string.IsNullOrEmpty(name)) continue;
                var entry = new StyleEntry { Name = name, Global = false, Filename = name + ".sld" };
                await CaptureSldAsync("/rest/workspaces/" + EscPath(workspaceName) + "/styles/" + EscPath(name) + ".sld",
                    "styles/" + workspaceName + "_" + name + ".sld", entry, slds, manifest);
                bool sameNameGlobal = await GetOrNullAsync("/rest/styles/" + EscPath(name) + ".json", "style") != null;
                entry.DedupSameNameGlobalStyle = sameNameGlobal;
                if (sameNameGlobal)
                {
                    // 同名全局样式 SLD 一并入包（导入端按工作空间层注册；目标实例可能没有该全局样式）
                    var globalEntry = manifest.Styles.Find(s => s.Global && string.Equals(s.Name, name, StringComparison.Ordinal));
                    if (globalEntry == null)
                    {
                        globalEntry = new StyleEntry { Name = name, Global = true, Filename = name + ".sld" };
                        await CaptureSldAsync("/rest/styles/" + EscPath(name) + ".sld",
                            "styles/" + name + ".sld", globalEntry, slds, manifest);
                        manifest.Styles.Add(globalEntry);
                    }
                }
                manifest.Styles.Add(entry);
            }

            // 图层：默认样式绑定差异（图层资源由资源创建自动发布）
            string sourceUri = manifest.NamespaceUri;
            foreach (var layerRef in await ListRootAsync("/rest/workspaces/" + EscPath(workspaceName) + "/layers.json",
                "layers", "layer", manifest, "layers"))
            {
                string name = (string)layerRef["name"];
                if (string.IsNullOrEmpty(name)) continue;
                var detail = await GetDetailAsync("/rest/workspaces/" + EscPath(workspaceName) + "/layers/" + EscPath(name) + ".json",
                    "layer", manifest, "layer " + name);
                if (detail == null) continue;
                var defaultStyle = detail["defaultStyle"] as JObject;
                if (defaultStyle == null) continue;
                string styleName = (string)defaultStyle["name"];
                if (string.IsNullOrEmpty(styleName)) continue;

                string href = (string)defaultStyle["href"];
                bool isWorkspaceStyle = href != null
                    && href.IndexOf("/workspaces/", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!isWorkspaceStyle)
                {
                    // 全局样式：确保 SLD 一并入包（目标实例可能没有该样式）
                    var globalEntry = manifest.Styles.Find(s => s.Global && string.Equals(s.Name, styleName, StringComparison.Ordinal));
                    if (globalEntry == null)
                    {
                        globalEntry = new StyleEntry { Name = styleName, Global = true, Filename = styleName + ".sld" };
                        await CaptureSldAsync("/rest/styles/" + EscPath(styleName) + ".sld",
                            "styles/" + styleName + ".sld", globalEntry, slds, manifest);
                        manifest.Styles.Add(globalEntry);
                    }
                }

                manifest.LayerBindings.Add(new LayerBindingEntry
                {
                    QualifiedLayer = workspaceName + ":" + name,
                    Style = styleName,
                    WorkspaceStyle = isWorkspaceStyle,
                });
            }

            // 工作空间图层组
            foreach (var lgRef in await ListRootAsync("/rest/workspaces/" + EscPath(workspaceName) + "/layergroups.json",
                "layerGroups", "layerGroup", manifest, "layergroups"))
            {
                string name = (string)lgRef["name"];
                if (string.IsNullOrEmpty(name)) continue;
                var detail = await GetDetailAsync("/rest/workspaces/" + EscPath(workspaceName) + "/layergroups/" + EscPath(name) + ".json",
                    "layerGroup", manifest, "layerGroup " + name);
                if (detail == null) continue;
                manifest.LayerGroups.Add(new LayerGroupEntry { Name = name, Raw = detail });
            }

            var archive = BuildArchive(manifest, slds);
            return new WorkspaceExportResult { Archive = archive, Manifest = manifest };
        }

        // ================= 导入 =================

        /// <summary>
        /// 从归档导入（重建）工作空间。幂等：已存在资源默认跳过，OverwriteExisting 时更新。
        /// </summary>
        /// <param name="request">导入请求。</param>
        /// <returns>导入结果（逐项回放状态）。</returns>
        public async Task<WorkspaceImportResult> ImportWorkspaceAsync(WorkspaceImportRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var bytes = request.Archive;
            if ((bytes == null || bytes.Length == 0) && !string.IsNullOrEmpty(request.ArchivePath))
                bytes = File.ReadAllBytes(request.ArchivePath);
            if (bytes == null || bytes.Length == 0)
                throw new ArgumentException("归档为空（Archive 与 ArchivePath 均未提供）", nameof(request));

            var manifest = ReadManifest(bytes);
            var result = new WorkspaceImportResult { Manifest = manifest };

            string sourceWs = manifest.Workspace;
            string sourcePrefix = string.IsNullOrEmpty(manifest.NamespacePrefix) ? sourceWs : manifest.NamespacePrefix;
            string sourceUri = manifest.NamespaceUri;

            string targetWs = string.IsNullOrEmpty(request.TargetWorkspace) ? sourceWs : request.TargetWorkspace;
            string targetPrefix = string.IsNullOrEmpty(request.TargetNamespacePrefix) ? targetWs : request.TargetNamespacePrefix;
            string targetUri = ResolveTargetNamespaceUri(request, manifest, targetWs);

            // 1) 工作空间（同时自动得到占位命名空间）
            await Step(result, "workspace", targetWs, async () =>
            {
                bool exists = await ExistsAsync("/rest/workspaces/" + EscPath(targetWs) + ".json");
                if (!exists)
                    await PostJsonAsync("/rest/workspaces", "workspace", new JObject { { "name", targetWs } });
                else if (!request.OverwriteExisting)
                    return ToSkipped("工作空间已存在，内部资源按逐项跳过/覆盖策略处理");
                return null;
            });

            // 2) 命名空间 URI 校正（保留源 URI 语义）
            await Step(result, "namespace", targetPrefix, async () =>
            {
                var ns = await GetOrNullAsync("/rest/namespaces/" + EscPath(targetPrefix) + ".json", "namespace");
                if (ns == null)
                {
                    // 工作空间创建应已自动生成；仍缺失则显式创建
                    await PostJsonAsync("/rest/namespaces", "namespace",
                        new JObject { { "prefix", targetPrefix }, { "uri", targetUri } });
                    return null;
                }
                string currentUri = (string)ns["uri"];
                if (!string.Equals(currentUri, targetUri, StringComparison.Ordinal))
                {
                    await PutJsonAsync("/rest/namespaces/" + EscPath(targetPrefix), "namespace",
                        new JObject { { "prefix", targetPrefix }, { "uri", targetUri } });
                }
                return null;
            });

            // 3) 存储
            foreach (var entry in manifest.DataStores)
            {
                await Step(result, "dataStore", targetWs + ":" + entry.Name, async () =>
                {
                    string path = "/rest/workspaces/" + EscPath(targetWs) + "/datastores/" + EscPath(entry.Name) + ".json";
                    bool exists = await ExistsAsync(path);
                    if (exists && !request.OverwriteExisting) return ToSkipped("已存在");
                    var raw = Rewrite(entry.Raw, sourceWs, sourcePrefix, sourceUri, targetWs, targetPrefix, targetUri);
                    if (exists)
                    {
                        await PutJsonAsync("/rest/workspaces/" + EscPath(targetWs) + "/datastores/" + EscPath(entry.Name), "dataStore", raw);
                        return null;
                    }
                    await PostJsonAsync("/rest/workspaces/" + EscPath(targetWs) + "/datastores", "dataStore", raw);
                    return null;
                });
            }
            foreach (var entry in manifest.CoverageStores)
            {
                await Step(result, "coverageStore", targetWs + ":" + entry.Name, async () =>
                {
                    string path = "/rest/workspaces/" + EscPath(targetWs) + "/coveragestores/" + EscPath(entry.Name) + ".json";
                    bool exists = await ExistsAsync(path);
                    if (exists && !request.OverwriteExisting) return ToSkipped("已存在");
                    var raw = Rewrite(entry.Raw, sourceWs, sourcePrefix, sourceUri, targetWs, targetPrefix, targetUri);
                    if (exists)
                    {
                        await PutJsonAsync("/rest/workspaces/" + EscPath(targetWs) + "/coveragestores/" + EscPath(entry.Name), "coverageStore", raw);
                        return null;
                    }
                    await PostJsonAsync("/rest/workspaces/" + EscPath(targetWs) + "/coveragestores", "coverageStore", raw);
                    return null;
                });
            }

            // 4) 样式（两步：POST 元数据 + PUT SLD 内容）
            foreach (var entry in manifest.Styles)
            {
                string sld = ReadArchiveMember(bytes, entry.SldPath);
                await Step(result, "style", (entry.Global ? "" : targetWs + ":") + entry.Name, async () =>
                {
                    if (string.IsNullOrEmpty(entry.SldPath) || sld == null)
                        return ToFailed("归档缺少 SLD 内容（" + (entry.SldPath ?? "(无路径)") + "）");
                    string basePath = entry.Global
                        ? "/rest/styles"
                        : "/rest/workspaces/" + EscPath(targetWs) + "/styles";
                    string getPath = basePath + "/" + EscPath(entry.Name) + ".json";
                    bool exists = await ExistsAsync(getPath);
                    if (exists && !request.OverwriteExisting) return ToSkipped("已存在");
                    if (!exists)
                    {
                        await PostJsonAsync(basePath, "style", new JObject
                        {
                            { "name", entry.Name },
                            { "filename", string.IsNullOrEmpty(entry.Filename) ? entry.Name + ".sld" : entry.Filename },
                        });
                    }
                    using (var content = new StringContent(sld, Encoding.UTF8, "application/vnd.ogc.sld+xml"))
                    {
                        await Http.PutAsync(basePath + "/" + EscPath(entry.Name), content);
                    }
                    return null;
                });
            }

            // 5) 资源（featureType / coverage；自动发布图层）
            foreach (var entry in manifest.FeatureTypes)
            {
                await Step(result, "featureType", targetWs + ":" + entry.Store + ":" + entry.Name, async () =>
                {
                    string store = MapStore(entry.Store, sourceWs, targetWs);
                    string basePath = "/rest/workspaces/" + EscPath(targetWs) + "/datastores/" + EscPath(store) + "/featuretypes";
                    bool exists = await ExistsAsync(basePath + "/" + EscPath(entry.Name) + ".json");
                    if (exists && !request.OverwriteExisting) return ToSkipped("已存在");
                    var raw = Rewrite(entry.Raw, sourceWs, sourcePrefix, sourceUri, targetWs, targetPrefix, targetUri);
                    RemovePath(raw, "namespace");
                    RemovePath(raw, "store");
                    RemovePath(raw, "Id");
                    RemovePath(raw, "href");
                    if (exists)
                    {
                        await PutJsonAsync(basePath + "/" + EscPath(entry.Name), "featureType", raw);
                        return null;
                    }
                    await PostJsonAsync(basePath, "featureType", raw);
                    return null;
                });
            }
            foreach (var entry in manifest.Coverages)
            {
                await Step(result, "coverage", targetWs + ":" + entry.Store + ":" + entry.Name, async () =>
                {
                    string store = MapStore(entry.Store, sourceWs, targetWs);
                    string basePath = "/rest/workspaces/" + EscPath(targetWs) + "/coveragestores/" + EscPath(store) + "/coverages";
                    bool exists = await ExistsAsync(basePath + "/" + EscPath(entry.Name) + ".json");
                    if (exists && !request.OverwriteExisting) return ToSkipped("已存在");
                    var raw = Rewrite(entry.Raw, sourceWs, sourcePrefix, sourceUri, targetWs, targetPrefix, targetUri);
                    RemovePath(raw, "namespace");
                    RemovePath(raw, "store");
                    RemovePath(raw, "metadata");
                    if (exists)
                    {
                        await PutJsonAsync(basePath + "/" + EscPath(entry.Name), "coverage", raw);
                        return null;
                    }
                    await PostJsonAsync(basePath, "coverage", raw);
                    return null;
                });
            }

            // 6) 图层默认样式绑定（资源创建自动发布后还原差异）
            foreach (var entry in manifest.LayerBindings)
            {
                string layerShort = entry.QualifiedLayer != null && entry.QualifiedLayer.Contains(":")
                    ? entry.QualifiedLayer.Substring(entry.QualifiedLayer.IndexOf(':') + 1)
                    : entry.QualifiedLayer;
                await Step(result, "layerBinding", targetWs + ":" + layerShort, async () =>
                {
                    var style = new JObject { { "name", entry.Style } };
                    if (entry.WorkspaceStyle)
                    {
                        style["workspace"] = new JObject { { "name", targetWs } };
                    }
                    var body = new JObject
                    {
                        { "name", targetWs + ":" + layerShort },
                        { "defaultStyle", style },
                    };
                    await PutJsonRaw("/rest/layers/" + EscPath(targetWs + ":" + layerShort),
                        "{\"layer\":" + body.ToString(Formatting.None) + "}");
                    return null;
                });
            }

            // 7) 图层组
            foreach (var entry in manifest.LayerGroups)
            {
                await Step(result, "layerGroup", targetWs + ":" + entry.Name, async () =>
                {
                    string basePath = "/rest/workspaces/" + EscPath(targetWs) + "/layergroups";
                    bool exists = await ExistsAsync(basePath + "/" + EscPath(entry.Name) + ".json");
                    if (exists && !request.OverwriteExisting) return ToSkipped("已存在");
                    var raw = Rewrite(entry.Raw, sourceWs, sourcePrefix, sourceUri, targetWs, targetPrefix, targetUri);
                    RemovePath(raw, "id");
                    RemovePath(raw, "uuid");
                    if (exists)
                    {
                        await PutJsonAsync(basePath + "/" + EscPath(entry.Name), "layerGroup", raw);
                        return null;
                    }
                    await PostJsonAsync(basePath, "layerGroup", raw);
                    return null;
                });
            }

            return result;
        }

        /// <summary>
        /// 读取归档清单（不做回放）。
        /// </summary>
        /// <param name="archive">归档字节。</param>
        /// <returns>清单。</returns>
        public static WorkspaceManifest ReadManifest(byte[] archive)
        {
            if (archive == null || archive.Length == 0) throw new ArgumentException("归档为空", nameof(archive));
            string json = ReadArchiveMember(archive, ManifestName);
            if (json == null) throw new InvalidDataException("归档缺少 manifest.json（不是 GeoServerDesktop 工作空间归档）");
            WorkspaceManifest manifest;
            try
            {
                manifest = JsonConvert.DeserializeObject<WorkspaceManifest>(json);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("manifest.json 解析失败：" + ex.Message, ex);
            }
            if (manifest == null || string.IsNullOrEmpty(manifest.Workspace))
                throw new InvalidDataException("manifest.json 缺少工作空间名");
            if (manifest.SchemaVersion > WorkspaceManifest.CurrentSchemaVersion)
                throw new InvalidDataException("归档 schema 版本 " + manifest.SchemaVersion
                    + " 高于当前支持版本 " + WorkspaceManifest.CurrentSchemaVersion);
            return manifest;
        }

        // ================= 归档 =================

        private static byte[] BuildArchive(WorkspaceManifest manifest, Dictionary<string, string> slds)
        {
            using (var buffer = new MemoryStream())
            {
                using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, true))
                {
                    WriteEntry(zip, ManifestName, JsonConvert.SerializeObject(manifest, Formatting.None));
                    foreach (var kv in slds)
                    {
                        if (kv.Value == null) continue;
                        WriteEntry(zip, kv.Key, kv.Value);
                    }
                }
                return buffer.ToArray();
            }
        }

        private static void WriteEntry(ZipArchive zip, string name, string content)
        {
            var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
            {
                writer.Write(content);
            }
        }

        private static string ReadArchiveMember(byte[] archive, string name)
        {
            return ReadArchiveMemberPublic(archive, name);
        }

        /// <summary>
        /// 读取归档包内指定成员内容（UTF-8 文本；成员名为归档内相对路径）。
        /// </summary>
        /// <param name="archive">归档字节。</param>
        /// <param name="name">成员名（如 manifest.json、styles/x.sld）。</param>
        /// <returns>成员内容；不存在时 null。</returns>
        public static string ReadArchiveMemberText(byte[] archive, string name)
        {
            return ReadArchiveMemberPublic(archive, name);
        }

        private static string ReadArchiveMemberPublic(byte[] archive, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            using (var ms = new MemoryStream(archive))
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                {
                    if (string.Equals(entry.FullName, name, StringComparison.OrdinalIgnoreCase))
                    {
                        using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
            return null;
        }

        // ================= 引用重写 =================

        private static string ResolveTargetNamespaceUri(WorkspaceImportRequest request, WorkspaceManifest manifest, string targetWs)
        {
            if (!string.IsNullOrEmpty(request.TargetNamespaceUri)) return request.TargetNamespaceUri;
            if (!string.IsNullOrEmpty(manifest.NamespaceUri)
                && string.Equals(manifest.Workspace, targetWs, StringComparison.Ordinal))
            {
                return manifest.NamespaceUri;
            }
            return "http://" + targetWs;
        }

        /// <summary>存储名映射：源限定名（ws:store）→ 目标限定名（target:store）。</summary>
        private static string MapStore(string storeRef, string sourceWs, string targetWs)
        {
            if (string.IsNullOrEmpty(storeRef)) return storeRef;
            if (storeRef.StartsWith(sourceWs + ":", StringComparison.Ordinal) && storeRef.Length > sourceWs.Length + 1)
                return targetWs + ":" + storeRef.Substring(sourceWs.Length + 1);
            return storeRef;
        }

        /// <summary>
        /// 深度重写归档 raw：引用替换（workspace/namespace/store 的 name、连接参数 entry 值、图层组字符串引用）
        /// 与服务端字段剥离。返回新的 JObject，不改原对象。
        /// </summary>
        internal static JObject Rewrite(
            JObject raw,
            string sourceWs, string sourcePrefix, string sourceUri,
            string targetWs, string targetPrefix, string targetUri)
        {
            if (raw == null) return null;
            var clone = (JObject)raw.DeepClone();
            RewriteObject(clone, sourceWs, sourcePrefix, sourceUri, targetWs, targetPrefix, targetUri, null);
            return clone;
        }

        private static void RewriteObject(
            JObject obj,
            string sourceWs, string sourcePrefix, string sourceUri,
            string targetWs, string targetPrefix, string targetUri,
            string parentKey)
        {
            // 剥离服务端管理键
            foreach (var key in StripKeys)
            {
                obj.Property(key)?.Remove();
            }
            // "Id"（覆盖度大小写变体）
            obj.Property("Id")?.Remove();

            var properties = new List<JProperty>();
            foreach (var prop in obj.Properties()) properties.Add(prop);

            foreach (var prop in properties)
            {
                // 引用对象 name 替换：workspace/namespace/store 内联引用
                if (prop.Name == "name" && prop.Value.Type == JTokenType.String
                    && (parentKey == "workspace" || parentKey == "namespace"))
                {
                    if (parentKey == "workspace" && IsExact((string)prop.Value, sourceWs)) prop.Value = targetWs;
                    else if (parentKey == "namespace" && IsExact((string)prop.Value, sourcePrefix)) prop.Value = targetPrefix;
                    continue;
                }
                if (prop.Name == "prefix" && prop.Value.Type == JTokenType.String
                    && IsExact((string)prop.Value, sourcePrefix))
                {
                    prop.Value = targetPrefix;
                    continue;
                }
                // 限定名/工作空间名引用（layergroup name、layerDependencies/namedLayers 的 name 等）
                if (prop.Name == "name" && prop.Value.Type == JTokenType.String
                    && IsQualified((string)prop.Value, sourceWs))
                {
                    prop.Value = MapStore((string)prop.Value, sourceWs, targetWs);
                    continue;
                }
                if (prop.Name == "uri" && prop.Value.Type == JTokenType.String
                    && !string.IsNullOrEmpty(sourceUri) && IsExact((string)prop.Value, sourceUri))
                {
                    prop.Value = targetUri;
                    continue;
                }
                if (prop.Name == "store" && prop.Value.Type == JTokenType.String
                    && IsQualified((string)prop.Value, sourceWs))
                {
                    prop.Value = MapStore((string)prop.Value, sourceWs, targetWs);
                    continue;
                }
                if (prop.Value is JObject child)
                {
                    if (prop.Name == "connectionParameters" && child.Count > 0 && !(child.First is JProperty fp && fp.Name == "entry"))
                    {
                        // 纯对象形态（部分 3.x 部署）：按属性名/值直接改写
                        RewritePlainConnectionParams(child, sourceWs, sourceUri, targetWs, targetUri);
                    }
                    else
                    {
                        RewriteObject(child, sourceWs, sourcePrefix, sourceUri, targetWs, targetPrefix, targetUri, prop.Name);
                    }
                    continue;
                }
                if (prop.Value is JArray arr)
                {
                    RewriteArray(arr, sourceWs, sourcePrefix, sourceUri, targetWs, targetPrefix, targetUri, prop.Name);
                }
            }
        }

        private static void RewriteArray(
            JArray arr,
            string sourceWs, string sourcePrefix, string sourceUri,
            string targetWs, string targetPrefix, string targetUri,
            string parentKey)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                var token = arr[i];
                if (token is JObject obj)
                {
                    // connectionParameters.entry：@key + $（或 name/value 形态）
                    if (parentKey == "entry")
                    {
                        RewriteEntryObject(obj, sourceWs, sourceUri, targetWs, targetUri);
                    }
                    // 图层组引用元素：{"name":"ws:layer","href":...}
                    else
                    {
                        RewriteObject(obj, sourceWs, sourcePrefix, sourceUri, targetWs, targetPrefix, targetUri, parentKey);
                    }
                    continue;
                }
                if (token.Type == JTokenType.String)
                {
                    var s = (string)token;
                    // 图层组 publishStep 等直接以字符串引用限定名
                    if (IsQualified(s, sourceWs)) arr[i] = MapStore(s, sourceWs, targetWs);
                    else if (!string.IsNullOrEmpty(sourceUri) && IsExact(s, sourceUri) && parentKey == "namespaceURI") arr[i] = targetUri;
                }
            }
        }

        private static void RewriteEntryObject(
            JObject entry, string sourceWs, string sourceUri, string targetWs, string targetUri)
        {
            string key = (string)entry["@key"] ?? (string)entry["key"];
            JToken valueToken = entry["$"] ?? entry["value"];
            if (key == null || valueToken == null || valueToken.Type != JTokenType.String) return;
            var value = (string)valueToken;

            if (key == "namespace" && IsExact(value, sourceUri) && !string.IsNullOrEmpty(sourceUri))
            {
                entry["$"] = targetUri;
                return;
            }
            if (key == "Schema" && IsExact(value, sourceWs)) entry["$"] = targetWs;
        }

        private static void RewritePlainConnectionParams(
            JObject cp, string sourceWs, string sourceUri, string targetWs, string targetUri)
        {
            foreach (var prop in cp.Properties())
            {
                if (prop.Value.Type != JTokenType.String) continue;
                var value = (string)prop.Value;
                if (prop.Name == "namespace" && IsExact(value, sourceUri)) prop.Value = targetUri;
                else if (prop.Name == "Schema" && IsExact(value, sourceWs)) prop.Value = targetWs;
            }
        }

        private static bool IsExact(string value, string source)
        {
            return !string.IsNullOrEmpty(source) && string.Equals(value, source, StringComparison.Ordinal);
        }

        private static bool IsQualified(string value, string sourceWs)
        {
            return !string.IsNullOrEmpty(sourceWs)
                && value != null
                && (value == sourceWs || value.StartsWith(sourceWs + ":", StringComparison.Ordinal));
        }

        private static void RemovePath(JObject raw, string path)
        {
            // 仅移除顶层键（导入载荷不需要嵌套清理）
            raw?.Property(path)?.Remove();
        }

        // ================= REST 小工具 =================

        private async Task<string> TryGetVersionAsync()
        {
            try
            {
                var json = await Http.GetAsync("/rest/about/version.json");
                var v = Root(json, "version");
                return (string)(v?["release"] ?? v?["git"]);
            }
            catch
            {
                return null;
            }
        }

        private async Task<JArray> ListRootAsync(string path, string listRoot, string itemKey,
            WorkspaceManifest manifest, string context)
        {
            try
            {
                var json = await Http.GetAsync(path);
                var root = Root(json, listRoot);
                return root?[itemKey] as JArray ?? new JArray();
            }
            catch (Exception ex)
            {
                manifest.Warnings.Add("list " + context + ": " + Describe(ex));
                return new JArray();
            }
        }

        private async Task<JObject> GetDetailAsync(string path, string itemKey,
            WorkspaceManifest manifest, string context)
        {
            try
            {
                var json = await Http.GetAsync(path);
                return Root(json, itemKey);
            }
            catch (Exception ex)
            {
                manifest.Warnings.Add("get " + context + ": " + Describe(ex));
                return null;
            }
        }

        private async Task CaptureSldAsync(string sldPath, string memberName, StyleEntry entry,
            Dictionary<string, string> slds, WorkspaceManifest manifest)
        {
            try
            {
                var sld = await Http.GetAsync(sldPath);
                if (!string.IsNullOrEmpty(sld))
                {
                    entry.SldPath = memberName;
                    slds[memberName] = sld;
                }
                else
                {
                    manifest.Warnings.Add("style " + entry.Name + ": SLD 内容为空");
                }
            }
            catch (Exception ex)
            {
                manifest.Warnings.Add("style " + entry.Name + ": " + Describe(ex));
            }
        }

        private async Task<JObject> GetOrNullAsync(string path, string rootKey)
        {
            try
            {
                var json = await Http.GetAsync(path);
                return Root(json, rootKey);
            }
            catch (GeoServerRequestException ex) when (ex.StatusCode == 404)
            {
                return null;
            }
        }

        private async Task<bool> ExistsAsync(string getPath)
        {
            try
            {
                await Http.GetAsync(getPath);
                return true;
            }
            catch (GeoServerRequestException ex) when (ex.StatusCode == 404)
            {
                return false;
            }
        }

        private Task PostJsonAsync(string path, string rootKey, JObject payload)
        {
            return PostJsonRaw(path, "{\"" + rootKey + "\":" + payload.ToString(Formatting.None) + "}");
        }

        private Task PutJsonAsync(string path, string rootKey, JObject payload)
        {
            return PutJsonRaw(path, "{\"" + rootKey + "\":" + payload.ToString(Formatting.None) + "}");
        }

        private async Task PostJsonRaw(string path, string json)
        {
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                await Http.PostAsync(path, content);
            }
        }

        private async Task PutJsonRaw(string path, string json)
        {
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                await Http.PutAsync(path, content);
            }
        }

        private static JObject Root(string json, string key)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            var parsed = JObject.Parse(json);
            return parsed[key] as JObject;
        }

        private static string EscPath(string value)
        {
            return Uri.EscapeDataString(value ?? string.Empty);
        }

        private static string Describe(Exception ex)
        {
            var gs = ex as GeoServerRequestException;
            if (gs == null) return ex.Message;
            var text = gs.ResponseContent;
            if (string.IsNullOrWhiteSpace(text)) return "HTTP " + gs.StatusCode;
            text = text.Trim();
            return "HTTP " + gs.StatusCode + "：" + (text.Length > 200 ? text.Substring(0, 200) + "…" : text);
        }

        // ================= 步骤执行 =================

        private sealed class StepOutcome
        {
            public MigrationItemStatus Status;
            public string Message;
        }

        private static StepOutcome ToSkipped(string message)
        {
            return new StepOutcome { Status = MigrationItemStatus.Skipped, Message = message };
        }

        private static StepOutcome ToFailed(string message)
        {
            return new StepOutcome { Status = MigrationItemStatus.Failed, Message = message };
        }

        private async Task Step(WorkspaceImportResult result, string step, string target,
            Func<Task<StepOutcome>> op)
        {
            var item = new MigrationItemResult { Step = step, Target = target };
            try
            {
                var outcome = await op();
                if (outcome == null)
                {
                    item.Status = MigrationItemStatus.Created;
                }
                else
                {
                    item.Status = outcome.Status;
                    item.Message = outcome.Message;
                }
            }
            catch (Exception ex)
            {
                item.Status = MigrationItemStatus.Failed;
                item.Message = Describe(ex);
            }
            result.Items.Add(item);
        }
    }
}
