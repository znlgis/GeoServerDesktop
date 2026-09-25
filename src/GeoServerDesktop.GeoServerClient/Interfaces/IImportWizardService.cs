using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Import;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IImportWizardService 的服务接口（M5 接口化）：与实现类 ImportWizardService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IImportWizardService
    {
        /// <summary>
        /// 探测 PostGIS 连接可用性：创建临时存储并触发一次真实读库（列出要素类型），随后清理。
        /// 连接不可用时 GeoServer 侧读取会失败（HTTP 5xx），据此判定。
        /// </summary>
        /// <param name="workspaceName">用于承载临时探测存储的工作空间（需已存在）。</param>
        /// <param name="parameters">PostGIS 连接参数。</param>
        /// <returns>探测结果。</returns>
        Task<PostgisProbeResult> ProbePostgisConnectionAsync(string workspaceName, PostgisConnectionParameters parameters);

        /// <summary>
        /// 按数据源类型分发发布。
        /// </summary>
        /// <param name="request">导入源描述。</param>
        /// <returns>发布结果。</returns>
        Task<PublishResult> PublishAsync(ImportSourceRequest request);

        /// <summary>
        /// 发布 Shapefile 图层：创建/复用 Shapefile 数据存储（url=file: 引用）并发布要素类型。
        /// </summary>
        /// <param name="request">导入源描述（FileRef 必填）。</param>
        /// <returns>发布结果。</returns>
        Task<PublishResult> PublishShapefileAsync(ImportSourceRequest request);

        /// <summary>
        /// 发布 GeoTIFF 覆盖度：创建/复用覆盖存储（url=file: 引用）并发布覆盖度。
        /// </summary>
        /// <param name="request">导入源描述（FileRef 必填）。</param>
        /// <returns>发布结果。</returns>
        Task<PublishResult> PublishGeoTiffAsync(ImportSourceRequest request);

        /// <summary>
        /// 发布 PostGIS 表：创建/复用 PostGIS 数据存储并发布要素类型（nativeName=表名）。
        /// </summary>
        /// <param name="request">导入源描述（Postgis 连接参数必填）。</param>
        /// <returns>发布结果。</returns>
        Task<PublishResult> PublishPostgisAsync(ImportSourceRequest request);
    }
}
