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
    /// Service for managing GeoWebCache gridsets
    /// </summary>
    public class GridsetService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 GridsetService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public GridsetService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Gets the list of gridsets
        /// </summary>
        /// <returns>List of gridsets</returns>
        public async Task<GridsetListWrapper> GetGridsetsAsync()
        {
            var response = await _httpClient.GetAsync("/gwc/rest/gridsets.json");
            return JsonConvert.DeserializeObject<GridsetListWrapper>(response);
        }

        /// <summary>
        /// 获取特定网格集的详细信息
        /// </summary>
        /// <param name="gridsetName">Gridset name</param>
        /// <returns>网格集详细信息</returns>
        public async Task<Gridset> GetGridsetAsync(string gridsetName)
        {
            var response = await _httpClient.GetAsync($"/gwc/rest/gridsets/{Uri.EscapeDataString(gridsetName)}.json");
            // FIXED-E9：单体实测为 {"gridSet":{...}}（大写 S），需 ParseSingle
            return Gridset.ParseSingle(response);
        }

        /// <summary>
        /// 创建新的网格集
        /// </summary>
        /// <param name="gridset">Gridset to create</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateGridsetAsync(Gridset gridset)
        {
            // FIXED-E10：创建路由为 PUT /gwc/rest/gridsets/{name}（GridSetController），
            // JSON 体在 3.0.1 实测报 "Duplicate field coords"，XStream XML 实测 201。
            var xml = (gridset ?? new Gridset()).ToXmlBody();
            using (var content = new StringContent(xml, Encoding.UTF8, "application/xml"))
            {
                await _httpClient.PutAsync($"/gwc/rest/gridsets/{Uri.EscapeDataString(gridset?.Name)}", content);
            }
        }

        /// <summary>
        /// 删除网格集
        /// </summary>
        /// <param name="gridsetName">Gridset name</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteGridsetAsync(string gridsetName)
        {
            await _httpClient.DeleteAsync($"/gwc/rest/gridsets/{gridsetName}");
        }
    }
}
