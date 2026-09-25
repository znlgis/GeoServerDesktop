using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// ISettingsService 的服务接口（M5 接口化）：与实现类 SettingsService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// 获取 GeoServer 全局设置
        /// </summary>
        /// <returns>全局设置</returns>
        Task<GlobalSettings> GetGlobalSettingsAsync();

        /// <summary>
        /// 更新 GeoServer 全局设置
        /// </summary>
        /// <param name="settings">更新后的全局设置</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateGlobalSettingsAsync(GlobalSettings settings);

        /// <summary>
        /// 从全局设置中获取联系信息
        /// </summary>
        /// <returns>联系信息</returns>
        Task<ContactInfoWrapper> GetContactInfoAsync();

        /// <summary>
        /// 更新全局设置中的联系信息
        /// </summary>
        /// <param name="contact">更新后的联系信息</param>
        /// <returns>表示异步操作的任务</returns>
        Task UpdateContactInfoAsync(ContactInfo contact);
    }
}
