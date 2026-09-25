using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace GeoServerDesktop.GeoServerClient.Migration
{
    /// <summary>
    /// 工作空间迁移归档清单（M4，schema v1）。
    /// 归档包为 ZIP：根目录 manifest.json（本清单的序列化结果）+ styles/ 下的 .sld 文件。
    /// 资源详情以 REST 原样 JSON（raw）存储，导入时经引用重写（工作空间/命名空间/存储限定名替换、
    /// 剥离 href/id/日期等服务端字段）后回放到目标实例；依赖顺序：ws → namespace → store →
    /// style → featureType/coverage（自动发布图层）→ 图层样式绑定 → layerGroup。
    /// </summary>
    public sealed class WorkspaceManifest
    {
        /// <summary>当前支持的归档 schema 版本。</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>归档 schema 版本（导入时高于当前版本的归档被拒绝）。</summary>
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>归档创建时间（UTC ISO-8601）。</summary>
        public string CreatedAt { get; set; }

        /// <summary>源实例 GeoServer 版本（/rest/about/version.json 的 git hash 或 release，仅记录用途）。</summary>
        public string SourceVersion { get; set; }

        /// <summary>源工作空间名。</summary>
        public string Workspace { get; set; }

        /// <summary>源命名空间前缀（缺省与工作空间同名）。</summary>
        public string NamespacePrefix { get; set; }

        /// <summary>源命名空间 URI。</summary>
        public string NamespaceUri { get; set; }

        /// <summary>导出过程中的警告（单项资源读取失败等；资源仍会尽力记入清单）。</summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>数据存储（raw 为 GET dataStore 详情的 "dataStore" 对象）。</summary>
        public List<DataStoreEntry> DataStores { get; set; } = new List<DataStoreEntry>();

        /// <summary>覆盖存储（raw 为 GET coverageStore 详情的 "coverageStore" 对象）。</summary>
        public List<CoverageStoreEntry> CoverageStores { get; set; } = new List<CoverageStoreEntry>();

        /// <summary>要素类型（raw 为 GET featureType 详情的 "featureType" 对象）。</summary>
        public List<FeatureTypeEntry> FeatureTypes { get; set; } = new List<FeatureTypeEntry>();

        /// <summary>覆盖度（raw 为 GET coverage 详情的 "coverage" 对象）。</summary>
        public List<CoverageEntry> Coverages { get; set; } = new List<CoverageEntry>();

        /// <summary>样式（SLD 内容在归档包 styles/ 成员中，SldPath 指向成员名）。</summary>
        public List<StyleEntry> Styles { get; set; } = new List<StyleEntry>();

        /// <summary>图层默认样式绑定（图层资源由资源创建自动发布，此处仅还原绑定差异）。</summary>
        public List<LayerBindingEntry> LayerBindings { get; set; } = new List<LayerBindingEntry>();

        /// <summary>工作空间图层组（raw 为 GET layergroup 详情的 "layerGroup" 对象）。</summary>
        public List<LayerGroupEntry> LayerGroups { get; set; } = new List<LayerGroupEntry>();
    }

    /// <summary>归档条目基类：名称 + 原始 JSON。</summary>
    public abstract class ManifestEntryBase
    {
        /// <summary>资源名。</summary>
        public string Name { get; set; }

        /// <summary>REST 详情原样 JSON（对象形态）。</summary>
        public JObject Raw { get; set; }
    }

    /// <summary>数据存储条目。</summary>
    public sealed class DataStoreEntry : ManifestEntryBase
    {
        /// <summary>存储类型（Shapefile/PostGIS/JDBC 等，仅展示与判断用）。</summary>
        public string Type { get; set; }
    }

    /// <summary>覆盖存储条目。</summary>
    public sealed class CoverageStoreEntry : ManifestEntryBase
    {
        /// <summary>存储类型（GeoTIFF/ImageMosaic 等）。</summary>
        public string Type { get; set; }

        /// <summary>数据文件引用（file: 形态；目标实例须能解析该引用，迁移前先落数据盘）。</summary>
        public string Url { get; set; }
    }

    /// <summary>要素类型条目。</summary>
    public sealed class FeatureTypeEntry : ManifestEntryBase
    {
        /// <summary>所属数据存储名。</summary>
        public string Store { get; set; }

        /// <summary>原始数据名（磁盘/数据库对象名）。</summary>
        public string NativeName { get; set; }
    }

    /// <summary>覆盖度条目。</summary>
    public sealed class CoverageEntry : ManifestEntryBase
    {
        /// <summary>所属覆盖存储名。</summary>
        public string Store { get; set; }

        /// <summary>原始数据名。</summary>
        public string NativeName { get; set; }
    }

    /// <summary>样式条目。</summary>
    public sealed class StyleEntry
    {
        /// <summary>样式名。</summary>
        public string Name { get; set; }

        /// <summary>是否为全局样式（false 表示工作空间级样式）。</summary>
        public bool Global { get; set; }

        /// <summary>样式文件名（服务端 filename，可空）。</summary>
        public string Filename { get; set; }

        /// <summary>归档包内 SLD 成员名（styles/xxx.sld；读取失败时为空并记警告）。</summary>
        public string SldPath { get; set; }

        /// <summary>工作空间样式与某全局样式同名（导出时探测）：该全局样式 SLD 一并入包以保证可迁移性。</summary>
        public bool DedupSameNameGlobalStyle { get; set; }
    }

    /// <summary>图层默认样式绑定条目。</summary>
    public sealed class LayerBindingEntry
    {
        /// <summary>图层 qualified 名（workspace:layer）。</summary>
        public string QualifiedLayer { get; set; }

        /// <summary>默认样式名。</summary>
        public string Style { get; set; }

        /// <summary>默认样式是否为工作空间级样式。</summary>
        public bool WorkspaceStyle { get; set; }
    }

    /// <summary>图层组条目。</summary>
    public sealed class LayerGroupEntry : ManifestEntryBase
    {
    }
}
