using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Represents a GeoServer WMS store (cascaded WMS)
    /// </summary>
    public class WMSStore
    {
        /// <summary>
        /// Gets or sets the WMS store name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the WMS store type
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets whether the WMS store is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the workspace reference
        /// </summary>
        [JsonProperty("workspace")]
        public WorkspaceReference Workspace { get; set; }

        /// <summary>
        /// Gets or sets the WMS store description
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the capabilities URL of the remote WMS
        /// </summary>
        [JsonProperty("capabilitiesURL")]
        public string CapabilitiesURL { get; set; }

        /// <summary>
        /// Gets or sets the username for authentication
        /// </summary>
        [JsonProperty("username")]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password for authentication
        /// </summary>
        [JsonProperty("password")]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of concurrent connections
        /// </summary>
        [JsonProperty("maxConnections")]
        public int? MaxConnections { get; set; }

        /// <summary>
        /// Gets or sets the read timeout in seconds
        /// </summary>
        [JsonProperty("readTimeout")]
        public int? ReadTimeout { get; set; }

        /// <summary>
        /// Gets or sets the connect timeout in seconds
        /// </summary>
        [JsonProperty("connectTimeout")]
        public int? ConnectTimeout { get; set; }

        /// <summary>
        /// Gets or sets the WMS store href (link)
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Wrapper for a single WMS store response
    /// </summary>
    public class WMSStoreWrapper
    {
        /// <summary>
        /// Gets or sets the WMS store
        /// </summary>
        [JsonProperty("wmsStore")]
        public WMSStore WMSStore { get; set; }
    }

    /// <summary>
    /// Wrapper for WMS store list response
    /// </summary>
    public class WMSStoreListWrapper
    {
        /// <summary>
        /// Gets or sets the WMS store list
        /// </summary>
        [JsonProperty("wmsStores")]
        public WMSStoreList WMSStoreList { get; set; }
    }

    /// <summary>
    /// Represents a list of WMS stores
    /// </summary>
    public class WMSStoreList
    {
        /// <summary>
        /// Gets or sets the array of WMS stores
        /// </summary>
        [JsonProperty("wmsStore")]
        public WMSStore[] WMSStores { get; set; }
    }

    /// <summary>
    /// Represents a GeoServer WMS layer (cascaded WMS layer)
    /// </summary>
    public class WMSLayer
    {
        /// <summary>
        /// Gets or sets the WMS layer name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the native name from the remote WMS
        /// </summary>
        [JsonProperty("nativeName")]
        public string NativeName { get; set; }

        /// <summary>
        /// Gets or sets the namespace reference
        /// </summary>
        [JsonProperty("namespace")]
        public NamespaceReference Namespace { get; set; }

        /// <summary>
        /// Gets or sets the WMS layer title
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the WMS layer description
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the abstract
        /// </summary>
        [JsonProperty("abstract")]
        public string Abstract { get; set; }

        /// <summary>
        /// Gets or sets keywords
        /// </summary>
        [JsonProperty("keywords")]
        public KeywordInfo Keywords { get; set; }

        /// <summary>
        /// Gets or sets the native CRS
        /// </summary>
        [JsonProperty("nativeCRS")]
        public string NativeCRS { get; set; }

        /// <summary>
        /// Gets or sets the SRS (Spatial Reference System)
        /// </summary>
        [JsonProperty("srs")]
        public string Srs { get; set; }

        /// <summary>
        /// Gets or sets the native bounding box
        /// </summary>
        [JsonProperty("nativeBoundingBox")]
        public BoundingBox NativeBoundingBox { get; set; }

        /// <summary>
        /// Gets or sets the lat/lon bounding box
        /// </summary>
        [JsonProperty("latLonBoundingBox")]
        public BoundingBox LatLonBoundingBox { get; set; }

        /// <summary>
        /// Gets or sets whether the WMS layer is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the store reference
        /// </summary>
        [JsonProperty("store")]
        public StoreReference Store { get; set; }

        /// <summary>
        /// Gets or sets the WMS layer href (link)
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Wrapper for a single WMS layer response
    /// </summary>
    public class WMSLayerWrapper
    {
        /// <summary>
        /// Gets or sets the WMS layer
        /// </summary>
        [JsonProperty("wmsLayer")]
        public WMSLayer WMSLayer { get; set; }
    }

    /// <summary>
    /// Wrapper for WMS layer list response
    /// </summary>
    public class WMSLayerListWrapper
    {
        /// <summary>
        /// Gets or sets the WMS layer list
        /// </summary>
        [JsonProperty("wmsLayers")]
        public WMSLayerList WMSLayerList { get; set; }
    }

    /// <summary>
    /// Represents a list of WMS layers
    /// </summary>
    public class WMSLayerList
    {
        /// <summary>
        /// Gets or sets the array of WMS layers
        /// </summary>
        [JsonProperty("wmsLayer")]
        public WMSLayer[] WMSLayers { get; set; }
    }
}
