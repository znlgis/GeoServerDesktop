using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示 GeoServer 命名空间
    /// </summary>
    public class Namespace
    {
        /// <summary>
        /// 获取或设置命名空间前缀
        /// </summary>
        [JsonProperty("prefix")]
        public string Prefix { get; set; }

        /// <summary>
        /// 获取或设置命名空间 URI
        /// </summary>
        [JsonProperty("uri")]
        public string Uri { get; set; }

        /// <summary>
        /// 获取或设置此命名空间是否隔离
        /// </summary>
        [JsonProperty("isolated")]
        public bool? Isolated { get; set; }

        /// <summary>
        /// 获取或设置命名空间链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 单个命名空间响应的包装器
    /// </summary>
    public class NamespaceWrapper
    {
        /// <summary>
        /// 获取或设置命名空间
        /// </summary>
        [JsonProperty("namespace")]
        public Namespace Namespace { get; set; }
    }

    /// <summary>
    /// 命名空间列表响应的包装器
    /// </summary>
    public class NamespaceListWrapper
    {
        /// <summary>
        /// 获取或设置命名空间列表
        /// </summary>
        [JsonProperty("namespaces")]
        public NamespaceList NamespaceList { get; set; }
    }

    /// <summary>
    /// 表示命名空间列表
    /// </summary>
    public class NamespaceList
    {
        /// <summary>
        /// 获取或设置命名空间数组
        /// </summary>
        [JsonProperty("namespace")]
        public Namespace[] Namespaces { get; set; }
    }
}
