using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Represents a GeoServer layer group
    /// </summary>
    public class LayerGroup
    {
        /// <summary>
        /// Name of the layer group
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Mode of the layer group
        /// </summary>
        [JsonProperty("mode")]
        public string Mode { get; set; }

        /// <summary>
        /// Title of the layer group
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Abstract/description of the layer group
        /// </summary>
        [JsonProperty("abstractTxt")]
        public string Abstract { get; set; }

        /// <summary>
        /// Workspace the layer group belongs to
        /// </summary>
        [JsonProperty("workspace")]
        public WorkspaceReference Workspace { get; set; }

        /// <summary>
        /// Publishables (layers) in the group
        /// </summary>
        [JsonProperty("publishables")]
        public PublishableList Publishables { get; set; }

        /// <summary>
        /// Link to the layer group resource
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// List of publishables (layers/layer groups)
    /// </summary>
    public class PublishableList
    {
        /// <summary>
        /// Array of published items
        /// </summary>
        [JsonProperty("published")]
        public PublishedItem[] Published { get; set; }
    }

    /// <summary>
    /// A published item (layer or layer group)
    /// </summary>
    public class PublishedItem
    {
        /// <summary>
        /// Type of the published item
        /// </summary>
        [JsonProperty("@type")]
        public string Type { get; set; }

        /// <summary>
        /// Name of the published item
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Link to the published item
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Wrapper for a single layer group response
    /// </summary>
    public class LayerGroupWrapper
    {
        /// <summary>
        /// The layer group data
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
