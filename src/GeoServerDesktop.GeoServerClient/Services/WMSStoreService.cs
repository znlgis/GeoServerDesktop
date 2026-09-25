using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于管理 GeoServer WMS 存储（级联 WMS）的服务
    /// </summary>
    public class WMSStoreService : ServiceBase
    {

        /// <summary>
        /// 初始化 WMSStoreService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public WMSStoreService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// 获取工作空间中的 WMS 存储列表
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <returns>WMS 存储数组</returns>
        public async Task<WMSStore[]> GetWMSStoresAsync(string workspaceName)
        {
            var wrapper = await GetJsonAsync<WMSStoreListWrapper>($"/rest/workspaces/{Esc(workspaceName)}/wmsstores.json");
            return wrapper?.WMSStoreList?.WMSStores ?? Array.Empty<WMSStore>();
        }

        /// <summary>
        /// 获取特定 WMS 存储的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStoreName">WMS 存储的名称</param>
        /// <returns>WMS 存储详细信息</returns>
        public async Task<WMSStore> GetWMSStoreAsync(string workspaceName, string wmsStoreName)
        {
            var wrapper = await GetJsonAsync<WMSStoreWrapper>($"/rest/workspaces/{Esc(workspaceName)}/wmsstores/{Esc(wmsStoreName)}.json");
            return wrapper?.WMSStore;
        }

        /// <summary>
        /// 创建新的 WMS 存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStore">WMS 存储配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateWMSStoreAsync(string workspaceName, WMSStore wmsStore)
        {
            var wrapper = new { wmsStore = wmsStore };
            await PostJsonAsync($"/rest/workspaces/{Esc(workspaceName)}/wmsstores", wrapper);
        }

        /// <summary>
        /// 更新现有的 WMS 存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStoreName">WMS 存储的名称</param>
        /// <param name="wmsStore">Updated WMS 存储配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateWMSStoreAsync(string workspaceName, string wmsStoreName, WMSStore wmsStore)
        {
            var wrapper = new { wmsStore = wmsStore };
            await PutJsonAsync($"/rest/workspaces/{Esc(workspaceName)}/wmsstores/{Esc(wmsStoreName)}", wrapper);
        }

        /// <summary>
        /// 删除 WMS 存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="wmsStoreName">WMS 存储的名称</param>
        /// <param name="recurse">Whether to recursively delete all WMS layers in the store</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteWMSStoreAsync(string workspaceName, string wmsStoreName, bool recurse = false)
        {
            var recurseValue = recurse ? "true" : "false";
            var path = $"/rest/workspaces/{Esc(workspaceName)}/wmsstores/{Esc(wmsStoreName)}?recurse={recurseValue}";
            await Http.DeleteAsync(path);
        }
    }
}
