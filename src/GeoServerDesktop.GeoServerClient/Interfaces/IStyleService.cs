using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IStyleService 的服务接口（M5 接口化）：与实现类 StyleService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IStyleService
    {
        /// <summary>
        /// 获取所有样式的列表
        /// </summary>
        /// <returns>样式数组</returns>
        Task<Style[]> GetStylesAsync();

        /// <summary>
        /// 获取特定样式的详细信息
        /// </summary>
        /// <param name="styleName">样式的名称</param>
        /// <returns>样式详细信息</returns>
        Task<Style> GetStyleAsync(string styleName);

        /// <summary>
        /// 获取特定样式的 SLD 内容
        /// </summary>
        /// <param name="styleName">样式的名称</param>
        /// <returns>SLD 内容字符串</returns>
        Task<string> GetStyleSldAsync(string styleName);

        /// <summary>
        /// 创建新样式
        /// </summary>
        /// <param name="styleName">要创建的样式名称</param>
        /// <param name="sldContent">样式的 SLD 内容</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateStyleAsync(string styleName, string sldContent);

        /// <summary>
        /// 更新现有样式
        /// </summary>
        /// <param name="styleName">样式的名称</param>
        /// <param name="sldContent">更新后的 SLD 内容</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateStyleAsync(string styleName, string sldContent);

        /// <summary>
        /// 删除样式
        /// </summary>
        /// <param name="styleName">要删除的样式名称</param>
        /// <param name="purge">是否从磁盘清除样式文件</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteStyleAsync(string styleName, bool purge = false);

        /// <summary>
        /// 获取特定工作空间中的样式列表
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <returns>工作空间中的样式数组</returns>
        Task<Style[]> GetWorkspaceStylesAsync(string workspaceName);

        /// <summary>
        /// 获取工作空间中特定样式的详细信息
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="styleName">样式的名称</param>
        /// <returns>样式详细信息</returns>
        Task<Style> GetWorkspaceStyleAsync(string workspaceName, string styleName);

        /// <summary>
        /// 获取工作空间中特定样式的 SLD 内容
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="styleName">样式的名称</param>
        /// <returns>SLD 内容字符串</returns>
        Task<string> GetWorkspaceStyleSldAsync(string workspaceName, string styleName);

        /// <summary>
        /// 在特定工作空间中创建新样式
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="styleName">要创建的样式名称</param>
        /// <param name="sldContent">样式的 SLD 内容</param>
        /// <returns>表示异步操作的任务</returns>
        Task CreateWorkspaceStyleAsync(string workspaceName, string styleName, string sldContent);

        /// <summary>
        /// 更新工作空间中的现有样式
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="styleName">样式的名称</param>
        /// <param name="sldContent">更新后的 SLD 内容</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateWorkspaceStyleAsync(string workspaceName, string styleName, string sldContent);

        /// <summary>
        /// 从工作空间中删除样式
        /// </summary>
        /// <param name="workspaceName">工作空间的名称</param>
        /// <param name="styleName">要删除的样式名称</param>
        /// <param name="purge">是否从磁盘清除样式文件</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteWorkspaceStyleAsync(string workspaceName, string styleName, bool purge = false);
    }
}
