using System;
using System.Collections.Generic;
using System.IO;
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

            // 发布前预检：GeoServer 对结构性损坏的 shapefile 一律返回 201（实测），缺陷要到服务查询/出图
            // 才暴露，因此阻断级问题必须由客户端拦下；编码/投影等风险以告警形式随结果返回。
            List<string> preErrors, preWarnings;
            PreflightShapefile(request, out preErrors, out preWarnings);
            result.PreflightErrors = preErrors;
            result.Warnings = preWarnings;
            if (preErrors.Count > 0)
            {
                result.Success = false;
                result.Message = "发布前预检未通过：" + string.Join("；", preErrors);
                return result;
            }

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

                // 发布后验证：GeoServer 在存储侧匹配不到磁盘文件时（真实数据实测：含非 ASCII 基名的
                // shapefile 目录存储）会“另建一个空要素类型”并回 2xx——REST 里资源存在，但属性面为空、
                // WFS 服务侧不可用。只判 2xx 会让向导报“发布成功”而用户拿到空图层，故必须回读校验。
                var verify = await VerifyFeatureTypeAsync(ftSvc, request.Workspace, result.StoreName,
                    request.LayerName, NativeOf(request));
                if (verify != null)
                {
                    result.Success = false;
                    result.Message = verify;
                    return result;
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

            List<string> preErrors, preWarnings;
            PreflightGeoTiff(request, out preErrors, out preWarnings);
            result.PreflightErrors = preErrors;
            result.Warnings = preWarnings;
            if (preErrors.Count > 0)
            {
                result.Success = false;
                result.Message = "发布前预检未通过：" + string.Join("；", preErrors);
                return result;
            }

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

        /// <summary>
        /// 发布后回读校验：要素类型必须存在、启用，且服务端真的读到了数据（原生范围非退化）。
        /// 返回 null 表示通过；否则为可诊断原因。实测：GeoServer 在存储侧匹配不到磁盘文件时
        /// 会直接 400（含非 ASCII 基名的 shapefile），只判 2xx 会让向导误报“发布成功”。
        /// </summary>
        private static async Task<string> VerifyFeatureTypeAsync(FeatureTypeService ftSvc, string workspace,
            string store, string layer, string nativeName)
        {
            FeatureType ft;
            try
            {
                ft = await ftSvc.GetFeatureTypeAsync(workspace, store, layer);
            }
            catch (Exception ex)
            {
                return "发布后回读失败：REST 读不到 " + workspace + ":" + layer + "（" + Describe(ex) + "）";
            }
            if (ft == null) return "发布后回读为空：REST 未返回 " + workspace + ":" + layer + " 的要素类型";
            if (ft.Enabled == false) return "要素类型已创建但处于禁用状态，服务面不会发布该图层";

            // 属性面判据：除几何字段外还存在业务字段，才说明服务端真的把原始名解析成了磁盘数据。
            // 实测两种“假成功”形态：① 原始名匹配不到文件时 GeoServer 直接 400；
            // ② 匹配到但属性表不可读时建出只有几何字段的空类型——REST 里资源存在、图层却不可用。
            int business = 0;
            var attrs = ft.Attributes;
            if (attrs != null)
            {
                for (int i2 = 0; i2 < attrs.Count; i2++)
                {
                    var a = attrs[i2];
                    if (a == null || string.IsNullOrEmpty(a.Name)) continue;
                    if (string.Equals(a.Name, "the_geom", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(a.Name, "geom", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(a.Name, "geometry", StringComparison.OrdinalIgnoreCase)) continue;
                    business++;
                }
            }
            if (attrs == null || attrs.Count == 0)
                return "发布后服务端未回传属性面（attributes 缺失）：存储 " + store + " 可能没把原始名 \"" + nativeName
                    + "\" 匹配到磁盘数据（常见原因：基名含非 ASCII 或空格、目录存储未指向该文件所在目录）。";
            if (business == 0)
                return "发布后属性面只有几何字段：存储 " + store + " 未从 \"" + nativeName
                    + "\" 解析出业务字段（常见原因：缺 .dbf 或属性表不可读），图层可被列出但属性查询/GetFeatureInfo 不可用。";
            return null;
        }

        // ---------- 发布前预检 ----------

        /// <summary>
        /// 用本地副本做发布前预检（<see cref="ImportSourceRequest.LocalSourcePath"/> 为空时静默跳过）。
        /// 阻断级：缺 .dbf、SHP 头长与实际不符、DBF 记录数不符、.prj 非法 WKT（服务端一律接受，必须由客户端拦）。
        /// 告警级：属性编码风险（缺 .cpg 的非 ASCII 数据）、缺 .prj / 无 EPSG 权威码、缺 .shx 索引。
        /// </summary>
        private static void PreflightShapefile(ImportSourceRequest request, out List<string> errors, out List<string> warnings)
        {
            errors = new List<string>();
            warnings = new List<string>();
            string local = request == null ? null : request.LocalSourcePath;
            if (string.IsNullOrWhiteSpace(local)) return;

            string shp = ResolveLocalShp(local, NativeOf(request));
            if (shp == null)
            {
                errors.Add("本地预检失败：在 " + local + " 下找不到 " + NativeOf(request) + ".shp，无法核对数据完整性。");
                return;
            }

            GeoServerDesktop.GeoServerClient.Import.ShapefilePreview p;
            try
            {
                p = GeoServerDesktop.GeoServerClient.Import.GeoFileInspector.InspectShapefile(shp);
            }
            catch (Exception ex)
            {
                errors.Add("本地预检失败：" + ex.Message);
                return;
            }

            foreach (var e in GeoServerDesktop.GeoServerClient.Import.GeoFileInspector.IntegrityErrors(p)) errors.Add(e);
            if ((p.IntegrityRisk & GeoServerDesktop.GeoServerClient.Import.ShapefileIntegrityRisk.MissingShx) != 0)
                warnings.Add("缺少 .shx 索引：数据仍可读，但空间过滤会退化为全量扫描（大文件建议重建索引）。");
            if (p.Warnings != null)
                foreach (var w in p.Warnings)
                    if (!errors.Contains(w) && !warnings.Contains(w)) warnings.Add(w);
        }

        /// <summary>栅格本地预检：FileRef 指向服务器侧文件，客户端只能在持有本地副本时核对 TIFF 头。</summary>
        private static void PreflightGeoTiff(ImportSourceRequest request, out List<string> errors, out List<string> warnings)
        {
            errors = new List<string>();
            warnings = new List<string>();
            string local = request == null ? null : request.LocalSourcePath;
            if (string.IsNullOrWhiteSpace(local) || !File.Exists(local)) return;
            try
            {
                GeoServerDesktop.GeoServerClient.Import.GeoFileInspector.InspectGeoTiff(local);
            }
            catch (Exception ex)
            {
                errors.Add("栅格本地预检失败：" + ex.Message);
            }
        }

        /// <summary>本地路径 → 待检 .shp：给文件则直接用（须为 .shp）；给目录则按原始名在目录内定位。</summary>
        private static string ResolveLocalShp(string local, string nativeName)
        {
            try
            {
                if (File.Exists(local))
                    return local.EndsWith(".shp", StringComparison.OrdinalIgnoreCase) ? local : null;
                if (!Directory.Exists(local) || string.IsNullOrEmpty(nativeName)) return null;
                var direct = Path.Combine(local, nativeName + ".shp");
                if (File.Exists(direct)) return direct;
                foreach (var f in Directory.GetFiles(local, "*.shp", SearchOption.TopDirectoryOnly))
                    if (string.Equals(Path.GetFileNameWithoutExtension(f), nativeName, StringComparison.OrdinalIgnoreCase))
                        return f;
                return null;
            }
            catch { return null; }
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


        private static string Summarize(GeoServerRequestException ex)
        {
            var text = ex.ResponseContent;
            if (string.IsNullOrWhiteSpace(text)) return ex.Message;
            text = text.Trim();
            return text.Length > 300 ? text.Substring(0, 300) + "…" : text;
        }
    }
}
