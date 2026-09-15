using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoWebCache layers
    /// </summary>
    public class GWCLayerService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 GWCLayerService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public GWCLayerService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Gets the list of cached layers
        /// </summary>
        /// <returns>List of cached layers</returns>
        public async Task<GWCLayerListWrapper> GetLayersAsync()
        {
            var response = await _httpClient.GetAsync("/gwc/rest/layers.json");
            return JsonConvert.DeserializeObject<GWCLayerListWrapper>(response);
        }

        /// <summary>
        /// Gets information about a specific cached layer
        /// </summary>
        /// <param name="layerName">Layer name</param>
        /// <returns>Layer information</returns>
        public async Task<GWCLayer> GetLayerAsync(string layerName)
        {
            var response = await _httpClient.GetAsync($"/gwc/rest/layers/{Uri.EscapeDataString(layerName)}.json");
            // FIXED-E9：单体实测根为实现类名 {"GeoServerLayer":{...}}，动态键需手工解析
            return GWCLayer.ParseSingle(response);
        }

        /// <summary>
        /// Seeds a layer (starts tile generation)
        /// </summary>
        /// <param name="layerName">Layer name</param>
        /// <param name="seedRequest">Seed request configuration</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task SeedLayerAsync(string layerName, SeedRequest seedRequest)
        {
            // FIXED-E12：GWC SeedController 实测 POST /gwc/rest/seed/{layer} 以 XML seedRequest 为体
            // （XStream 形态；zoomStart/zoomStop 必填）。truncate 同路由，仅 type=truncate。
            var xml = (seedRequest ?? new SeedRequest()).ToXmlBody();
            using (var content = new StringContent(xml, Encoding.UTF8, "application/xml"))
            {
                await _httpClient.PostAsync($"/gwc/rest/seed/{Uri.EscapeDataString(layerName)}", content);
            }
        }

        /// <summary>
        /// Truncates the cache for all layers (mass truncate)
        /// </summary>
        /// <returns>表示异步操作的任务</returns>
        public async Task TruncateAllLayersAsync()
        {
            // FIXED-E12：MassTruncateController 源码以 XStream DomDriver 读 XML（或 form）请求体，
            // 空 <massTruncateRequest/> 即触发全量清空；JSON {"truncateAll":true} 实测不被接受。
            var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<massTruncateRequest/>";
            using (var content = new StringContent(xml, Encoding.UTF8, "application/xml"))
            {
                await _httpClient.PostAsync("/gwc/rest/masstruncate", content);
            }
        }
    }
}
