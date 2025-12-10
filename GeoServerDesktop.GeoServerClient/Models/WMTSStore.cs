using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Represents a GeoServer WMTS store (cascaded WMTS service)
    /// </summary>
    public class WMTSStore
    {
        /// <summary>
        /// Gets or sets the WMTS store name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the WMTS store type
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets whether the WMTS store is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the workspace reference
        /// </summary>
        [JsonProperty("workspace")]
        public WorkspaceReference Workspace { get; set; }

        /// <summary>
        /// Gets or sets the WMTS store description
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the capabilities URL for the remote WMTS service
        /// </summary>
        [JsonProperty("capabilitiesURL")]
        public string CapabilitiesUrl { get; set; }

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
        /// Gets or sets the maximum number of connections
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
        /// Gets or sets the WMTS store href (link)
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Wrapper for a single WMTS store response
    /// </summary>
    public class WMTSStoreWrapper
    {
        /// <summary>
        /// Gets or sets the WMTS store
        /// </summary>
        [JsonProperty("wmtsStore")]
        public WMTSStore WMTSStore { get; set; }
    }

    /// <summary>
    /// Wrapper for WMTS store list response
    /// </summary>
    public class WMTSStoreListWrapper
    {
        /// <summary>
        /// Gets or sets the WMTS store list
        /// </summary>
        [JsonProperty("wmtsStores")]
        public WMTSStoreList WMTSStoreList { get; set; }
    }

    /// <summary>
    /// Represents a list of WMTS stores
    /// </summary>
    public class WMTSStoreList
    {
        /// <summary>
        /// Gets or sets the array of WMTS stores
        /// </summary>
        [JsonProperty("wmtsStore")]
        public WMTSStore[] WMTSStores { get; set; }
    }

    /// <summary>
    /// Represents a GeoServer WMTS layer (layer from cascaded WMTS service)
    /// </summary>
    public class WMTSLayer
    {
        /// <summary>
        /// Gets or sets the WMTS layer name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the native name (layer name in remote WMTS service)
        /// </summary>
        [JsonProperty("nativeName")]
        public string NativeName { get; set; }

        /// <summary>
        /// Gets or sets the namespace reference
        /// </summary>
        [JsonProperty("namespace")]
        public NamespaceReference Namespace { get; set; }

        /// <summary>
        /// Gets or sets the WMTS layer title
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the WMTS layer description
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the WMTS layer abstract
        /// </summary>
        [JsonProperty("abstract")]
        public string Abstract { get; set; }

        /// <summary>
        /// Gets or sets whether the WMTS layer is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets whether the WMTS layer is advertised
        /// </summary>
        [JsonProperty("advertised")]
        public bool? Advertised { get; set; }

        /// <summary>
        /// Gets or sets the store reference
        /// </summary>
        [JsonProperty("store")]
        public StoreReference Store { get; set; }

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
        /// Gets or sets the SRS (Spatial Reference System)
        /// </summary>
        [JsonProperty("srs")]
        public string Srs { get; set; }

        /// <summary>
        /// Gets or sets the WMTS layer href (link)
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Wrapper for a single WMTS layer response
    /// </summary>
    public class WMTSLayerWrapper
    {
        /// <summary>
        /// Gets or sets the WMTS layer
        /// </summary>
        [JsonProperty("wmtsLayer")]
        public WMTSLayer WMTSLayer { get; set; }
    }

    /// <summary>
    /// Wrapper for WMTS layer list response
    /// </summary>
    public class WMTSLayerListWrapper
    {
        /// <summary>
        /// Gets or sets the WMTS layer list
        /// </summary>
        [JsonProperty("wmtsLayers")]
        public WMTSLayerList WMTSLayerList { get; set; }
    }

    /// <summary>
    /// Represents a list of WMTS layers
    /// </summary>
    public class WMTSLayerList
    {
        /// <summary>
        /// Gets or sets the array of WMTS layers
        /// </summary>
        [JsonProperty("wmtsLayer")]
        public WMTSLayer[] WMTSLayers { get; set; }
    }
}
