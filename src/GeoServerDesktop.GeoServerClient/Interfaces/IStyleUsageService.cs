using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IStyleUsageService 的服务接口（M5 接口化）：与实现类 StyleUsageService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IStyleUsageService
    {
        /// <summary>
        /// 聚合样式使用总览：每个全局样式及其被哪些图层引用（作为默认样式）。
        /// </summary>
        /// <returns>使用情况数组（含未引用样式；顺序与样式列表一致）。</returns>
        Task<StyleUsage[]> GetStyleUsageAsync();

        /// <summary>
        /// 未引用样式名：无任何图层作为默认样式引用（删除这类样式不会触发在用保护）。
        /// </summary>
        /// <returns>未引用样式名数组。</returns>
        Task<string[]> GetUnusedStyleNamesAsync();
    }
}
