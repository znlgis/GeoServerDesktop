using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Represents a GeoServer coverage store (raster data store)
    /// </summary>
    public class CoverageStore
    {
        /// <summary>
        /// Gets or sets the coverage store name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the coverage store type
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets whether the coverage store is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the workspace reference
        /// </summary>
        [JsonProperty("workspace")]
        public WorkspaceReference Workspace { get; set; }

        /// <summary>
        /// Gets or sets the coverage store description
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the URL to the coverage store resource
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the connection parameters for the coverage store
        /// </summary>
        [JsonProperty("connectionParameters")]
        public ConnectionParameters ConnectionParameters { get; set; }

        /// <summary>
        /// Gets or sets the coverage store href (link)
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Wrapper for a single coverage store response
    /// </summary>
    public class CoverageStoreWrapper
    {
        /// <summary>
        /// Gets or sets the coverage store
        /// </summary>
        [JsonProperty("coverageStore")]
        public CoverageStore CoverageStore { get; set; }
    }

    /// <summary>
    /// Wrapper for coverage store list response
    /// </summary>
    public class CoverageStoreListWrapper
    {
        /// <summary>
        /// Gets or sets the coverage store list
        /// </summary>
        [JsonProperty("coverageStores")]
        public CoverageStoreList CoverageStoreList { get; set; }
    }

    /// <summary>
    /// Represents a list of coverage stores
    /// </summary>
    public class CoverageStoreList
    {
        /// <summary>
        /// Gets or sets the array of coverage stores
        /// </summary>
        [JsonProperty("coverageStore")]
        public CoverageStore[] CoverageStores { get; set; }
    }

    /// <summary>
    /// Represents a GeoServer coverage (raster layer)
    /// </summary>
    public class Coverage
    {
        /// <summary>
        /// Gets or sets the coverage name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the native name
        /// </summary>
        [JsonProperty("nativeName")]
        public string NativeName { get; set; }

        /// <summary>
        /// Gets or sets the namespace reference
        /// </summary>
        [JsonProperty("namespace")]
        public NamespaceReference Namespace { get; set; }

        /// <summary>
        /// Gets or sets the coverage title
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the coverage description
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the coverage abstract
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
        /// Gets or sets whether the coverage is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the store reference
        /// </summary>
        [JsonProperty("store")]
        public StoreReference Store { get; set; }

        /// <summary>
        /// Gets or sets the coverage href (link)
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Wrapper for a single coverage response
    /// </summary>
    public class CoverageWrapper
    {
        /// <summary>
        /// Gets or sets the coverage
        /// </summary>
        [JsonProperty("coverage")]
        public Coverage Coverage { get; set; }
    }

    /// <summary>
    /// Wrapper for coverage list response
    /// </summary>
    public class CoverageListWrapper
    {
        /// <summary>
        /// Gets or sets the coverage list
        /// </summary>
        [JsonProperty("coverages")]
        public CoverageList CoverageList { get; set; }
    }

    /// <summary>
    /// Represents a list of coverages
    /// </summary>
    public class CoverageList
    {
        /// <summary>
        /// Gets or sets the array of coverages
        /// </summary>
        [JsonProperty("coverage")]
        public Coverage[] Coverages { get; set; }
    }

    /// <summary>
    /// Represents a structured coverage index configuration
    /// </summary>
    public class StructuredCoverageIndex
    {
        /// <summary>
        /// Gets or sets the index name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the schema attributes
        /// </summary>
        [JsonProperty("schema")]
        public System.Collections.Generic.Dictionary<string, object> Schema { get; set; }
    }

    /// <summary>
    /// Represents granule information
    /// </summary>
    public class Granule
    {
        /// <summary>
        /// Gets or sets the granule ID
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the feature type
        /// </summary>
        [JsonProperty("fid")]
        public string Fid { get; set; }

        /// <summary>
        /// Gets or sets the granule attributes
        /// </summary>
        [JsonProperty("properties")]
        public System.Collections.Generic.Dictionary<string, object> Properties { get; set; }
    }

    /// <summary>
    /// Wrapper for granules list
    /// </summary>
    public class GranuleListWrapper
    {
        /// <summary>
        /// Gets or sets the list of granules
        /// </summary>
        [JsonProperty("features")]
        public System.Collections.Generic.List<Granule> Granules { get; set; }

        /// <summary>
        /// Gets or sets the total features count
        /// </summary>
        [JsonProperty("totalFeatures")]
        public int? TotalFeatures { get; set; }
    }

    /// <summary>
    /// Represents a coverage view configuration
    /// </summary>
    public class CoverageView
    {
        /// <summary>
        /// Gets or sets the coverage view name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the envelope (bounding box)
        /// </summary>
        [JsonProperty("envelope")]
        public System.Collections.Generic.Dictionary<string, object> Envelope { get; set; }

        /// <summary>
        /// Gets or sets the coverage bands
        /// </summary>
        [JsonProperty("coverageBands")]
        public System.Collections.Generic.List<CoverageBand> CoverageBands { get; set; }
    }

    /// <summary>
    /// Represents a coverage band
    /// </summary>
    public class CoverageBand
    {
        /// <summary>
        /// Gets or sets the input coverage bands
        /// </summary>
        [JsonProperty("inputCoverageBands")]
        public System.Collections.Generic.List<InputCoverageBand> InputCoverageBands { get; set; }

        /// <summary>
        /// Gets or sets the definition
        /// </summary>
        [JsonProperty("definition")]
        public string Definition { get; set; }

        /// <summary>
        /// Gets or sets the index
        /// </summary>
        [JsonProperty("index")]
        public int? Index { get; set; }

        /// <summary>
        /// Gets or sets the composition type
        /// </summary>
        [JsonProperty("compositionType")]
        public string CompositionType { get; set; }
    }

    /// <summary>
    /// Represents an input coverage band
    /// </summary>
    public class InputCoverageBand
    {
        /// <summary>
        /// Gets or sets the coverage name
        /// </summary>
        [JsonProperty("coverageName")]
        public string CoverageName { get; set; }

        /// <summary>
        /// Gets or sets the band index
        /// </summary>
        [JsonProperty("band")]
        public int? Band { get; set; }
    }

    /// <summary>
    /// Wrapper for coverage view list
    /// </summary>
    public class CoverageViewListWrapper
    {
        /// <summary>
        /// Gets or sets the list of coverage views
        /// </summary>
        [JsonProperty("coverageViews")]
        public System.Collections.Generic.List<string> CoverageViews { get; set; }
    }

    /// <summary>
    /// Wrapper for coverage view response
    /// </summary>
    public class CoverageViewWrapper
    {
        /// <summary>
        /// Gets or sets the coverage view
        /// </summary>
        [JsonProperty("coverageView")]
        public CoverageView CoverageView { get; set; }
    }
}
