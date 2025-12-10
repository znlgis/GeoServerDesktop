using System;
using System.Collections.Generic;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for generating WMS preview URLs
    /// </summary>
    public class PreviewService
    {
        private readonly string _baseUrl;

        /// <summary>
        /// Initializes a new instance of the PreviewService class
        /// </summary>
        /// <param name="baseUrl">Base URL of the GeoServer instance</param>
        public PreviewService(string baseUrl)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        }

        /// <summary>
        /// Generates a WMS GetMap URL for layer preview
        /// </summary>
        /// <param name="workspace">Workspace name</param>
        /// <param name="layerName">Layer name</param>
        /// <param name="srs">Spatial reference system (e.g., EPSG:3857)</param>
        /// <param name="bbox">Bounding box in format: minX,minY,maxX,maxY</param>
        /// <param name="width">Image width in pixels</param>
        /// <param name="height">Image height in pixels</param>
        /// <param name="format">Image format (default: image/png)</param>
        /// <returns>WMS GetMap URL</returns>
        public string GetWmsUrl(
            string workspace,
            string layerName,
            string srs,
            string bbox,
            int width = 800,
            int height = 600,
            string format = "image/png")
        {
            var parameters = new Dictionary<string, string>
            {
                ["service"] = "WMS",
                ["version"] = "1.1.0",
                ["request"] = "GetMap",
                ["layers"] = string.IsNullOrWhiteSpace(workspace) ? layerName : $"{workspace}:{layerName}",
                ["srs"] = srs,
                ["bbox"] = bbox,
                ["width"] = width.ToString(),
                ["height"] = height.ToString(),
                ["format"] = format
            };

            var queryString = string.Join("&", 
                Array.ConvertAll(
                    new List<string>(parameters.Keys).ToArray(), 
                    key => $"{key}={Uri.EscapeDataString(parameters[key])}"
                )
            );

            return $"{_baseUrl}/wms?{queryString}";
        }

        /// <summary>
        /// Generates a WMS GetCapabilities URL
        /// </summary>
        /// <param name="workspace">Optional workspace name to scope the capabilities</param>
        /// <returns>WMS GetCapabilities URL</returns>
        public string GetCapabilitiesUrl(string workspace = null)
        {
            var path = string.IsNullOrWhiteSpace(workspace) 
                ? "/wms" 
                : $"/{workspace}/wms";

            return $"{_baseUrl}{path}?service=WMS&version=1.1.0&request=GetCapabilities";
        }
    }
}
