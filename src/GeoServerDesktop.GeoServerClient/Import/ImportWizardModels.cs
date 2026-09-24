using System.Collections.Generic;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Import
{
    /// <summary>导入向导：发布参数模板中的单个参数定义。</summary>
    public sealed class StoreParameterField
    {
        /// <summary>参数键（GeoServer 连接参数键名，如 url/host）。</summary>
        public string Key { get; set; }

        /// <summary>展示名（供 UI 表单标签）。</summary>
        public string DisplayName { get; set; }

        /// <summary>是否必填。</summary>
        public bool Required { get; set; }

        /// <summary>默认值（可空）。</summary>
        public string DefaultValue { get; set; }

        /// <summary>说明。</summary>
        public string Description { get; set; }

        /// <summary>示例值。</summary>
        public string Example { get; set; }
    }

    /// <summary>导入向导：PostGIS 连接参数（强类型）。</summary>
    public sealed class PostgisConnectionParameters
    {
        /// <summary>数据库主机（GeoServer 容器可达地址，如 postgis）。</summary>
        public string Host { get; set; }

        /// <summary>端口（默认 5432）。</summary>
        public int Port { get; set; }

        /// <summary>数据库名。</summary>
        public string Database { get; set; }

        /// <summary>用户名。</summary>
        public string User { get; set; }

        /// <summary>密码。</summary>
        public string Password { get; set; }

        /// <summary>初始化 PostgisConnectionParameters（端口默认 5432）。</summary>
        public PostgisConnectionParameters()
        {
            Port = 5432;
        }
    }

    /// <summary>导入向导：PostGIS 连接探测结果（经 GeoServer 侧试连）。</summary>
    public sealed class PostgisProbeResult
    {
        /// <summary>连接是否可用。</summary>
        public bool Success { get; set; }

        /// <summary>结果说明（失败原因或成功提示）。</summary>
        public string Message { get; set; }
    }

    /// <summary>导入向导：发布结果。</summary>
    public sealed class PublishResult
    {
        /// <summary>是否成功。</summary>
        public bool Success { get; set; }

        /// <summary>存储名。</summary>
        public string StoreName { get; set; }

        /// <summary>图层/覆盖度名。</summary>
        public string LayerName { get; set; }

        /// <summary>发布资源全名（workspace:layer），供构造 WMS/WFS 预览链接。</summary>
        public string QualifiedName { get; set; }

        /// <summary>说明（失败原因或成功提示）。</summary>
        public string Message { get; set; }
    }

    /// <summary>导入向导：Importer 扩展可用性状态。</summary>
    public enum ImporterAvailabilityState
    {
        /// <summary>可用（/rest/imports 响应正常）。</summary>
        Available = 0,

        /// <summary>未安装（404；3.0.1 默认镜像基线）。</summary>
        NotInstalled = 1,

        /// <summary>探测失败（网络/认证等其它错误）。</summary>
        Unknown = 2,
    }

    /// <summary>导入向导：Importer 扩展可用性探测结果。</summary>
    public sealed class ImporterAvailability
    {
        /// <summary>状态。</summary>
        public ImporterAvailabilityState State { get; set; }

        /// <summary>说明。</summary>
        public string Message { get; set; }

        /// <summary>是否可用。</summary>
        public bool IsAvailable
        {
            get { return State == ImporterAvailabilityState.Available; }
        }
    }

    /// <summary>导入向导：发布数据源的完整描述（供服务方法与 UI 传递）。</summary>
    public sealed class ImportSourceRequest
    {
        /// <summary>数据源类型。</summary>
        public ImportDataSourceKind Kind { get; set; }

        /// <summary>file: 引用（Shapefile 目录 / GeoTIFF 文件），PostGIS 时为空。</summary>
        public string FileRef { get; set; }

        /// <summary>发布名（全局唯一；Shapefile/GeoTIFF/PostGIS 通用）。</summary>
        public string LayerName { get; set; }

        /// <summary>原始名称（Shapefile：磁盘 .shp 基名；GeoTIFF：栅格文件基名；PostGIS：表名；缺省时与 LayerName 相同）。</summary>
        public string NativeName { get; set; }

        /// <summary>目标工作空间。</summary>
        public string Workspace { get; set; }

        /// <summary>存储名（缺省时按图层名生成）。</summary>
        public string StoreName { get; set; }

        /// <summary>SRS（如 EPSG:4326；缺省时服务端按数据推断）。</summary>
        public string Srs { get; set; }

        /// <summary>PostGIS 连接参数（Kind=PostGIS 时使用）。</summary>
        public PostgisConnectionParameters Postgis { get; set; }
    }

    /// <summary>导入向导：模板与便捷构造。</summary>
    public static class StoreParameterTemplates
    {
        /// <summary>
        /// 按数据源类型给出数据存储（DataStore）发布参数模板。
        /// 实测依据：shapefile 存储必须显式声明 url（FIXED-E40：仅 namespace 会产生不可用空壳存储）；
        /// PostGIS 参数键沿用集成测试实测通过的键名（dbtype/host/port/database/user/passwd）。
        /// </summary>
        /// <param name="kind">数据源类型（ShapefileDirectory / Postgis 场景）。</param>
        /// <returns>参数定义列表。</returns>
        public static IList<StoreParameterField> ForDataStore(ImportDataSourceKind kind)
        {
            if (kind == ImportDataSourceKind.ShapefileDirectory || kind == ImportDataSourceKind.ShapefileFile)
            {
                return new List<StoreParameterField>
                {
                    new StoreParameterField
                    {
                        Key = "url", DisplayName = "数据目录引用", Required = true,
                        Description = "file: 引用，相对 GeoServer data_dir 或为其可达的绝对路径",
                        Example = "file:gdtest_data",
                    },
                    new StoreParameterField
                    {
                        Key = "namespace", DisplayName = "命名空间", Required = false,
                        Description = "缺省使用目标工作空间同名命名空间",
                        Example = "http://example.com",
                    },
                    new StoreParameterField
                    {
                        Key = "charset", DisplayName = "字符集", Required = false, DefaultValue = "UTF-8",
                        Description = "DBF 字符集（中文数据建议 GBK 或 UTF-8）",
                        Example = "UTF-8",
                    },
                };
            }

            return new List<StoreParameterField>
            {
                new StoreParameterField
                {
                    Key = "dbtype", DisplayName = "数据库类型", Required = true, DefaultValue = "postgis",
                    Description = "固定为 postgis",
                    Example = "postgis",
                },
                new StoreParameterField
                {
                    Key = "host", DisplayName = "主机", Required = true,
                    Description = "GeoServer 容器可达的数据库主机名",
                    Example = "postgis",
                },
                new StoreParameterField
                {
                    Key = "port", DisplayName = "端口", Required = true, DefaultValue = "5432",
                    Description = "数据库端口",
                    Example = "5432",
                },
                new StoreParameterField
                {
                    Key = "database", DisplayName = "数据库", Required = true,
                    Description = "数据库名",
                    Example = "postgres",
                },
                new StoreParameterField
                {
                    Key = "user", DisplayName = "用户", Required = true,
                    Description = "数据库用户",
                    Example = "postgres",
                },
                new StoreParameterField
                {
                    Key = "passwd", DisplayName = "密码", Required = true,
                    Description = "数据库密码（GeoServer 连接参数键名为 passwd）",
                    Example = "postgres",
                },
                new StoreParameterField
                {
                    Key = "Expose primary keys", DisplayName = "暴露主键", Required = false, DefaultValue = "true",
                    Description = "将主键列作为属性暴露",
                    Example = "true",
                },
            };
        }

        /// <summary>
        /// 按数据源类型给出覆盖存储（CoverageStore）发布参数模板（GeoTIFF）。
        /// </summary>
        /// <returns>参数定义列表。</returns>
        public static IList<StoreParameterField> ForCoverageStore()
        {
            return new List<StoreParameterField>
            {
                new StoreParameterField
                {
                    Key = "url", DisplayName = "栅格文件引用", Required = true,
                    Description = "file: 引用，指向 GeoServer 可达的 .tif/.tiff 文件",
                    Example = "file:gdtest_data/gdtest_dem.tif",
                },
            };
        }

        /// <summary>构造 Shapefile 数据存储连接参数（url 必填，namespace 可选）。</summary>
        /// <param name="fileRef">file: 引用。</param>
        /// <param name="namespace">命名空间（可空）。</param>
        /// <returns>连接参数。</returns>
        public static ConnectionParameters BuildShapefileConnectionParameters(string fileRef, string @namespace = null)
        {
            var entries = new List<ConnectionParameterEntry>
            {
                new ConnectionParameterEntry { Key = "url", Value = fileRef },
            };
            if (!string.IsNullOrEmpty(@namespace))
                entries.Add(new ConnectionParameterEntry { Key = "namespace", Value = @namespace });
            return new ConnectionParameters { Entries = entries.ToArray() };
        }

        /// <summary>构造 PostGIS 数据存储连接参数（键名沿用集成测试实测通过集合）。</summary>
        /// <param name="p">连接参数。</param>
        /// <returns>连接参数。</returns>
        public static ConnectionParameters BuildPostgisConnectionParameters(PostgisConnectionParameters p)
        {
            if (p == null) throw new System.ArgumentNullException(nameof(p));
            return new ConnectionParameters
            {
                Entries = new[]
                {
                    new ConnectionParameterEntry { Key = "dbtype", Value = "postgis" },
                    new ConnectionParameterEntry { Key = "host", Value = p.Host },
                    new ConnectionParameterEntry { Key = "port", Value = p.Port.ToString() },
                    new ConnectionParameterEntry { Key = "database", Value = p.Database },
                    new ConnectionParameterEntry { Key = "user", Value = p.User },
                    new ConnectionParameterEntry { Key = "passwd", Value = p.Password },
                    new ConnectionParameterEntry { Key = "Expose primary keys", Value = "true" },
                },
            };
        }
    }
}
