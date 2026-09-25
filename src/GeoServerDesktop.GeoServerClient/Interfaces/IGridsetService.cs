using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IGridsetService 的服务接口（M5 接口化）：与实现类 GridsetService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IGridsetService
    {
        /// <summary>
        /// Gets the list of gridsets
        /// </summary>
        /// <returns>List of gridsets</returns>
        Task<GridsetListWrapper> GetGridsetsAsync();

        /// <summary>
        /// 获取特定网格集的详细信息
        /// </summary>
        /// <param name="gridsetName">Gridset name</param>
        /// <returns>网格集详细信息</returns>
        Task<Gridset> GetGridsetAsync(string gridsetName);

        /// <summary>
        /// 创建新的网格集
        /// </summary>
        /// <param name="gridset">Gridset to create</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateGridsetAsync(Gridset gridset);

        /// <summary>
        /// 删除网格集
        /// </summary>
        /// <param name="gridsetName">Gridset name</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteGridsetAsync(string gridsetName);
    }
}
