using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// INamespaceService 的服务接口（M5 接口化）：与实现类 NamespaceService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface INamespaceService
    {
        /// <summary>
        /// 获取所有命名空间的列表
        /// </summary>
        /// <returns>命名空间数组</returns>
        Task<Namespace[]> GetNamespacesAsync();

        /// <summary>
        /// 获取特定命名空间的详细信息
        /// </summary>
        /// <param name="namespacePrefix">命名空间的前缀</param>
        /// <returns>命名空间详细信息</returns>
        Task<Namespace> GetNamespaceAsync(string namespacePrefix);

        /// <summary>
        /// 创建新的命名空间
        /// </summary>
        /// <param name="namespacePrefix">命名空间的前缀</param>
        /// <param name="uri">命名空间的 URI</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateNamespaceAsync(string namespacePrefix, string uri);

        /// <summary>
        /// 更新现有的命名空间
        /// </summary>
        /// <param name="namespacePrefix">要更新的命名空间前缀</param>
        /// <param name="uri">命名空间的新 URI</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateNamespaceAsync(string namespacePrefix, string uri);

        /// <summary>
        /// 删除命名空间
        /// </summary>
        /// <param name="namespacePrefix">要删除的命名空间前缀</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteNamespaceAsync(string namespacePrefix);
    }
}
