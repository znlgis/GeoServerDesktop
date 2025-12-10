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
        /// Parameter key
        /// </summary>
        [JsonProperty("@key")]
        public string Key { get; set; }

        /// <summary>
        /// Parameter value
        /// </summary>
        [JsonProperty("$")]
        public string Value { get; set; }
    }

    /// <summary>
    /// Wrapper for a single data store response
    /// </summary>
    public class DataStoreWrapper
    {
        /// <summary>
        /// The data store data
        /// </summary>
        [JsonProperty("dataStore")]
        public DataStore DataStore { get; set; }
    }

    /// <summary>
    /// List of data stores
    /// </summary>
    public class DataStoreList
    {
        /// <summary>
        /// Array of data stores
        /// </summary>
        [JsonProperty("dataStore")]
        public DataStore[] DataStores { get; set; }
    }

    /// <summary>
    /// Wrapper for data store list response
    /// </summary>
    public class DataStoreListWrapper
    {
        /// <summary>
        /// The data store list data
        /// </summary>
        [JsonProperty("dataStores")]
        public DataStoreList DataStoreList { get; set; }
    }
}
