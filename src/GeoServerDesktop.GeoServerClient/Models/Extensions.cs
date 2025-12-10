using Newtonsoft.Json;
using System.Collections.Generic;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Represents an import context
    /// </summary>
    public class ImportContext
    {
        /// <summary>
        /// Gets or sets the import ID
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the import state (PENDING, READY, RUNNING, COMPLETE, etc.)
        /// </summary>
        [JsonProperty("state")]
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the target workspace
        /// </summary>
        [JsonProperty("targetWorkspace")]
        public WorkspaceReference TargetWorkspace { get; set; }

        /// <summary>
        /// Gets or sets the target store
        /// </summary>
        [JsonProperty("targetStore")]
        public StoreReference TargetStore { get; set; }

        /// <summary>
        /// Gets or sets the list of tasks
        /// </summary>
        [JsonProperty("tasks")]
        public List<ImportTask> Tasks { get; set; }
    }

    /// <summary>
    /// Represents an import task
    /// </summary>
    public class ImportTask
    {
        /// <summary>
        /// Gets or sets the task ID
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the task state
        /// </summary>
        [JsonProperty("state")]
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the source data
        /// </summary>
        [JsonProperty("data")]
        public ImportData Data { get; set; }

        /// <summary>
        /// Gets or sets the target layer
        /// </summary>
        [JsonProperty("target")]
        public LayerReference Target { get; set; }

        /// <summary>
        /// Gets or sets the progress information
        /// </summary>
        [JsonProperty("progress")]
        public string Progress { get; set; }
    }

    /// <summary>
    /// Represents import data
    /// </summary>
    public class ImportData
    {
        /// <summary>
        /// Gets or sets the data type (file, directory, database, etc.)
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the format
        /// </summary>
        [JsonProperty("format")]
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets the location
        /// </summary>
        [JsonProperty("location")]
        public string Location { get; set; }
    }

    /// <summary>
    /// Represents a layer reference
    /// </summary>
    public class LayerReference
    {
        /// <summary>
        /// Gets or sets the layer name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Wrapper for import context
    /// </summary>
    public class ImportContextWrapper
    {
        /// <summary>
        /// Gets or sets the import context
        /// </summary>
        [JsonProperty("import")]
        public ImportContext Import { get; set; }
    }

    /// <summary>
    /// Represents monitoring request information
    /// </summary>
    public class MonitorRequest
    {
        /// <summary>
        /// Gets or sets the request ID
        /// </summary>
        [JsonProperty("id")]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the path
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the query string
        /// </summary>
        [JsonProperty("queryString")]
        public string QueryString { get; set; }

        /// <summary>
        /// Gets or sets the HTTP method
        /// </summary>
        [JsonProperty("httpMethod")]
        public string HttpMethod { get; set; }

        /// <summary>
        /// Gets or sets the start time
        /// </summary>
        [JsonProperty("startTime")]
        public string StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time
        /// </summary>
        [JsonProperty("endTime")]
        public string EndTime { get; set; }

        /// <summary>
        /// Gets or sets the total time in milliseconds
        /// </summary>
        [JsonProperty("totalTime")]
        public long TotalTime { get; set; }

        /// <summary>
        /// Gets or sets the response status
        /// </summary>
        [JsonProperty("responseStatus")]
        public int ResponseStatus { get; set; }

        /// <summary>
        /// Gets or sets the response length
        /// </summary>
        [JsonProperty("responseLength")]
        public long ResponseLength { get; set; }

        /// <summary>
        /// Gets or sets the remote address
        /// </summary>
        [JsonProperty("remoteAddr")]
        public string RemoteAddr { get; set; }

        /// <summary>
        /// Gets or sets the remote host
        /// </summary>
        [JsonProperty("remoteHost")]
        public string RemoteHost { get; set; }
    }

    /// <summary>
    /// Wrapper for monitoring requests list
    /// </summary>
    public class MonitorRequestListWrapper
    {
        /// <summary>
        /// Gets or sets the list of requests
        /// </summary>
        [JsonProperty("requests")]
        public List<MonitorRequest> Requests { get; set; }
    }

    /// <summary>
    /// Represents monitoring statistics
    /// </summary>
    public class MonitorStatistics
    {
        /// <summary>
        /// Gets or sets the total requests
        /// </summary>
        [JsonProperty("totalRequests")]
        public long TotalRequests { get; set; }

        /// <summary>
        /// Gets or sets the average response time
        /// </summary>
        [JsonProperty("avgResponseTime")]
        public double AvgResponseTime { get; set; }

        /// <summary>
        /// Gets or sets the request statistics by path
        /// </summary>
        [JsonProperty("byPath")]
        public Dictionary<string, PathStatistics> ByPath { get; set; }
    }

    /// <summary>
    /// Represents statistics for a specific path
    /// </summary>
    public class PathStatistics
    {
        /// <summary>
        /// Gets or sets the request count
        /// </summary>
        [JsonProperty("count")]
        public long Count { get; set; }

        /// <summary>
        /// Gets or sets the average response time
        /// </summary>
        [JsonProperty("avgTime")]
        public double AvgTime { get; set; }

        /// <summary>
        /// Gets or sets the total bytes transferred
        /// </summary>
        [JsonProperty("totalBytes")]
        public long TotalBytes { get; set; }
    }

    /// <summary>
    /// Represents a transform (XSLT)
    /// </summary>
    public class Transform
    {
        /// <summary>
        /// Gets or sets the transform name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the XSLT content
        /// </summary>
        [JsonProperty("xslt")]
        public string XSLT { get; set; }

        /// <summary>
        /// Gets or sets the source format
        /// </summary>
        [JsonProperty("sourceFormat")]
        public string SourceFormat { get; set; }

        /// <summary>
        /// Gets or sets the output format
        /// </summary>
        [JsonProperty("outputFormat")]
        public string OutputFormat { get; set; }
    }

    /// <summary>
    /// Wrapper for transforms list
    /// </summary>
    public class TransformListWrapper
    {
        /// <summary>
        /// Gets or sets the list of transforms
        /// </summary>
        [JsonProperty("transforms")]
        public List<string> Transforms { get; set; }
    }

    /// <summary>
    /// Represents a URL check rule
    /// </summary>
    public class URLCheck
    {
        /// <summary>
        /// Gets or sets the rule name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets whether the check is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the URL pattern (regex)
        /// </summary>
        [JsonProperty("urlPattern")]
        public string UrlPattern { get; set; }

        /// <summary>
        /// Gets or sets the check type (DENY, ALLOW)
        /// </summary>
        [JsonProperty("checkType")]
        public string CheckType { get; set; }
    }

    /// <summary>
    /// Wrapper for URL checks list
    /// </summary>
    public class URLCheckListWrapper
    {
        /// <summary>
        /// Gets or sets the list of URL checks
        /// </summary>
        [JsonProperty("checks")]
        public List<string> Checks { get; set; }
    }
}
