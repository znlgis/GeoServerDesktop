using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Import;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 数据导入向导（内置路径）数据面服务：不依赖 Importer 扩展，
    /// 提供 PostGIS 连接探测（经 GeoServer 侧试连）与三类数据源的发布编排
    /// （Shapefile 目录 / GeoTIFF / PostGIS 表 → store + 图层，幂等）。
    /// 与集成测试 fixture 同源的发布形态（参数键名、最小请求体）。
    /// </summary>
    public class ImportWizardService : ServiceBase, IImportWizardService
    {

        /// <summary>
        /// 初始化 ImportWizardService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public ImportWizardService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// 探测 PostGIS 连接可用性：创建临时存储并触发一次真实读库（列出要素类型），随后清理。
        /// 连接不可用时 GeoServer 侧读取会失败（HTTP 5xx），据此判定。
        /// </summary>
        /// <param name="workspaceName">用于承载临时探测存储的工作空间（需已存在）。</param>
        /// <param name="parameters">PostGIS 连接参数。</param>
        /// <returns>探测结果。</returns>
        public async Task<PostgisProbeResult> ProbePostgisConnectionAsync(string workspaceName, PostgisConnectionParameters parameters)
        {
            if (string.IsNullOrEmpty(workspaceName)) throw new ArgumentNullException(nameof(workspaceName));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            string probeStore = "_probe_pg_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var dsSvc = new DataStoreService(Http);
            try
            {
                await dsSvc.CreateDataStoreAsync(workspaceName, new DataStore
                {
                    Name = probeStore,
                    Type = "PostGIS",
                    Enabled = true,
                    ConnectionParameters = StoreParameterTemplates.BuildPostgisConnectionParameters(parameters),
                });

                // 触发真实读库：连接失败会在此抛出（5xx）。
                var ftSvc = new FeatureTypeService(Http);
                await ftSvc.GetFeatureTypesAsync(workspaceName, probeStore);

                return new PostgisProbeResult { Success = true, Message = "连接可用" };
            }
            catch (GeoServerRequestException ex)
            {
                return new PostgisProbeResult
                {
                    Success = false,
                    Message = "连接探测失败（HTTP " + ex.StatusCode + "）：" + Summarize(ex),
                };
            }
            catch (Exception ex)
            {
                return new PostgisProbeResult { Success = false, Message = "连接探测失败：" + ex.Message };
            }
            finally
            {
                try { await dsSvc.DeleteDataStoreAsync(workspaceName, probeStore); }
                catch { /* 清理尽力而为 */ }
            }
        }

        /// <summary>
        /// 按数据源类型分发发布。
        /// </summary>
        /// <param name="request">导入源描述。</param>
        /// <returns>发布结果。</returns>
        public Task<PublishResult> PublishAsync(ImportSourceRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            switch (request.Kind)
            {
                case ImportDataSourceKind.ShapefileDirectory:
                case ImportDataSourceKind.ShapefileFile:
                    return PublishShapefileAsync(request);
                case ImportDataSourceKind.GeoTiffFile:
                    return PublishGeoTiffAsync(request);
                case ImportDataSourceKind.Postgis:
                default:
                    return PublishPostgisAsync(request);
            }
        }

        /// <summary>
        /// 发布 Shapefile 图层：创建/复用 Shapefile 数据存储（url=file: 引用）并发布要素类型。
        /// </summary>
        /// <param name="request">导入源描述（FileRef 必填）。</param>
        /// <returns>发布结果。</returns>
        public async Task<PublishResult> PublishShapefileAsync(ImportSourceRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var result = NewResult(request);
            try
            {
                var dsSvc = new DataStoreService(Http);
                if (!await ExistsAsync(() => dsSvc.GetDataStoreAsync(request.Workspace, result.StoreName)))
                {
                    await dsSvc.CreateDataStoreAsync(request.Workspace, new DataStore
                    {
                        Name = result.StoreName,
                        Type = "Shapefile",
                        Enabled = true,
                        ConnectionParameters = StoreParameterTemplates.BuildShapefileConnectionParameters(request.FileRef),
                    });
                }

                var ftSvc = new FeatureTypeService(Http);
                if (!await ExistsAsync(() => ftSvc.GetFeatureTypeAsync(request.Workspace, result.StoreName, request.LayerName)))
                {
                    await ftSvc.CreateFeatureTypeAsync(request.Workspace, result.StoreName, new FeatureType
                    {
                        Name = request.LayerName,
                        NativeName = NativeOf(request),
                        Srs = request.Srs,
                        Enabled = true,
                        Namespace = new NamespaceReference { Name = request.Workspace },
                    });
                }

                result.Success = true;
                result.Message = "发布成功";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = Describe(ex);
            }
            return result;
        }

        /// <summary>
        /// 发布 GeoTIFF 覆盖度：创建/复用覆盖存储（url=file: 引用）并发布覆盖度。
        /// </summary>
        /// <param name="request">导入源描述（FileRef 必填）。</param>
        /// <returns>发布结果。</returns>
        public async Task<PublishResult> PublishGeoTiffAsync(ImportSourceRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var result = NewResult(request);
            try
            {
                var csSvc = new CoverageStoreService(Http);
                if (!await ExistsAsync(() => csSvc.GetCoverageStoreAsync(request.Workspace, result.StoreName)))
                {
                    await csSvc.CreateCoverageStoreAsync(request.Workspace, new CoverageStore
                    {
                        Name = result.StoreName,
                        Type = "GeoTIFF",
                        Enabled = true,
                        Workspace = new WorkspaceReference { Name = request.Workspace },
                        Url = request.FileRef,
                    });
                }

                var covSvc = new CoverageService(Http);
                if (!await ExistsAsync(() => covSvc.GetCoverageAsync(request.Workspace, result.StoreName, request.LayerName)))
                {
                    await covSvc.CreateCoverageAsync(request.Workspace, result.StoreName, new Coverage
                    {
                        Name = request.LayerName,
                        NativeName = NativeOf(request),
                        Srs = request.Srs,
                        Enabled = true,
                    });
                }

                result.Success = true;
                result.Message = "发布成功";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = Describe(ex);
            }
            return result;
        }

        /// <summary>
        /// 发布 PostGIS 表：创建/复用 PostGIS 数据存储并发布要素类型（nativeName=表名）。
        /// </summary>
        /// <param name="request">导入源描述（Postgis 连接参数必填）。</param>
        /// <returns>发布结果。</returns>
        public async Task<PublishResult> PublishPostgisAsync(ImportSourceRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var result = NewResult(request);
            try
            {
                if (request.Postgis == null)
                    throw new ArgumentException("缺少 PostGIS 连接参数");

                var dsSvc = new DataStoreService(Http);
                if (!await ExistsAsync(() => dsSvc.GetDataStoreAsync(request.Workspace, result.StoreName)))
                {
                    await dsSvc.CreateDataStoreAsync(request.Workspace, new DataStore
                    {
                        Name = result.StoreName,
                        Type = "PostGIS",
                        Enabled = true,
                        ConnectionParameters = StoreParameterTemplates.BuildPostgisConnectionParameters(request.Postgis),
                    });
                }

                var ftSvc = new FeatureTypeService(Http);
                if (!await ExistsAsync(() => ftSvc.GetFeatureTypeAsync(request.Workspace, result.StoreName, request.LayerName)))
                {
                    await ftSvc.CreateFeatureTypeAsync(request.Workspace, result.StoreName, new FeatureType
                    {
                        Name = request.LayerName,
                        NativeName = NativeOf(request),
                        Srs = request.Srs,
                        Enabled = true,
                        Namespace = new NamespaceReference { Name = request.Workspace },
                    });
                }

                result.Success = true;
                result.Message = "发布成功";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = Describe(ex);
            }
            return result;
        }

        // ---------- 内部辅助 ----------

        /// <summary>原始名称：未显式指定时与发布名相同（发布名需全局唯一，原始名指向磁盘文件/数据库表）。</summary>
        private static string NativeOf(ImportSourceRequest request)
        {
            return string.IsNullOrEmpty(request.NativeName) ? request.LayerName : request.NativeName;
        }

        private static PublishResult NewResult(ImportSourceRequest request)
        {
            if (string.IsNullOrEmpty(request.Workspace)) throw new ArgumentException("缺少目标工作空间", nameof(request));
            if (string.IsNullOrEmpty(request.LayerName)) throw new ArgumentException("缺少图层名", nameof(request));
            var storeName = string.IsNullOrEmpty(request.StoreName) ? request.LayerName : request.StoreName;
            return new PublishResult
            {
                StoreName = storeName,
                LayerName = request.LayerName,
                QualifiedName = request.Workspace + ":" + request.LayerName,
            };
        }

        /// <summary>
        /// 存在性探测：GET 成功 → true；404 → false；其他异常（5xx/解析失败等）向上传播。
        /// 原兜底 catch 会把反序列化异常与 5xx 误判为“不存在”，引发误导性的重复创建（coverage 幂等重入实测暴露）。
        /// </summary>
        private static async Task<bool> ExistsAsync(Func<Task> probe)
        {
            try
            {
                await probe();
                return true;
            }
            catch (GeoServerRequestException ex) when (ex.StatusCode == 404)
            {
                return false;
            }
        }

        private static string Describe(Exception ex)
        {
            var gs = ex as GeoServerRequestException;
            if (gs != null) return "HTTP " + gs.StatusCode + "：" + Summarize(gs);
            return ex.Message;
        }

        private static string Summarize(GeoServerRequestException ex)
        {
            var text = ex.ResponseContent;
            if (string.IsNullOrWhiteSpace(text)) return ex.Message;
            text = text.Trim();
            return text.Length > 300 ? text.Substring(0, 300) + "…" : text;
        }
    }
}
