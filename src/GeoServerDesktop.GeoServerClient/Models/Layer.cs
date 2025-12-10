using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Represents a GeoServer layer
    /// </summary>
    public class Layer
    {
        /// <summary>
        /// Name of the layer
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Type of the layer (e.g., VECTOR, RASTER)
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Default style for the layer
        /// </summary>
        [JsonProperty("defaultStyle")]
        public StyleReference DefaultStyle { get; set; }

        /// <summary>
        /// Resource associated with the layer
        /// </summary>
        [JsonProperty("resource")]
        public ResourceReference Resource { get; set; }

        /// <summary>
        /// Link to the layer resource
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Reference to a style
    /// </summary>
    public class StyleReference
    {
        /// <summary>
        /// Name of the style
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Link to the style
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Reference to a resource (feature type or coverage)
    /// </summary>
    public class ResourceReference
    {
        /// <summary>
        /// Class of the resource
        /// </summary>
        [JsonProperty("@class")]
        public string Class { get; set; }

        /// <summary>
        /// Name of the resource
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Link to the resource
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Wrapper for a single layer response
    /// </summary>
    public class LayerWrapper
    {
        /// <summary>
        /// The layer data
        /// </summary>
        [JsonProperty("layer")]
        public Layer Layer { get; set; }
    }

    /// <summary>
    /// List of layers
    /// </summary>
    public class LayerList
    {
        /// <summary>
        /// Array of layers
        /// </summary>
        [JsonProperty("layer")]
        public Layer[] Layers { get; set; }
    }

    /// <summary>
    /// Wrapper for layer list response
    /// </summary>
    public class LayerListWrapper
    {
        /// <summary>
        /// The layer list data
        /// </summary>
        [JsonProperty("layers")]
        public LayerList LayerList { get; set; }
    }
}
