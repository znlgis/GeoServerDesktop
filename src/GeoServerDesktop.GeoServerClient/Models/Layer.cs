using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示 GeoServer 图层
    /// </summary>
    public class Layer
    {
        /// <summary>
        /// 图层的名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 图层的类型（例如 VECTOR、RASTER）
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// 图层的默认样式
        /// </summary>
        [JsonProperty("defaultStyle")]
        public StyleReference DefaultStyle { get; set; }

        /// <summary>
        /// 与图层关联的资源
        /// </summary>
        [JsonProperty("resource")]
        public ResourceReference Resource { get; set; }

        /// <summary>
        /// 图层资源的链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 对样式的引用
    /// </summary>
    public class StyleReference
    {
        /// <summary>
        /// 样式的名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 样式的链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 对资源（要素类型或覆盖）的引用
    /// </summary>
    public class ResourceReference
    {
        /// <summary>
        /// 资源的类
        /// </summary>
        [JsonProperty("@class")]
        public string Class { get; set; }

        /// <summary>
        /// 资源的名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 资源的链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 单个图层响应的包装器
    /// </summary>
    public class LayerWrapper
    {
        /// <summary>
        /// 图层数据
        /// </summary>
        [JsonProperty("layer")]
        public Layer Layer { get; set; }
    }

    /// <summary>
    /// 图层列表
    /// </summary>
    public class LayerList
    {
        /// <summary>
        /// 图层数组
        /// </summary>
        [JsonProperty("layer")]
        public Layer[] Layers { get; set; }
    }

    /// <summary>
    /// 图层列表响应的包装器
    /// </summary>
    public class LayerListWrapper
    {
        /// <summary>
        /// 图层列表数据
        /// </summary>
        [JsonProperty("layers")]
        public LayerList LayerList { get; set; }
    }
}
