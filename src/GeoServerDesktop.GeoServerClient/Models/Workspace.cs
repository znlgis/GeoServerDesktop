using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示 GeoServer 工作空间
    /// </summary>
    public class Workspace
    {
        /// <summary>
        /// 工作空间的名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 此工作空间是否隔离
        /// </summary>
        [JsonProperty("isolated")]
        public bool? Isolated { get; set; }

        /// <summary>
        /// 工作空间资源的链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 单个工作空间响应的包装器
    /// </summary>
    public class WorkspaceWrapper
    {
        /// <summary>
        /// 工作空间数据
        /// </summary>
        [JsonProperty("workspace")]
        public Workspace Workspace { get; set; }
    }

    /// <summary>
    /// 工作空间列表
    /// </summary>
    public class WorkspaceList
    {
        /// <summary>
        /// 工作空间数组
        /// </summary>
        [JsonProperty("workspace")]
        public Workspace[] Workspaces { get; set; }
    }

    /// <summary>
    /// 工作空间列表响应的包装器
    /// </summary>
    public class WorkspaceListWrapper
    {
        /// <summary>
        /// 工作空间列表数据
        /// </summary>
        [JsonProperty("workspaces")]
        public WorkspaceList WorkspaceList { get; set; }
    }
}
