using System;
using System.Collections.Generic;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于生成 WMS 预览 URL 的服务
    /// </summary>
    public class PreviewService
    {
        private readonly string _baseUrl;

        /// <summary>
        /// 初始化 PreviewService 类的新实例
        /// </summary>
        /// <param name="baseUrl">GeoServer 实例的基础 URL</param>
        public PreviewService(string baseUrl)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        }

        /// <summary>
        /// 生成用于图层预览的 WMS GetMap URL
        /// </summary>
        /// <param name="workspace">工作空间名称</param>
        /// <param name="layerName">图层名称</param>
        /// <param name="srs">空间参考系统（例如 EPSG:3857）</param>
        /// <param name="bbox">边界框，格式为：minX,minY,maxX,maxY</param>
        /// <param name="width">图像宽度（像素）</param>
        /// <param name="height">图像高度（像素）</param>
        /// <param name="format">图像格式（默认：image/png）</param>
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
        /// 生成 WMS GetCapabilities URL
        /// </summary>
        /// <param name="workspace">可选的工作空间名称，用于限定功能范围</param>
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
