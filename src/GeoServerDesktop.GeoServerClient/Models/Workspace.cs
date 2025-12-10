using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Represents a GeoServer workspace
    /// </summary>
    public class Workspace
    {
        /// <summary>
        /// Name of the workspace
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Whether this workspace is isolated
        /// </summary>
        [JsonProperty("isolated")]
        public bool? Isolated { get; set; }

        /// <summary>
        /// Link to the workspace resource
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Wrapper for a single workspace response
    /// </summary>
    public class WorkspaceWrapper
    {
        /// <summary>
        /// The workspace data
        /// </summary>
        [JsonProperty("workspace")]
        public Workspace Workspace { get; set; }
    }

    /// <summary>
    /// List of workspaces
    /// </summary>
    public class WorkspaceList
    {
        /// <summary>
        /// Array of workspaces
        /// </summary>
        [JsonProperty("workspace")]
        public Workspace[] Workspaces { get; set; }
    }

    /// <summary>
    /// Wrapper for workspace list response
    /// </summary>
    public class WorkspaceListWrapper
    {
        /// <summary>
        /// The workspace list data
        /// </summary>
        [JsonProperty("workspaces")]
        public WorkspaceList WorkspaceList { get; set; }
    }
}
