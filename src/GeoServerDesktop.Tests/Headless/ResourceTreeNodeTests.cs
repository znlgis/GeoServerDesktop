using GeoServerDesktop.App.Models;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// ResourceTreeNode 纯 POCO 离线测试：CanHaveChildren 语义与 Children 默认值。
    /// </summary>
    public sealed class ResourceTreeNodeTests
    {
        [Theory]
        [InlineData(ResourceType.GeoServer, true)]
        [InlineData(ResourceType.WorkspacesContainer, true)]
        [InlineData(ResourceType.Workspace, true)]
        [InlineData(ResourceType.DataStoresContainer, true)]
        [InlineData(ResourceType.DataStore, true)]
        [InlineData(ResourceType.LayersContainer, true)]
        [InlineData(ResourceType.Layer, false)]           // 叶子：不可有子节点
        [InlineData(ResourceType.StylesContainer, true)]
        [InlineData(ResourceType.Style, false)]           // 叶子：不可有子节点
        [InlineData(ResourceType.LayerGroupsContainer, true)]
        [InlineData(ResourceType.LayerGroup, true)]
        public void CanHaveChildren_MatchesType(ResourceType type, bool expected)
        {
            var node = new ResourceTreeNode { Type = type };
            Assert.Equal(expected, node.CanHaveChildren);
        }

        [Fact]
        public void NewNode_ChildrenEmpty_AndNameDefaultsEmpty()
        {
            var node = new ResourceTreeNode();
            Assert.NotNull(node.Children);
            Assert.Empty(node.Children);
            Assert.Equal(string.Empty, node.Name);
            Assert.False(node.IsExpanded);
            Assert.False(node.IsSelected);
            Assert.Null(node.Tag);
        }

        [Fact]
        public void Children_CanAddNodes()
        {
            var parent = new ResourceTreeNode { Name = "parent", Type = ResourceType.Workspace };
            parent.Children.Add(new ResourceTreeNode { Name = "child", Type = ResourceType.DataStoresContainer });

            Assert.Single(parent.Children);
            Assert.Equal("child", parent.Children[0].Name);
        }
    }
}
