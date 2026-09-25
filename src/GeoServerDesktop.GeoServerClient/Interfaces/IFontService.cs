using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IFontService 的服务接口（M5 接口化）：与实现类 FontService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IFontService
    {
        /// <summary>
        /// Gets the list of available fonts
        /// </summary>
        /// <returns>List of fonts</returns>
        Task<FontListWrapper> GetFontsAsync();

        /// <summary>
        /// Uploads a new font file
        /// </summary>
        /// <param name="fontName">Font name</param>
        /// <param name="fontContent">Font file content</param>
        /// <returns>表示异步操作的任务</returns>
        Task UploadFontAsync(string fontName, byte[] fontContent);
    }
}
