using Newtonsoft.Json;
using System.Collections.Generic;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Wrapper for fonts list
    /// </summary>
    public class FontListWrapper
    {
        /// <summary>
        /// Gets or sets the list of fonts
        /// </summary>
        [JsonProperty("fonts")]
        public List<string> Fonts { get; set; }
    }

    /// <summary>
    /// Represents a feature template
    /// </summary>
    public class Template
    {
        /// <summary>
        /// Gets or sets the template name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the template content
        /// </summary>
        [JsonProperty("content")]
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the template type (e.g., header, footer, content)
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }
    }

    /// <summary>
    /// Wrapper for templates list
    /// </summary>
    public class TemplateListWrapper
    {
        /// <summary>
        /// Gets or sets the list of templates
        /// </summary>
        [JsonProperty("templates")]
        public List<string> Templates { get; set; }
    }
}
