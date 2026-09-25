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
    /// Service for managing bulk data imports
    /// </summary>
    public class ImporterService : ServiceBase, IImporterService
    {

        /// <summary>
        /// 初始化 ImporterService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public ImporterService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// 探测 Importer 扩展可用性（环境探测）：GET /rest/imports。
        /// 404 → 扩展未安装（3.0.1 默认镜像基线）；2xx → 可用；其它错误 → Unknown。
        /// </summary>
        /// <returns>探测结果（状态与说明）。</returns>
        public async Task<ImporterAvailability> ProbeAvailabilityAsync()
        {
            try
            {
                await Http.GetAsync("/rest/imports.json");
                return new ImporterAvailability
                {
                    State = ImporterAvailabilityState.Available,
                    Message = "Importer 扩展可用",
                };
            }
            catch (GeoServerRequestException ex)
            {
                if (ex.StatusCode == 404)
                {
                    return new ImporterAvailability
                    {
                        State = ImporterAvailabilityState.NotInstalled,
                        Message = "Importer 扩展未安装（HTTP 404）；将使用内置发布向导",
                    };
                }
                return new ImporterAvailability
                {
                    State = ImporterAvailabilityState.Unknown,
                    Message = "探测失败（HTTP " + ex.StatusCode + "）",
                };
            }
            catch (Exception ex)
            {
                return new ImporterAvailability
                {
                    State = ImporterAvailabilityState.Unknown,
                    Message = "探测失败：" + ex.Message,
                };
            }
        }

        /// <summary>
        /// Creates a new import context。
        /// FIXED-E14：请求体复用 GeoServerJson.Request（NullValueHandling.Ignore），
        /// 不传 targetStore 时不再发送 "targetStore":null。
        /// 注：3.0.1 默认镜像未安装 importer 扩展（/rest/imports 实测 404），集成基线维持"扩展未装"。
        /// </summary>
        /// <param name="targetWorkspace">Target workspace name</param>
        /// <param name="targetStore">Optional target store name</param>
        /// <returns>Import context</returns>
        public async Task<ImportContextWrapper> CreateImportAsync(string targetWorkspace, string targetStore = null)
        {
            var import = new
            {
                import = new
                {
                    targetWorkspace = new { name = targetWorkspace },
                    targetStore = targetStore != null ? new { name = targetStore } : null
                }
            };
            using (var content = JsonContent(import))
            {
                var response = await Http.PostAsync("/rest/imports", content);
                return Parse<ImportContextWrapper>(response);
            }
        }

        /// <summary>
        /// Gets an import context
        /// </summary>
        /// <param name="importId">Import ID</param>
        /// <returns>Import context</returns>
        public async Task<ImportContextWrapper> GetImportAsync(int importId)
        {
            var response = await Http.GetAsync($"/rest/imports/{importId}.json");
            return JsonConvert.DeserializeObject<ImportContextWrapper>(response);
        }

        /// <summary>
        /// Deletes an import context
        /// </summary>
        /// <param name="importId">Import ID</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DeleteImportAsync(int importId)
        {
            await Http.DeleteAsync($"/rest/imports/{importId}");
        }

        /// <summary>
        /// Gets the tasks for an import
        /// </summary>
        /// <param name="importId">Import ID</param>
        /// <returns>List of tasks</returns>
        public async Task<string> GetImportTasksAsync(int importId)
        {
            return await Http.GetAsync($"/rest/imports/{importId}/tasks.json");
        }

        /// <summary>
        /// Uploads data to an import task
        /// </summary>
        /// <param name="importId">Import ID</param>
        /// <param name="taskId">Task ID</param>
        /// <param name="data">Data to upload</param>
        /// <param name="contentType">Content type</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UploadDataAsync(int importId, int taskId, byte[] data, string contentType = "application/octet-stream")
        {
            using (var content = new ByteArrayContent(data))
            {
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                await Http.PutAsync($"/rest/imports/{importId}/tasks/{taskId}/data", content);
            }
        }
    }
}
