using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeoServerDesktop.GeoServerClient.Migration
{
    /// <summary>
    /// ISettingsCompareService 的服务接口（M5 接口化）：与实现类 SettingsCompareService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface ISettingsCompareService
    {
        /// <summary>读取本实例指定域的设置原始 JSON。</summary>
        /// <param name="domain">域。</param>
        /// <returns>原始 JSON。</returns>
        Task<string> ReadRawAsync(SettingsDomain domain);

        /// <summary>
        /// 读取任意路径的原始 JSON（工作空间级设置等同族端点）。
        /// </summary>
        /// <param name="getPath">GET 路径。</param>
        /// <returns>原始 JSON。</returns>
        Task<string> ReadRawAtPathAsync(string getPath);

        /// <summary>
        /// 整包 PUT 任意设置路径（工作空间级设置等同族端点；载荷须为含根包装的完整 JSON）。
        /// </summary>
        /// <param name="putPath">PUT 路径（无 .json 后缀）。</param>
        /// <param name="wrappedJson">含根包装的完整 JSON。</param>
        /// <returns>表示异步操作的任务。</returns>
        Task PutRawAsync(string putPath, string wrappedJson);

        /// <summary>把勾选路径从源侧应用到本（目标）实例：合并整包后 PUT。</summary>
        /// <param name="sourceJson">源实例原始 JSON。</param>
        /// <param name="domain">域。</param>
        /// <param name="paths">勾选的差异路径。</param>
        /// <returns>表示异步操作的任务。</returns>
        Task ApplyAsync(string sourceJson, SettingsDomain domain, IEnumerable<string> paths);
    }
}
