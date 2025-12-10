using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Represents a GeoServer namespace
    /// </summary>
    public class Namespace
    {
        /// <summary>
        /// Gets or sets the namespace prefix
        /// </summary>
        [JsonProperty("prefix")]
        public string Prefix { get; set; }

        /// <summary>
        /// Gets or sets the namespace URI
        /// </summary>
        [JsonProperty("uri")]
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets whether this is the default namespace
        /// </summary>
        [JsonProperty("isolated")]
        public bool? Isolated { get; set; }

        /// <summary>
        /// Gets or sets the namespace href (link)
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Wrapper for a single namespace response
    /// </summary>
    public class NamespaceWrapper
    {
        /// <summary>
        /// Gets or sets the namespace
        /// </summary>
        [JsonProperty("namespace")]
        public Namespace Namespace { get; set; }
    }

    /// <summary>
    /// Wrapper for namespace list response
    /// </summary>
    public class NamespaceListWrapper
    {
        /// <summary>
        /// Gets or sets the namespace list
        /// </summary>
        [JsonProperty("namespaces")]
        public NamespaceList NamespaceList { get; set; }
    }

    /// <summary>
    /// Represents a list of namespaces
    /// </summary>
    public class NamespaceList
    {
        /// <summary>
        /// Gets or sets the array of namespaces
        /// </summary>
        [JsonProperty("namespace")]
        public Namespace[] Namespaces { get; set; }
    }
}
