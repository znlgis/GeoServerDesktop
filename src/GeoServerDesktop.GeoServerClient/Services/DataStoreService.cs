using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 用于管理 GeoServer 数据存储的服务
    /// </summary>
    public class DataStoreService : ServiceBase, IDataStoreService
    {

        /// <summary>
        /// 初始化 DataStoreService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public DataStoreService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// 获取工作空间中的数据存储列表
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <returns>数据存储数组</returns>
        public async Task<DataStore[]> GetDataStoresAsync(string workspaceName)
        {
            var wrapper = await GetJsonAsync<DataStoreListWrapper>($"/rest/workspaces/{Esc(workspaceName)}/datastores.json");
            return wrapper?.DataStoreList?.DataStores ?? Array.Empty<DataStore>();
        }

        /// <summary>
        /// 获取特定数据存储的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <returns>数据存储详细信息</returns>
        public async Task<DataStore> GetDataStoreAsync(string workspaceName, string dataStoreName)
        {
            var wrapper = await GetJsonAsync<DataStoreWrapper>($"/rest/workspaces/{Esc(workspaceName)}/datastores/{Esc(dataStoreName)}.json");
            return wrapper?.DataStore;
        }

        /// <summary>
        /// 创建新的数据存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStore">数据存储配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CreateDataStoreAsync(string workspaceName, DataStore dataStore)
        {
            var wrapper = new { dataStore = dataStore };
            await PostJsonAsync($"/rest/workspaces/{Esc(workspaceName)}/datastores", wrapper);
        }

        /// <summary>
        /// 更新现有的数据存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <param name="dataStore">更新后的数据存储配置</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UpdateDataStoreAsync(string workspaceName, string dataStoreName, DataStore dataStore)
        {
            var wrapper = new { dataStore = dataStore };
            await PutJsonAsync($"/rest/workspaces/{Esc(workspaceName)}/datastores/{Esc(dataStoreName)}", wrapper);
        }

        /// <summary>
        /// 删除数据存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">要删除的数据存储名称</param>
        /// <param name="recurse">是否递归删除数据存储中的所有资源</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteDataStoreAsync(string workspaceName, string dataStoreName, bool recurse = false)
        {
            var path = $"/rest/workspaces/{Esc(workspaceName)}/datastores/{Esc(dataStoreName)}?recurse={Bool(recurse)}";
            await Http.DeleteAsync(path);
        }

        /// <summary>
        /// 重置数据存储缓存
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task ResetDataStoreAsync(string workspaceName, string dataStoreName)
        {
            await Http.PutAsync($"/rest/workspaces/{Esc(workspaceName)}/datastores/{Esc(dataStoreName)}/reset", null);
        }

        /// <summary>
        /// 上传文件到数据存储（例如 shapefile、properties 文件）
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <param name="fileFormat">文件格式扩展名（例如 shp、properties）</param>
        /// <param name="fileContent">文件内容字节数组</param>
        /// <param name="contentType">内容类型（例如 shapefile 使用 application/zip）</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UploadFileAsync(string workspaceName, string dataStoreName, string fileFormat, byte[] fileContent, string contentType = "application/octet-stream")
        {
            await PutContentAsync(
                $"/rest/workspaces/{Esc(workspaceName)}/datastores/{Esc(dataStoreName)}/file.{Esc(fileFormat)}",
                BytesContent(fileContent, contentType));
        }
    }
}
