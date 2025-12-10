using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示 GeoServer 图层组
    /// </summary>
    public class LayerGroup
    {
        /// <summary>
        /// 图层组的名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 图层组的模式
        /// </summary>
        [JsonProperty("mode")]
        public string Mode { get; set; }

        /// <summary>
        /// 图层组的标题
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 图层组的摘要/描述
        /// </summary>
        [JsonProperty("abstractTxt")]
        public string Abstract { get; set; }

        /// <summary>
        /// 图层组所属的工作空间
        /// </summary>
        [JsonProperty("workspace")]
        public WorkspaceReference Workspace { get; set; }

        /// <summary>
        /// 组中的可发布项（图层）
        /// </summary>
        [JsonProperty("publishables")]
        public PublishableList Publishables { get; set; }

        /// <summary>
        /// 图层组资源的链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 可发布项（图层/图层组）列表
    /// </summary>
    public class PublishableList
    {
        /// <summary>
        /// 已发布项数组
        /// </summary>
        [JsonProperty("published")]
        public PublishedItem[] Published { get; set; }
    }

    /// <summary>
    /// 已发布的项（图层或图层组）
    /// </summary>
    public class PublishedItem
    {
        /// <summary>
        /// 已发布项的类型
        /// </summary>
        [JsonProperty("@type")]
        public string Type { get; set; }

        /// <summary>
        /// 已发布项的名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 已发布项的链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 单个图层组响应的包装器
    /// </summary>
    public class LayerGroupWrapper
    {
        /// <summary>
        /// 图层组数据
        /// </summary>
        [JsonProperty("layerGroup")]
        public LayerGroup LayerGroup { get; set; }
    }

    /// <summary>
    /// List of layer groups
    /// </summary>
    public class LayerGroupList
    {
        /// <summary>
        /// Array of layer groups
        /// </summary>
        [JsonProperty("layerGroup")]
        public LayerGroup[] LayerGroups { get; set; }
    }

    /// <summary>
    /// Wrapper for layer group list response
    /// </summary>
    public class LayerGroupListWrapper
    {
        /// <summary>
        /// The layer group list data
        /// </summary>
        [JsonProperty("layerGroups")]
        public LayerGroupList LayerGroupList { get; set; }
    }
}
