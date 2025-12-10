using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Represents a GeoServer feature type
    /// </summary>
    public class FeatureType
    {
        /// <summary>
        /// Name of the feature type
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Native name of the feature type
        /// </summary>
        [JsonProperty("nativeName")]
        public string NativeName { get; set; }

        /// <summary>
        /// Namespace associated with the feature type
        /// </summary>
        [JsonProperty("namespace")]
        public NamespaceReference Namespace { get; set; }

        /// <summary>
        /// Title of the feature type
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Abstract/description of the feature type
        /// </summary>
        [JsonProperty("abstract")]
        public string Abstract { get; set; }

        /// <summary>
        /// Keywords associated with the feature type
        /// </summary>
        [JsonProperty("keywords")]
        public KeywordInfo Keywords { get; set; }

        /// <summary>
        /// Native CRS of the feature type
        /// </summary>
        [JsonProperty("nativeCRS")]
        public string NativeCRS { get; set; }

        /// <summary>
        /// SRS of the feature type
        /// </summary>
        [JsonProperty("srs")]
        public string Srs { get; set; }

        /// <summary>
        /// Native bounding box
        /// </summary>
        [JsonProperty("nativeBoundingBox")]
        public BoundingBox NativeBoundingBox { get; set; }

        /// <summary>
        /// Lat/lon bounding box
        /// </summary>
        [JsonProperty("latLonBoundingBox")]
        public BoundingBox LatLonBoundingBox { get; set; }

        /// <summary>
        /// Whether the feature type is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Store reference
        /// </summary>
        [JsonProperty("store")]
        public StoreReference Store { get; set; }

        /// <summary>
        /// Link to the feature type resource
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Reference to a namespace
    /// </summary>
    public class NamespaceReference
    {
        /// <summary>
        /// Name of the namespace
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Link to the namespace
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Reference to a data store
    /// </summary>
    public class StoreReference
    {
        /// <summary>
        /// Class type of the store
        /// </summary>
        [JsonProperty("@class")]
        public string Class { get; set; }

        /// <summary>
        /// Name of the store
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Link to the store
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Keyword information
    /// </summary>
    public class KeywordInfo
    {
        /// <summary>
        /// Array of keywords
        /// </summary>
        [JsonProperty("string")]
        public string[] Keywords { get; set; }
    }

    /// <summary>
    /// Bounding box information
    /// </summary>
    public class BoundingBox
    {
        /// <summary>
        /// Minimum X coordinate
        /// </summary>
        [JsonProperty("minx")]
        public double MinX { get; set; }

        /// <summary>
        /// Maximum X coordinate
        /// </summary>
        [JsonProperty("maxx")]
        public double MaxX { get; set; }

        /// <summary>
        /// Minimum Y coordinate
        /// </summary>
        [JsonProperty("miny")]
        public double MinY { get; set; }

        /// <summary>
        /// Maximum Y coordinate
        /// </summary>
        [JsonProperty("maxy")]
        public double MaxY { get; set; }

        /// <summary>
        /// Coordinate reference system
        /// </summary>
        [JsonProperty("crs")]
        public string Crs { get; set; }
    }

    /// <summary>
    /// Wrapper for a single feature type response
    /// </summary>
    public class FeatureTypeWrapper
    {
        /// <summary>
        /// The feature type data
        /// </summary>
        [JsonProperty("featureType")]
        public FeatureType FeatureType { get; set; }
    }

    /// <summary>
    /// List of feature types
    /// </summary>
    public class FeatureTypeList
    {
        /// <summary>
        /// Array of feature types
        /// </summary>
        [JsonProperty("featureType")]
        public FeatureType[] FeatureTypes { get; set; }
    }

    /// <summary>
    /// Wrapper for feature type list response
    /// </summary>
    public class FeatureTypeListWrapper
    {
        /// <summary>
        /// The feature type list data
        /// </summary>
        [JsonProperty("featureTypes")]
        public FeatureTypeList FeatureTypeList { get; set; }
    }
}
