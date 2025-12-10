using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示 GeoServer WMS 存储（级联 WMS）
    /// </summary>
    public class WMSStore
    {
        /// <summary>
        /// 获取或设置 WMS 存储名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置 WMS 存储类型
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// 获取或设置 WMS 存储是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置工作空间引用
        /// </summary>
        [JsonProperty("workspace")]
        public WorkspaceReference Workspace { get; set; }

        /// <summary>
        /// 获取或设置 WMS 存储描述
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// 获取或设置远程 WMS 的能力 URL
        /// </summary>
        [JsonProperty("capabilitiesURL")]
        public string CapabilitiesURL { get; set; }

        /// <summary>
        /// 获取或设置认证用户名
        /// </summary>
        [JsonProperty("username")]
        public string Username { get; set; }

        /// <summary>
        /// 获取或设置认证密码
        /// </summary>
        [JsonProperty("password")]
        public string Password { get; set; }

        /// <summary>
        /// 获取或设置最大并发连接数
        /// </summary>
        [JsonProperty("maxConnections")]
        public int? MaxConnections { get; set; }

        /// <summary>
        /// 获取或设置读取超时（秒）
        /// </summary>
        [JsonProperty("readTimeout")]
        public int? ReadTimeout { get; set; }

        /// <summary>
        /// 获取或设置连接超时（秒）
        /// </summary>
        [JsonProperty("connectTimeout")]
        public int? ConnectTimeout { get; set; }

        /// <summary>
        /// 获取或设置 WMS 存储的链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 单个 WMS 存储响应的包装器
    /// </summary>
    public class WMSStoreWrapper
    {
        /// <summary>
        /// 获取或设置 WMS 存储
        /// </summary>
        [JsonProperty("wmsStore")]
        public WMSStore WMSStore { get; set; }
    }

    /// <summary>
    /// WMS 存储列表响应的包装器
    /// </summary>
    public class WMSStoreListWrapper
    {
        /// <summary>
        /// 获取或设置 WMS 存储列表
        /// </summary>
        [JsonProperty("wmsStores")]
        public WMSStoreList WMSStoreList { get; set; }
    }

    /// <summary>
    /// 表示 WMS 存储列表
    /// </summary>
    public class WMSStoreList
    {
        /// <summary>
        /// 获取或设置 WMS 存储数组
        /// </summary>
        [JsonProperty("wmsStore")]
        public WMSStore[] WMSStores { get; set; }
    }

    /// <summary>
    /// 表示 GeoServer WMS 图层（级联 WMS 图层）
    /// </summary>
    public class WMSLayer
    {
        /// <summary>
        /// 获取或设置 WMS 图层名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置来自远程 WMS 的原生名称
        /// </summary>
        [JsonProperty("nativeName")]
        public string NativeName { get; set; }

        /// <summary>
        /// 获取或设置命名空间引用
        /// </summary>
        [JsonProperty("namespace")]
        public NamespaceReference Namespace { get; set; }

        /// <summary>
        /// 获取或设置 WMS 图层标题
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 获取或设置 WMS 图层描述
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// 获取或设置摘要
        /// </summary>
        [JsonProperty("abstract")]
        public string Abstract { get; set; }

        /// <summary>
        /// 获取或设置关键字
        /// </summary>
        [JsonProperty("keywords")]
        public KeywordInfo Keywords { get; set; }

        /// <summary>
        /// 获取或设置原生坐标参考系统
        /// </summary>
        [JsonProperty("nativeCRS")]
        public string NativeCRS { get; set; }

        /// <summary>
        /// 获取或设置空间参考系统
        /// </summary>
        [JsonProperty("srs")]
        public string Srs { get; set; }

        /// <summary>
        /// 获取或设置原生边界框
        /// </summary>
        [JsonProperty("nativeBoundingBox")]
        public BoundingBox NativeBoundingBox { get; set; }

        /// <summary>
        /// 获取或设置经纬度边界框
        /// </summary>
        [JsonProperty("latLonBoundingBox")]
        public BoundingBox LatLonBoundingBox { get; set; }

        /// <summary>
        /// 获取或设置 WMS 图层是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置存储引用
        /// </summary>
        [JsonProperty("store")]
        public StoreReference Store { get; set; }

        /// <summary>
        /// 获取或设置 WMS 图层的链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 单个 WMS 图层响应的包装器
    /// </summary>
    public class WMSLayerWrapper
    {
        /// <summary>
        /// 获取或设置 WMS 图层
        /// </summary>
        [JsonProperty("wmsLayer")]
        public WMSLayer WMSLayer { get; set; }
    }

    /// <summary>
    /// WMS 图层列表响应的包装器
    /// </summary>
    public class WMSLayerListWrapper
    {
        /// <summary>
        /// 获取或设置 WMS 图层列表
        /// </summary>
        [JsonProperty("wmsLayers")]
        public WMSLayerList WMSLayerList { get; set; }
    }

    /// <summary>
    /// 表示 WMS 图层列表
    /// </summary>
    public class WMSLayerList
    {
        /// <summary>
        /// 获取或设置 WMS 图层数组
        /// </summary>
        [JsonProperty("wmsLayer")]
        public WMSLayer[] WMSLayers { get; set; }
    }
}
