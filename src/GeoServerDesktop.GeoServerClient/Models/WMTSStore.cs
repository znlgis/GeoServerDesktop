using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示 GeoServer WMTS 存储（级联 WMTS 服务）
    /// </summary>
    public class WMTSStore
    {
        /// <summary>
        /// 获取或设置 WMTS 存储名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置 WMTS 存储类型
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// 获取或设置 WMTS 存储是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置工作空间引用
        /// </summary>
        [JsonProperty("workspace")]
        public WorkspaceReference Workspace { get; set; }

        /// <summary>
        /// 获取或设置 WMTS 存储描述
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// 获取或设置远程 WMTS 服务的能力 URL
        /// </summary>
        [JsonProperty("capabilitiesURL")]
        public string CapabilitiesUrl { get; set; }

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
        /// 获取或设置最大连接数
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
        /// 获取或设置 WMTS 存储的链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 单个 WMTS 存储响应的包装器
    /// </summary>
    public class WMTSStoreWrapper
    {
        /// <summary>
        /// 获取或设置 WMTS 存储
        /// </summary>
        [JsonProperty("wmtsStore")]
        public WMTSStore WMTSStore { get; set; }
    }

    /// <summary>
    /// WMTS 存储列表响应的包装器
    /// </summary>
    public class WMTSStoreListWrapper
    {
        /// <summary>
        /// 获取或设置 WMTS 存储列表
        /// </summary>
        [JsonProperty("wmtsStores")]
        public WMTSStoreList WMTSStoreList { get; set; }
    }

    /// <summary>
    /// 表示 WMTS 存储列表
    /// </summary>
    public class WMTSStoreList
    {
        /// <summary>
        /// 获取或设置 WMTS 存储数组
        /// </summary>
        [JsonProperty("wmtsStore")]
        public WMTSStore[] WMTSStores { get; set; }
    }

    /// <summary>
    /// 表示 GeoServer WMTS 图层（来自级联 WMTS 服务的图层）
    /// </summary>
    public class WMTSLayer
    {
        /// <summary>
        /// 获取或设置 WMTS 图层名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置原生名称（远程 WMTS 服务中的图层名称）
        /// </summary>
        [JsonProperty("nativeName")]
        public string NativeName { get; set; }

        /// <summary>
        /// 获取或设置命名空间引用
        /// </summary>
        [JsonProperty("namespace")]
        public NamespaceReference Namespace { get; set; }

        /// <summary>
        /// 获取或设置 WMTS 图层标题
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 获取或设置 WMTS 图层描述
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// 获取或设置 WMTS 图层摘要
        /// </summary>
        [JsonProperty("abstract")]
        public string Abstract { get; set; }

        /// <summary>
        /// 获取或设置 WMTS 图层是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置 WMTS 图层是否公开
        /// </summary>
        [JsonProperty("advertised")]
        public bool? Advertised { get; set; }

        /// <summary>
        /// 获取或设置存储引用
        /// </summary>
        [JsonProperty("store")]
        public StoreReference Store { get; set; }

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
        /// 获取或设置空间参考系统
        /// </summary>
        [JsonProperty("srs")]
        public string Srs { get; set; }

        /// <summary>
        /// 获取或设置 WMTS 图层的链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 单个 WMTS 图层响应的包装器
    /// </summary>
    public class WMTSLayerWrapper
    {
        /// <summary>
        /// 获取或设置 WMTS 图层
        /// </summary>
        [JsonProperty("wmtsLayer")]
        public WMTSLayer WMTSLayer { get; set; }
    }

    /// <summary>
    /// WMTS 图层列表响应的包装器
    /// </summary>
    public class WMTSLayerListWrapper
    {
        /// <summary>
        /// 获取或设置 WMTS 图层列表
        /// </summary>
        [JsonProperty("wmtsLayers")]
        public WMTSLayerList WMTSLayerList { get; set; }
    }

    /// <summary>
    /// 表示 WMTS 图层列表
    /// </summary>
    public class WMTSLayerList
    {
        /// <summary>
        /// 获取或设置 WMTS 图层数组
        /// </summary>
        [JsonProperty("wmtsLayer")]
        public WMTSLayer[] WMTSLayers { get; set; }
    }
}
