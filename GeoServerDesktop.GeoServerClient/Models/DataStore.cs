using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Represents a GeoServer data store
    /// </summary>
    public class DataStore
    {
        /// <summary>
        /// Name of the data store
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Type of the data store (e.g., PostGIS, Shapefile)
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Whether the data store is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Workspace the data store belongs to
        /// </summary>
        [JsonProperty("workspace")]
        public WorkspaceReference Workspace { get; set; }

        /// <summary>
        /// Connection parameters for the data store
        /// </summary>
        [JsonProperty("connectionParameters")]
        public ConnectionParameters ConnectionParameters { get; set; }

        /// <summary>
        /// Link to the data store resource
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Reference to a workspace
    /// </summary>
    public class WorkspaceReference
    {
        /// <summary>
        /// Name of the workspace
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Link to the workspace
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Connection parameters for a data store
    /// </summary>
    public class ConnectionParameters
    {
        /// <summary>
        /// Entry collection for connection parameters
        /// </summary>
        [JsonProperty("entry")]
        public ConnectionParameterEntry[] Entries { get; set; }
    }

    /// <summary>
    /// Single connection parameter entry
    /// </summary>
    public class ConnectionParameterEntry
    {
        /// <summary>
        /// Parameter key
        /// </summary>
        [JsonProperty("@key")]
        public string Key { get; set; }

        /// <summary>
        /// Parameter value
        /// </summary>
        [JsonProperty("$")]
        public string Value { get; set; }
    }

    /// <summary>
    /// Wrapper for a single data store response
    /// </summary>
    public class DataStoreWrapper
    {
        /// <summary>
        /// The data store data
        /// </summary>
        [JsonProperty("dataStore")]
        public DataStore DataStore { get; set; }
    }

    /// <summary>
    /// List of data stores
    /// </summary>
    public class DataStoreList
    {
        /// <summary>
        /// Array of data stores
        /// </summary>
        [JsonProperty("dataStore")]
        public DataStore[] DataStores { get; set; }
    }

    /// <summary>
    /// Wrapper for data store list response
    /// </summary>
    public class DataStoreListWrapper
    {
        /// <summary>
        /// The data store list data
        /// </summary>
        [JsonProperty("dataStores")]
        public DataStoreList DataStoreList { get; set; }
    }
}
