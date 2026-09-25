using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
    /// IWorkspaceMigrationService 的服务接口（M5 接口化）：与实现类 WorkspaceMigrationService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IWorkspaceMigrationService
    {
        /// <summary>
        /// 导出工作空间为迁移归档（内存 ZIP 字节 + 清单）。
        /// </summary>
        /// <param name="workspaceName">工作空间名。</param>
        /// <returns>导出结果。</returns>
        Task<WorkspaceExportResult> ExportWorkspaceAsync(string workspaceName);

        /// <summary>
        /// 从归档导入（重建）工作空间。幂等：已存在资源默认跳过，OverwriteExisting 时更新。
        /// </summary>
        /// <param name="request">导入请求。</param>
        /// <returns>导入结果（逐项回放状态）。</returns>
        Task<WorkspaceImportResult> ImportWorkspaceAsync(WorkspaceImportRequest request);
    }
}
