using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Represents WMS service settings
    /// </summary>
    public class WMSSettings
    {
        /// <summary>
        /// Gets or sets the WMS service wrapper
        /// </summary>
        [JsonProperty("wms")]
        public WMSServiceConfig WMS { get; set; }
    }

    /// <summary>
    /// WMS service configuration
    /// </summary>
    public class WMSServiceConfig
    {
        /// <summary>
        /// Gets or sets whether the service is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the service name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the service title
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the service abstract
        /// </summary>
        [JsonProperty("abstrct")]
        public string Abstract { get; set; }

        /// <summary>
        /// Gets or sets the maintainer
        /// </summary>
        [JsonProperty("maintainer")]
        public string Maintainer { get; set; }

        /// <summary>
        /// Gets or sets the online resource
        /// </summary>
        [JsonProperty("onlineResource")]
        public string OnlineResource { get; set; }

        /// <summary>
        /// Gets or sets whether to cite compliant
        /// </summary>
        [JsonProperty("citeCompliant")]
        public bool? CiteCompliant { get; set; }

        /// <summary>
        /// Gets or sets the schema base URL
        /// </summary>
        [JsonProperty("schemaBaseURL")]
        public string SchemaBaseURL { get; set; }

        /// <summary>
        /// Gets or sets whether verbose is enabled
        /// </summary>
        [JsonProperty("verbose")]
        public bool? Verbose { get; set; }

        /// <summary>
        /// Gets or sets metadata link
        /// </summary>
        [JsonProperty("metadataLink")]
        public MetadataLink MetadataLink { get; set; }

        /// <summary>
        /// Gets or sets the watermark configuration
        /// </summary>
        [JsonProperty("watermark")]
        public Watermark Watermark { get; set; }

        /// <summary>
        /// Gets or sets whether dynamic styling is enabled
        /// </summary>
        [JsonProperty("dynamicStylingDisabled")]
        public bool? DynamicStylingDisabled { get; set; }

        /// <summary>
        /// Gets or sets the maximum rendering time in seconds
        /// </summary>
        [JsonProperty("maxRequestMemory")]
        public int? MaxRequestMemory { get; set; }

        /// <summary>
        /// Gets or sets the maximum rendering errors
        /// </summary>
        [JsonProperty("maxRenderingErrors")]
        public int? MaxRenderingErrors { get; set; }

        /// <summary>
        /// Gets or sets the SRS list
        /// </summary>
        [JsonProperty("srs")]
        public SRSList SRS { get; set; }
    }

    /// <summary>
    /// Represents WFS service settings
    /// </summary>
    public class WFSSettings
    {
        /// <summary>
        /// Gets or sets the WFS service wrapper
        /// </summary>
        [JsonProperty("wfs")]
        public WFSServiceConfig WFS { get; set; }
    }

    /// <summary>
    /// WFS service configuration
    /// </summary>
    public class WFSServiceConfig
    {
        /// <summary>
        /// Gets or sets whether the service is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the service name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the service title
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the service abstract
        /// </summary>
        [JsonProperty("abstrct")]
        public string Abstract { get; set; }

        /// <summary>
        /// Gets or sets the maintainer
        /// </summary>
        [JsonProperty("maintainer")]
        public string Maintainer { get; set; }

        /// <summary>
        /// Gets or sets the online resource
        /// </summary>
        [JsonProperty("onlineResource")]
        public string OnlineResource { get; set; }

        /// <summary>
        /// Gets or sets whether to cite compliant
        /// </summary>
        [JsonProperty("citeCompliant")]
        public bool? CiteCompliant { get; set; }

        /// <summary>
        /// Gets or sets the schema base URL
        /// </summary>
        [JsonProperty("schemaBaseURL")]
        public string SchemaBaseURL { get; set; }

        /// <summary>
        /// Gets or sets whether verbose is enabled
        /// </summary>
        [JsonProperty("verbose")]
        public bool? Verbose { get; set; }

        /// <summary>
        /// Gets or sets the GML2 output format
        /// </summary>
        [JsonProperty("gml2")]
        public GMLFormat GML2 { get; set; }

        /// <summary>
        /// Gets or sets the GML3 output format
        /// </summary>
        [JsonProperty("gml3")]
        public GMLFormat GML3 { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of features
        /// </summary>
        [JsonProperty("maxFeatures")]
        public int? MaxFeatures { get; set; }

        /// <summary>
        /// Gets or sets the service level
        /// </summary>
        [JsonProperty("serviceLevel")]
        public string ServiceLevel { get; set; }

        /// <summary>
        /// Gets or sets whether feature bounding is enabled
        /// </summary>
        [JsonProperty("featureBounding")]
        public bool? FeatureBounding { get; set; }

        /// <summary>
        /// Gets or sets whether canonical schema location is enabled
        /// </summary>
        [JsonProperty("canonicalSchemaLocation")]
        public bool? CanonicalSchemaLocation { get; set; }

        /// <summary>
        /// Gets or sets whether to encode feature member
        /// </summary>
        [JsonProperty("encodeFeatureMember")]
        public bool? EncodeFeatureMember { get; set; }

        /// <summary>
        /// Gets or sets whether hits ignore max features
        /// </summary>
        [JsonProperty("hitsIgnoreMaxFeatures")]
        public bool? HitsIgnoreMaxFeatures { get; set; }
    }

    /// <summary>
    /// Represents WCS service settings
    /// </summary>
    public class WCSSettings
    {
        /// <summary>
        /// Gets or sets the WCS service wrapper
        /// </summary>
        [JsonProperty("wcs")]
        public WCSServiceConfig WCS { get; set; }
    }

    /// <summary>
    /// WCS service configuration
    /// </summary>
    public class WCSServiceConfig
    {
        /// <summary>
        /// Gets or sets whether the service is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the service name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the service title
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the service abstract
        /// </summary>
        [JsonProperty("abstrct")]
        public string Abstract { get; set; }

        /// <summary>
        /// Gets or sets the maintainer
        /// </summary>
        [JsonProperty("maintainer")]
        public string Maintainer { get; set; }

        /// <summary>
        /// Gets or sets the online resource
        /// </summary>
        [JsonProperty("onlineResource")]
        public string OnlineResource { get; set; }

        /// <summary>
        /// Gets or sets whether to cite compliant
        /// </summary>
        [JsonProperty("citeCompliant")]
        public bool? CiteCompliant { get; set; }

        /// <summary>
        /// Gets or sets the schema base URL
        /// </summary>
        [JsonProperty("schemaBaseURL")]
        public string SchemaBaseURL { get; set; }

        /// <summary>
        /// Gets or sets whether verbose is enabled
        /// </summary>
        [JsonProperty("verbose")]
        public bool? Verbose { get; set; }

        /// <summary>
        /// Gets or sets the maximum input memory in KB
        /// </summary>
        [JsonProperty("maxInputMemory")]
        public int? MaxInputMemory { get; set; }

        /// <summary>
        /// Gets or sets the maximum output memory in KB
        /// </summary>
        [JsonProperty("maxOutputMemory")]
        public int? MaxOutputMemory { get; set; }

        /// <summary>
        /// Gets or sets whether subsampling is enabled
        /// </summary>
        [JsonProperty("subsamplingEnabled")]
        public bool? SubsamplingEnabled { get; set; }

        /// <summary>
        /// Gets or sets whether overview policy
        /// </summary>
        [JsonProperty("overviewPolicy")]
        public string OverviewPolicy { get; set; }

        /// <summary>
        /// Gets or sets the resource consumption limits
        /// </summary>
        [JsonProperty("resourceConsumptionLimits")]
        public ResourceConsumptionLimits ResourceConsumptionLimits { get; set; }
    }

    /// <summary>
    /// Represents WMTS service settings
    /// </summary>
    public class WMTSSettings
    {
        /// <summary>
        /// Gets or sets the WMTS service wrapper
        /// </summary>
        [JsonProperty("wmts")]
        public WMTSServiceConfig WMTS { get; set; }
    }

    /// <summary>
    /// WMTS service configuration
    /// </summary>
    public class WMTSServiceConfig
    {
        /// <summary>
        /// Gets or sets whether the service is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the service name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the service title
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the service abstract
        /// </summary>
        [JsonProperty("abstrct")]
        public string Abstract { get; set; }

        /// <summary>
        /// Gets or sets the maintainer
        /// </summary>
        [JsonProperty("maintainer")]
        public string Maintainer { get; set; }

        /// <summary>
        /// Gets or sets the online resource
        /// </summary>
        [JsonProperty("onlineResource")]
        public string OnlineResource { get; set; }

        /// <summary>
        /// Gets or sets whether to cite compliant
        /// </summary>
        [JsonProperty("citeCompliant")]
        public bool? CiteCompliant { get; set; }

        /// <summary>
        /// Gets or sets whether verbose is enabled
        /// </summary>
        [JsonProperty("verbose")]
        public bool? Verbose { get; set; }
    }

    /// <summary>
    /// Represents metadata link information
    /// </summary>
    public class MetadataLink
    {
        /// <summary>
        /// Gets or sets the metadata type
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the metadata URL
        /// </summary>
        [JsonProperty("metadataType")]
        public string MetadataType { get; set; }

        /// <summary>
        /// Gets or sets the content
        /// </summary>
        [JsonProperty("content")]
        public string Content { get; set; }
    }

    /// <summary>
    /// Represents watermark configuration
    /// </summary>
    public class Watermark
    {
        /// <summary>
        /// Gets or sets whether watermark is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the watermark position
        /// </summary>
        [JsonProperty("position")]
        public string Position { get; set; }

        /// <summary>
        /// Gets or sets the transparency (0-100)
        /// </summary>
        [JsonProperty("transparency")]
        public int? Transparency { get; set; }

        /// <summary>
        /// Gets or sets the watermark URL
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    /// <summary>
    /// Represents SRS list
    /// </summary>
    public class SRSList
    {
        /// <summary>
        /// Gets or sets the list of SRS codes
        /// </summary>
        [JsonProperty("string")]
        public string[] Strings { get; set; }
    }

    /// <summary>
    /// Represents GML format configuration
    /// </summary>
    public class GMLFormat
    {
        /// <summary>
        /// Gets or sets whether to override GML attributes
        /// </summary>
        [JsonProperty("overrideGMLAttributes")]
        public bool? OverrideGMLAttributes { get; set; }

        /// <summary>
        /// Gets or sets the SRS name style
        /// </summary>
        [JsonProperty("srsNameStyle")]
        public string SrsNameStyle { get; set; }
    }

    /// <summary>
    /// Represents resource consumption limits
    /// </summary>
    public class ResourceConsumptionLimits
    {
        /// <summary>
        /// Gets or sets the maximum dimension values
        /// </summary>
        [JsonProperty("maxDimensions")]
        public int? MaxDimensions { get; set; }

        /// <summary>
        /// Gets or sets the maximum request size in pixels
        /// </summary>
        [JsonProperty("maxRequestSizePixels")]
        public long? MaxRequestSizePixels { get; set; }
    }
}
