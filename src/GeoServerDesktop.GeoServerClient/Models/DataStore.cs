using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示 GeoServer 数据存储
    /// </summary>
    public class DataStore
    {
        /// <summary>
        /// 数据存储的名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 数据存储的类型（例如 PostGIS、Shapefile）
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// 数据存储是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 数据存储所属的工作空间
        /// </summary>
        [JsonProperty("workspace")]
        public WorkspaceReference Workspace { get; set; }

        /// <summary>
        /// 数据存储的连接参数
        /// </summary>
        [JsonProperty("connectionParameters")]
        public ConnectionParameters ConnectionParameters { get; set; }

        /// <summary>
        /// 数据存储资源的链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 对工作空间的引用
    /// </summary>
    public class WorkspaceReference
    {
        /// <summary>
        /// 工作空间的名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 工作空间的链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 数据存储的连接参数
    /// </summary>
    public class ConnectionParameters
    {
        /// <summary>
        /// 连接参数的条目集合
        /// </summary>
        [JsonProperty("entry")]
        public ConnectionParameterEntry[] Entries { get; set; }
    }

    /// <summary>
    /// 单个连接参数条目
    /// </summary>
    public class ConnectionParameterEntry
    {
        /// <summary>
        /// 参数键
        /// </summary>
        [JsonProperty("@key")]
        public string Key { get; set; }

        /// <summary>
        /// 参数值
        /// </summary>
        [JsonProperty("$")]
        public string Value { get; set; }
    }

    /// <summary>
    /// 单个数据存储响应的包装器
    /// </summary>
    public class DataStoreWrapper
    {
        /// <summary>
        /// 数据存储数据
        /// </summary>
        [JsonProperty("dataStore")]
        public DataStore DataStore { get; set; }
    }

    /// <summary>
    /// 数据存储列表
    /// </summary>
    public class DataStoreList
    {
        /// <summary>
        /// 数据存储数组
        /// </summary>
        [JsonProperty("dataStore")]
        public DataStore[] DataStores { get; set; }
    }

    /// <summary>
    /// 数据存储列表响应的包装器
    /// </summary>
    public class DataStoreListWrapper
    {
        /// <summary>
        /// 数据存储列表数据
        /// </summary>
        [JsonProperty("dataStores")]
        public DataStoreList DataStoreList { get; set; }
    }
}
