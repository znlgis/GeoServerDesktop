using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Represents GeoServer logging configuration
    /// </summary>
    public class LoggingSettings
    {
        /// <summary>
        /// Gets or sets the logging configuration
        /// </summary>
        [JsonProperty("logging")]
        public LoggingConfig Logging { get; set; }
    }

    /// <summary>
    /// Represents logging configuration details
    /// </summary>
    public class LoggingConfig
    {
        /// <summary>
        /// Gets or sets the logging level
        /// </summary>
        [JsonProperty("level")]
        public string Level { get; set; }

        /// <summary>
        /// Gets or sets the logging location (file path)
        /// </summary>
        [JsonProperty("location")]
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets whether to log to stdout
        /// </summary>
        [JsonProperty("stdOutLogging")]
        public bool? StdOutLogging { get; set; }

        /// <summary>
        /// Gets or sets whether to enable logging to file
        /// </summary>
        [JsonProperty("fileLogging")]
        public bool? FileLogging { get; set; }
    }
}
