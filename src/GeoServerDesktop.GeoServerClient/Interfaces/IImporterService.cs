using System;
using System.Net.Http;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Import;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IImporterService 的服务接口（M5 接口化）：与实现类 ImporterService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IImporterService
    {
        /// <summary>
        /// 探测 Importer 扩展可用性（环境探测）：GET /rest/imports。
        /// 404 → 扩展未安装（3.0.1 默认镜像基线）；2xx → 可用；其它错误 → Unknown。
        /// </summary>
        /// <returns>探测结果（状态与说明）。</returns>
        Task<ImporterAvailability> ProbeAvailabilityAsync();

        /// <summary>
        /// Creates a new import context。
        /// FIXED-E14：请求体复用 GeoServerJson.Request（NullValueHandling.Ignore），
        /// 不传 targetStore 时不再发送 "targetStore":null。
        /// 注：3.0.1 默认镜像未安装 importer 扩展（/rest/imports 实测 404），集成基线维持"扩展未装"。
        /// </summary>
        /// <param name="targetWorkspace">Target workspace name</param>
        /// <param name="targetStore">Optional target store name</param>
        /// <returns>Import context</returns>
        Task<ImportContextWrapper> CreateImportAsync(string targetWorkspace, string targetStore = null);

        /// <summary>
        /// Gets an import context
        /// </summary>
        /// <param name="importId">Import ID</param>
        /// <returns>Import context</returns>
        Task<ImportContextWrapper> GetImportAsync(int importId);

        /// <summary>
        /// Deletes an import context
        /// </summary>
        /// <param name="importId">Import ID</param>
        /// <returns>表示异步操作的任务</returns>
        Task DeleteImportAsync(int importId);

        /// <summary>
        /// Gets the tasks for an import
        /// </summary>
        /// <param name="importId">Import ID</param>
        /// <returns>List of tasks</returns>
        Task<string> GetImportTasksAsync(int importId);

        /// <summary>
        /// Uploads data to an import task
        /// </summary>
        /// <param name="importId">Import ID</param>
        /// <param name="taskId">Task ID</param>
        /// <param name="data">Data to upload</param>
        /// <param name="contentType">Content type</param>
        /// <returns>表示异步操作的任务</returns>
        Task UploadDataAsync(int importId, int taskId, byte[] data, string contentType = "application/octet-stream");
    }
}
