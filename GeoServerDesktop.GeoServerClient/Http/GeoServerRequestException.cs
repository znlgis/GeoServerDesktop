using System;

namespace GeoServerDesktop.GeoServerClient.Http
{
    /// <summary>
    /// Exception thrown when a GeoServer REST API request fails
    /// </summary>
    public class GeoServerRequestException : Exception
    {
        /// <summary>
        /// HTTP status code from the response
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Response content from the failed request
        /// </summary>
        public string ResponseContent { get; }

        /// <summary>
        /// Initializes a new instance of the GeoServerRequestException class
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <param name="responseContent">Response content</param>
        public GeoServerRequestException(string message, int statusCode, string responseContent)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }

        /// <summary>
        /// Initializes a new instance of the GeoServerRequestException class with an inner exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <param name="responseContent">Response content</param>
        /// <param name="innerException">Inner exception</param>
        public GeoServerRequestException(string message, int statusCode, string responseContent, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }
    }
}
