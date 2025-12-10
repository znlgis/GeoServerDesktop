using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示 GeoServer 覆盖范围存储（栅格数据存储）
    /// </summary>
    public class CoverageStore
    {
        /// <summary>
        /// 获取或设置覆盖范围存储名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置覆盖范围存储类型
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// 获取或设置覆盖范围存储是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置工作空间引用
        /// </summary>
        [JsonProperty("workspace")]
        public WorkspaceReference Workspace { get; set; }

        /// <summary>
        /// 获取或设置覆盖范围存储描述
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// 获取或设置覆盖范围存储资源的 URL
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// 获取或设置覆盖范围存储的连接参数
        /// </summary>
        [JsonProperty("connectionParameters")]
        public ConnectionParameters ConnectionParameters { get; set; }

        /// <summary>
        /// 获取或设置覆盖范围存储链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 单个覆盖范围存储响应的包装器
    /// </summary>
    public class CoverageStoreWrapper
    {
        /// <summary>
        /// 获取或设置覆盖范围存储数据
        /// </summary>
        [JsonProperty("coverageStore")]
        public CoverageStore CoverageStore { get; set; }
    }

    /// <summary>
    /// 覆盖范围存储列表响应的包装器
    /// </summary>
    public class CoverageStoreListWrapper
    {
        /// <summary>
        /// 获取或设置覆盖范围存储列表数据
        /// </summary>
        [JsonProperty("coverageStores")]
        public CoverageStoreList CoverageStoreList { get; set; }
    }

    /// <summary>
    /// 表示覆盖范围存储列表
    /// </summary>
    public class CoverageStoreList
    {
        /// <summary>
        /// 获取或设置覆盖范围存储数组
        /// </summary>
        [JsonProperty("coverageStore")]
        public CoverageStore[] CoverageStores { get; set; }
    }

    /// <summary>
    /// 表示 GeoServer 覆盖范围（栅格图层）
    /// </summary>
    public class Coverage
    {
        /// <summary>
        /// 获取或设置覆盖范围名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置原始名称
        /// </summary>
        [JsonProperty("nativeName")]
        public string NativeName { get; set; }

        /// <summary>
        /// 获取或设置命名空间引用
        /// </summary>
        [JsonProperty("namespace")]
        public NamespaceReference Namespace { get; set; }

        /// <summary>
        /// 获取或设置覆盖范围标题
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 获取或设置覆盖范围描述
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// 获取或设置覆盖范围摘要
        /// </summary>
        [JsonProperty("abstract")]
        public string Abstract { get; set; }

        /// <summary>
        /// 获取或设置关键字
        /// </summary>
        [JsonProperty("keywords")]
        public KeywordInfo Keywords { get; set; }

        /// <summary>
        /// 获取或设置原始坐标参考系统
        /// </summary>
        [JsonProperty("nativeCRS")]
        public string NativeCRS { get; set; }

        /// <summary>
        /// 获取或设置空间参考系统（SRS）
        /// </summary>
        [JsonProperty("srs")]
        public string Srs { get; set; }

        /// <summary>
        /// 获取或设置原始边界框
        /// </summary>
        [JsonProperty("nativeBoundingBox")]
        public BoundingBox NativeBoundingBox { get; set; }

        /// <summary>
        /// 获取或设置经纬度边界框
        /// </summary>
        [JsonProperty("latLonBoundingBox")]
        public BoundingBox LatLonBoundingBox { get; set; }

        /// <summary>
        /// 获取或设置覆盖范围是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置存储引用
        /// </summary>
        [JsonProperty("store")]
        public StoreReference Store { get; set; }

        /// <summary>
        /// 获取或设置覆盖范围链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 单个覆盖范围响应的包装器
    /// </summary>
    public class CoverageWrapper
    {
        /// <summary>
        /// 获取或设置覆盖范围数据
        /// </summary>
        [JsonProperty("coverage")]
        public Coverage Coverage { get; set; }
    }

    /// <summary>
    /// 覆盖范围列表响应的包装器
    /// </summary>
    public class CoverageListWrapper
    {
        /// <summary>
        /// 获取或设置覆盖范围列表数据
        /// </summary>
        [JsonProperty("coverages")]
        public CoverageList CoverageList { get; set; }
    }

    /// <summary>
    /// 表示覆盖范围列表
    /// </summary>
    public class CoverageList
    {
        /// <summary>
        /// 获取或设置覆盖范围数组
        /// </summary>
        [JsonProperty("coverage")]
        public Coverage[] Coverages { get; set; }
    }

    /// <summary>
    /// 表示结构化覆盖范围索引配置
    /// </summary>
    public class StructuredCoverageIndex
    {
        /// <summary>
        /// 获取或设置索引名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置架构属性
        /// </summary>
        [JsonProperty("schema")]
        public System.Collections.Generic.Dictionary<string, object> Schema { get; set; }
    }

    /// <summary>
    /// 表示颗粒信息
    /// </summary>
    public class Granule
    {
        /// <summary>
        /// 获取或设置颗粒 ID
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// 获取或设置要素类型
        /// </summary>
        [JsonProperty("fid")]
        public string Fid { get; set; }

        /// <summary>
        /// 获取或设置颗粒属性
        /// </summary>
        [JsonProperty("properties")]
        public System.Collections.Generic.Dictionary<string, object> Properties { get; set; }
    }

    /// <summary>
    /// 颗粒列表的包装器
    /// </summary>
    public class GranuleListWrapper
    {
        /// <summary>
        /// 获取或设置颗粒列表
        /// </summary>
        [JsonProperty("features")]
        public System.Collections.Generic.List<Granule> Granules { get; set; }

        /// <summary>
        /// 获取或设置要素总数
        /// </summary>
        [JsonProperty("totalFeatures")]
        public int? TotalFeatures { get; set; }
    }

    /// <summary>
    /// 表示覆盖范围视图配置
    /// </summary>
    public class CoverageView
    {
        /// <summary>
        /// 获取或设置覆盖范围视图名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置包络线（边界框）
        /// </summary>
        [JsonProperty("envelope")]
        public System.Collections.Generic.Dictionary<string, object> Envelope { get; set; }

        /// <summary>
        /// 获取或设置覆盖范围波段
        /// </summary>
        [JsonProperty("coverageBands")]
        public System.Collections.Generic.List<CoverageBand> CoverageBands { get; set; }
    }

    /// <summary>
    /// 表示覆盖范围波段
    /// </summary>
    public class CoverageBand
    {
        /// <summary>
        /// 获取或设置输入覆盖范围波段
        /// </summary>
        [JsonProperty("inputCoverageBands")]
        public System.Collections.Generic.List<InputCoverageBand> InputCoverageBands { get; set; }

        /// <summary>
        /// 获取或设置定义
        /// </summary>
        [JsonProperty("definition")]
        public string Definition { get; set; }

        /// <summary>
        /// 获取或设置索引
        /// </summary>
        [JsonProperty("index")]
        public int? Index { get; set; }

        /// <summary>
        /// 获取或设置组合类型
        /// </summary>
        [JsonProperty("compositionType")]
        public string CompositionType { get; set; }
    }

    /// <summary>
    /// 表示输入覆盖范围波段
    /// </summary>
    public class InputCoverageBand
    {
        /// <summary>
        /// 获取或设置覆盖范围名称
        /// </summary>
        [JsonProperty("coverageName")]
        public string CoverageName { get; set; }

        /// <summary>
        /// 获取或设置波段索引
        /// </summary>
        [JsonProperty("band")]
        public int? Band { get; set; }
    }

    /// <summary>
    /// 覆盖范围视图列表的包装器
    /// </summary>
    public class CoverageViewListWrapper
    {
        /// <summary>
        /// 获取或设置覆盖范围视图列表
        /// </summary>
        [JsonProperty("coverageViews")]
        public System.Collections.Generic.List<string> CoverageViews { get; set; }
    }

    /// <summary>
    /// 覆盖范围视图响应的包装器
    /// </summary>
    public class CoverageViewWrapper
    {
        /// <summary>
        /// 获取或设置覆盖范围视图数据
        /// </summary>
        [JsonProperty("coverageView")]
        public CoverageView CoverageView { get; set; }
    }
}
