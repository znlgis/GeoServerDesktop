using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Represents GeoServer version information
    /// </summary>
    public class VersionInfo
    {
        /// <summary>
        /// Gets or sets the GeoServer version
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the GeoTools version
        /// </summary>
        [JsonProperty("geotools")]
        public string GeoTools { get; set; }

        /// <summary>
        /// Gets or sets the GeoWebCache version
        /// </summary>
        [JsonProperty("geowebcache")]
        public string GeoWebCache { get; set; }
    }

    /// <summary>
    /// Wrapper for version information response
    /// </summary>
    public class VersionInfoWrapper
    {
        /// <summary>
        /// Gets or sets the about information
        /// </summary>
        [JsonProperty("about")]
        public AboutInfo About { get; set; }
    }

    /// <summary>
    /// Represents about information
    /// </summary>
    public class AboutInfo
    {
        /// <summary>
        /// Gets or sets the resource information
        /// </summary>
        [JsonProperty("resource")]
        public ResourceInfo[] Resources { get; set; }
    }

    /// <summary>
    /// Represents resource information
    /// </summary>
    public class ResourceInfo
    {
        /// <summary>
        /// Gets or sets the resource name
        /// </summary>
        [JsonProperty("@name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the resource value
        /// </summary>
        [JsonProperty("$")]
        public string Value { get; set; }
    }

    /// <summary>
    /// Represents system status information
    /// </summary>
    public class SystemStatus
    {
        /// <summary>
        /// Gets or sets the memory information
        /// </summary>
        [JsonProperty("metrics")]
        public Metrics Metrics { get; set; }
    }

    /// <summary>
    /// Wrapper for system status response
    /// </summary>
    public class SystemStatusWrapper
    {
        /// <summary>
        /// Gets or sets the system status
        /// </summary>
        [JsonProperty("systemStatus")]
        public SystemStatus SystemStatus { get; set; }
    }

    /// <summary>
    /// Represents system metrics
    /// </summary>
    public class Metrics
    {
        /// <summary>
        /// Gets or sets memory usage
        /// </summary>
        [JsonProperty("memory")]
        public MemoryInfo Memory { get; set; }

        /// <summary>
        /// Gets or sets JVM information
        /// </summary>
        [JsonProperty("jvm")]
        public JvmInfo Jvm { get; set; }
    }

    /// <summary>
    /// Represents memory information
    /// </summary>
    public class MemoryInfo
    {
        /// <summary>
        /// Gets or sets total memory
        /// </summary>
        [JsonProperty("total")]
        public long Total { get; set; }

        /// <summary>
        /// Gets or sets free memory
        /// </summary>
        [JsonProperty("free")]
        public long Free { get; set; }

        /// <summary>
        /// Gets or sets used memory
        /// </summary>
        [JsonProperty("used")]
        public long Used { get; set; }

        /// <summary>
        /// Gets or sets maximum memory
        /// </summary>
        [JsonProperty("max")]
        public long Max { get; set; }
    }

    /// <summary>
    /// Represents JVM information
    /// </summary>
    public class JvmInfo
    {
        /// <summary>
        /// Gets or sets JVM version
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets JVM vendor
        /// </summary>
        [JsonProperty("vendor")]
        public string Vendor { get; set; }
    }

    /// <summary>
    /// Represents GeoServer manifest information
    /// </summary>
    public class ManifestInfo
    {
        /// <summary>
        /// Gets or sets the manifest name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the manifest version
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }
    }

    /// <summary>
    /// Wrapper for manifests response
    /// </summary>
    public class ManifestsWrapper
    {
        /// <summary>
        /// Gets or sets the manifests
        /// </summary>
        [JsonProperty("about")]
        public ManifestsAbout About { get; set; }
    }

    /// <summary>
    /// Represents manifests about information
    /// </summary>
    public class ManifestsAbout
    {
        /// <summary>
        /// Gets or sets the resource manifests
        /// </summary>
        [JsonProperty("resource")]
        public ManifestInfo[] Resources { get; set; }
    }
}
