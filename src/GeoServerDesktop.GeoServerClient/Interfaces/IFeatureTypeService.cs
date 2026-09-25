using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IFeatureTypeService 的服务接口（M5 接口化）：与实现类 FeatureTypeService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IFeatureTypeService
    {
        /// <summary>
        /// 获取数据存储中的要素类型列表
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <returns>要素类型数组</returns>
        Task<FeatureType[]> GetFeatureTypesAsync(string workspaceName, string dataStoreName);

        /// <summary>
        /// 获取特定要素类型的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <param name="featureTypeName">要素类型的名称</param>
        /// <returns>要素类型详细信息</returns>
        Task<FeatureType> GetFeatureTypeAsync(string workspaceName, string dataStoreName, string featureTypeName);

        /// <summary>
        /// 发布新的要素类型（从要素类型创建图层）
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <param name="featureType">要素类型配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateFeatureTypeAsync(string workspaceName, string dataStoreName, FeatureType featureType);

        /// <summary>
        /// 更新现有的要素类型
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <param name="featureTypeName">要素类型的名称</param>
        /// <param name="featureType">更新后的要素类型配置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateFeatureTypeAsync(string workspaceName, string dataStoreName, string featureTypeName, FeatureType featureType);

        /// <summary>
        /// 删除要素类型
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="dataStoreName">数据存储的名称</param>
        /// <param name="featureTypeName">要删除的要素类型名称</param>
        /// <param name="recurse">是否递归删除与要素类型相关的所有资源</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteFeatureTypeAsync(string workspaceName, string dataStoreName, string featureTypeName, bool recurse = false);
    }
}
