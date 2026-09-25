using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IPreviewService 的服务接口（M5 接口化）：与实现类 PreviewService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IPreviewService
    {
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
        string GetWmsUrl(string workspace, string layerName, string srs, string bbox, int width = 800, int height = 600, string format = "image/png");

        /// <summary>
        /// 生成 WMS GetCapabilities URL
        /// </summary>
        /// <param name="workspace">可选的工作空间名称，用于限定功能范围</param>
        /// <returns>WMS GetCapabilities URL</returns>
        string GetCapabilitiesUrl(string workspace = null);
    }
}
