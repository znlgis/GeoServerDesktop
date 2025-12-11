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
            _httpClient = httpClient;
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
            var response = await _httpClient.GetAsync($"/gwc/rest/gridsets/{gridsetName}.json");
            return JsonConvert.DeserializeObject<Gridset>(response);
        }

        /// <summary>
        /// 创建新的网格集
        /// </summary>
        /// <param name="gridset">Gridset to create</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateGridsetAsync(Gridset gridset)
        {
            var json = JsonConvert.SerializeObject(gridset);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/gwc/rest/gridsets", content);
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
