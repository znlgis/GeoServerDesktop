using System.Collections.ObjectModel;

namespace GeoServerDesktop.App.Models
{
    /// <summary>
    /// 表示 GeoServer 资源树中的节点
    /// </summary>
    public class ResourceTreeNode
    {
        /// <summary>
        /// 节点的显示名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 资源的类型
        /// </summary>
        public ResourceType Type { get; set; }

        /// <summary>
        /// 子节点集合
        /// </summary>
        public ObservableCollection<ResourceTreeNode> Children { get; set; } = new ObservableCollection<ResourceTreeNode>();

        /// <summary>
        /// 节点是否可以有子节点
        /// </summary>
        public bool CanHaveChildren => Type != ResourceType.Layer && Type != ResourceType.Style;

        /// <summary>
        /// 节点是否展开
        /// </summary>
        public bool IsExpanded { get; set; }

        /// <summary>
        /// 节点是否被选中
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 与节点关联的附加数据
        /// </summary>
        public object? Tag { get; set; }
    }

    /// <summary>
    /// 树中的资源类型
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// 根 GeoServer 节点
        /// </summary>
        GeoServer,

        /// <summary>
        /// 工作空间容器
        /// </summary>
        WorkspacesContainer,

        /// <summary>
        /// 单个工作空间
        /// </summary>
        Workspace,

        /// <summary>
        /// 数据存储容器
        /// </summary>
        DataStoresContainer,

        /// <summary>
        /// 单个数据存储
        /// </summary>
        DataStore,

        /// <summary>
        /// 图层容器
        /// </summary>
        LayersContainer,

        /// <summary>
        /// 单个图层
        /// </summary>
        Layer,

        /// <summary>
        /// 样式容器
        /// </summary>
        StylesContainer,

        /// <summary>
        /// 单个样式
        /// </summary>
        Style,

        /// <summary>
        /// 图层组容器
        /// </summary>
        LayerGroupsContainer,

        /// <summary>
        /// 单个图层组
        /// </summary>
        LayerGroup
    }
}
