namespace GeoServerDesktop.GeoServerClient.Configuration
{
    /// <summary>
    /// Configuration options for GeoServer REST API client
    /// </summary>
    public class GeoServerClientOptions
    {
        /// <summary>
        /// Base URL of the GeoServer instance (e.g., http://localhost:8080/geoserver)
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// Username for basic authentication
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Password for basic authentication
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// HTTP request timeout in seconds (default: 30)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }
}
