using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IDataStoreService 的服务接口（M5 接口化）：与实现类 DataStoreService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IDataStoreService
    {
        /// <summary>
        /// 获取工作空间中的数据存储列表
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <returns>数据存储数组</returns>
        Task<DataStore[]> GetDataStoresAsync(string workspaceName);

        /// <summary>
        /// 获取特定数据存储的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <returns>数据存储详细信息</returns>
        Task<DataStore> GetDataStoreAsync(string workspaceName, string dataStoreName);

        /// <summary>
        /// 创建新的数据存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStore">数据存储配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateDataStoreAsync(string workspaceName, DataStore dataStore);

        /// <summary>
        /// 更新现有的数据存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <param name="dataStore">更新后的数据存储配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateDataStoreAsync(string workspaceName, string dataStoreName, DataStore dataStore);

        /// <summary>
        /// 删除数据存储
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">要删除的数据存储名称</param>
        /// <param name="recurse">是否递归删除数据存储中的所有资源</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteDataStoreAsync(string workspaceName, string dataStoreName, bool recurse = false);

        /// <summary>
        /// 重置数据存储缓存
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <returns>表示异步操作的任务</returns>
        Task ResetDataStoreAsync(string workspaceName, string dataStoreName);

        /// <summary>
        /// 上传文件到数据存储（例如 shapefile、properties 文件）
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <param name="fileFormat">文件格式扩展名（例如 shp、properties）</param>
        /// <param name="fileContent">文件内容字节数组</param>
        /// <param name="contentType">内容类型（例如 shapefile 使用 application/zip）</param>
        /// <returns>表示异步操作的任务</returns>
        Task UploadFileAsync(string workspaceName, string dataStoreName, string fileFormat, byte[] fileContent, string contentType = "application/octet-stream");
    }
}
