using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Represents a GeoServer style
    /// </summary>
    public class Style
    {
        /// <summary>
        /// Name of the style
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Format of the style (e.g., sld)
        /// </summary>
        [JsonProperty("format")]
        public string Format { get; set; }

        /// <summary>
        /// Version of the style format
        /// </summary>
        [JsonProperty("languageVersion")]
        public LanguageVersion LanguageVersion { get; set; }

        /// <summary>
        /// Filename of the style
        /// </summary>
        [JsonProperty("filename")]
        public string Filename { get; set; }

        /// <summary>
        /// Link to the style resource
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Style language version
    /// </summary>
    public class LanguageVersion
    {
        /// <summary>
        /// Version string
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }
    }

    /// <summary>
    /// Wrapper for a single style response
    /// </summary>
    public class StyleWrapper
    {
        /// <summary>
        /// The style data
        /// </summary>
        [JsonProperty("style")]
        public Style Style { get; set; }
    }

    /// <summary>
    /// List of styles
    /// </summary>
    public class StyleList
    {
        /// <summary>
        /// Array of styles
        /// </summary>
        [JsonProperty("style")]
        public Style[] Styles { get; set; }
    }

    /// <summary>
    /// Wrapper for style list response
    /// </summary>
    public class StyleListWrapper
    {
        /// <summary>
        /// The style list data
        /// </summary>
        [JsonProperty("styles")]
        public StyleList StyleList { get; set; }
    }
}
