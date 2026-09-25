using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// ITransformService 的服务接口（M5 接口化）：与实现类 TransformService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface ITransformService
    {
        /// <summary>
        /// Gets the list of available transforms
        /// </summary>
        /// <returns>List of transforms</returns>
        Task<TransformListWrapper> GetTransformsAsync();

        /// <summary>
        /// Gets a specific transform
        /// </summary>
        /// <param name="transformName">Transform name</param>
        /// <returns>Transform content</returns>
        Task<string> GetTransformAsync(string transformName);

        /// <summary>
        /// Creates a new transform
        /// </summary>
        /// <param name="transformName">Transform name</param>
        /// <param name="xsltContent">XSLT content</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateTransformAsync(string transformName, string xsltContent);

        /// <summary>
        /// Deletes a transform
        /// </summary>
        /// <param name="transformName">Transform name</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteTransformAsync(string transformName);
    }
}
