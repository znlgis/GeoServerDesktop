using System.Collections.ObjectModel;

namespace GeoServerDesktop.App.Models
{
    /// <summary>
    /// Represents a node in the GeoServer resource tree
    /// </summary>
    public class ResourceTreeNode
    {
        /// <summary>
        /// Display name of the node
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Type of resource
        /// </summary>
        public ResourceType Type { get; set; }

        /// <summary>
        /// Child nodes
        /// </summary>
        public ObservableCollection<ResourceTreeNode> Children { get; set; } = new ObservableCollection<ResourceTreeNode>();

        /// <summary>
        /// Whether the node can have children
        /// </summary>
        public bool CanHaveChildren => Type != ResourceType.Layer && Type != ResourceType.Style;

        /// <summary>
        /// Whether the node is expanded
        /// </summary>
        public bool IsExpanded { get; set; }

        /// <summary>
        /// Whether the node is selected
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Additional data associated with the node
        /// </summary>
        public object? Tag { get; set; }
    }

    /// <summary>
    /// Types of resources in the tree
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// Root GeoServer node
        /// </summary>
        GeoServer,

        /// <summary>
        /// Workspaces container
        /// </summary>
        WorkspacesContainer,

        /// <summary>
        /// Individual workspace
        /// </summary>
        Workspace,

        /// <summary>
        /// Data stores container
        /// </summary>
        DataStoresContainer,

        /// <summary>
        /// Individual data store
        /// </summary>
        DataStore,

        /// <summary>
        /// Layers container
        /// </summary>
        LayersContainer,

        /// <summary>
        /// Individual layer
        /// </summary>
        Layer,

        /// <summary>
        /// Styles container
        /// </summary>
        StylesContainer,

        /// <summary>
        /// Individual style
        /// </summary>
        Style,

        /// <summary>
        /// Layer groups container
        /// </summary>
        LayerGroupsContainer,

        /// <summary>
        /// Individual layer group
        /// </summary>
        LayerGroup
    }
}
