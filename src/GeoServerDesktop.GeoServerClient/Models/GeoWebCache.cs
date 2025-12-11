using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Wrapper for GeoWebCache layers list
    /// </summary>
    public class GWCLayerListWrapper
    {
        /// <summary>
        /// 获取或设置缓存图层列表
        /// </summary>
        [JsonProperty("layers")]
        public List<string> Layers { get; set; }
    }

    /// <summary>
    /// 表示 GeoWebCache 图层
    /// </summary>
    public class GWCLayer
    {
        /// <summary>
        /// 获取或设置图层名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置图层是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置 MIME 格式
        /// </summary>
        [JsonProperty("mimeFormats")]
        public List<string> MimeFormats { get; set; }

        /// <summary>
        /// 获取或设置网格集
        /// </summary>
        [JsonProperty("gridSubsets")]
        public List<GridSubset> GridSubsets { get; set; }

        /// <summary>
        /// 获取或设置元数据 URL
        /// </summary>
        [JsonProperty("metaWidthHeight")]
        public int[] MetaWidthHeight { get; set; }

        /// <summary>
        /// 获取或设置过期时间（秒）
        /// </summary>
        [JsonProperty("expireCache")]
        public int? ExpireCache { get; set; }

        /// <summary>
        /// 获取或设置过期客户端
        /// </summary>
        [JsonProperty("expireClients")]
        public int? ExpireClients { get; set; }
    }

    /// <summary>
    /// 表示网格子集
    /// </summary>
    public class GridSubset
    {
        /// <summary>
        /// 获取或设置网格集名称
        /// </summary>
        [JsonProperty("gridSetName")]
        public string GridSetName { get; set; }

        /// <summary>
        /// 获取或设置起始缩放级别 level
        /// </summary>
        [JsonProperty("zoomStart")]
        public int? ZoomStart { get; set; }

        /// <summary>
        /// 获取或设置结束缩放级别 level
        /// </summary>
        [JsonProperty("zoomStop")]
        public int? ZoomStop { get; set; }

        /// <summary>
        /// 获取或设置范围
        /// </summary>
        [JsonProperty("extent")]
        public Extent Extent { get; set; }
    }

    /// <summary>
    /// 表示范围/边界框
    /// </summary>
    public class Extent
    {
        /// <summary>
        /// 获取或设置坐标
        /// </summary>
        [JsonProperty("coords")]
        public double[] Coords { get; set; }
    }

    /// <summary>
    /// 表示种子请求
    /// </summary>
    public class SeedRequest
    {
        /// <summary>
        /// 获取或设置种子请求包装器
        /// </summary>
        [JsonProperty("seedRequest")]
        public SeedRequestConfig Config { get; set; }
    }

    /// <summary>
    /// Seed request configuration
    /// </summary>
    public class SeedRequestConfig
    {
        /// <summary>
        /// 获取或设置图层名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置网格集 ID
        /// </summary>
        [JsonProperty("gridSetId")]
        public string GridSetId { get; set; }

        /// <summary>
        /// 获取或设置起始缩放级别 level
        /// </summary>
        [JsonProperty("zoomStart")]
        public int ZoomStart { get; set; }

        /// <summary>
        /// 获取或设置结束缩放级别 level
        /// </summary>
        [JsonProperty("zoomStop")]
        public int ZoomStop { get; set; }

        /// <summary>
        /// 获取或设置格式
        /// </summary>
        [JsonProperty("format")]
        public string Format { get; set; }

        /// <summary>
        /// 获取或设置类型（seed、reseed、truncate）
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// 获取或设置线程数
        /// </summary>
        [JsonProperty("threadCount")]
        public int? ThreadCount { get; set; }
    }

    /// <summary>
    /// 表示磁盘配额配置
    /// </summary>
    public class DiskQuotaConfig
    {
        /// <summary>
        /// 获取或设置是否启用磁盘配额
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置磁盘块大小
        /// </summary>
        [JsonProperty("diskBlockSize")]
        public int? DiskBlockSize { get; set; }

        /// <summary>
        /// 获取或设置缓存清理频率
        /// </summary>
        [JsonProperty("cacheCleanUpFrequency")]
        public int? CacheCleanUpFrequency { get; set; }

        /// <summary>
        /// 获取或设置缓存清理单位
        /// </summary>
        [JsonProperty("cacheCleanUpUnits")]
        public string CacheCleanUpUnits { get; set; }

        /// <summary>
        /// 获取或设置最大缓存时间
        /// </summary>
        [JsonProperty("maxConcurrentCleanUps")]
        public int? MaxConcurrentCleanUps { get; set; }

        /// <summary>
        /// 获取或设置全局配额
        /// </summary>
        [JsonProperty("globalQuota")]
        public Quota GlobalQuota { get; set; }

        /// <summary>
        /// 获取或设置图层配额
        /// </summary>
        [JsonProperty("layerQuotas")]
        public List<LayerQuota> LayerQuotas { get; set; }
    }

    /// <summary>
    /// 表示配额值
    /// </summary>
    public class Quota
    {
        /// <summary>
        /// 获取或设置值
        /// </summary>
        [JsonProperty("value")]
        public double Value { get; set; }

        /// <summary>
        /// 获取或设置单位（B、KB、MB、GB、TB）
        /// </summary>
        [JsonProperty("units")]
        public string Units { get; set; }
    }

    /// <summary>
    /// 表示图层特定配额
    /// </summary>
    public class LayerQuota
    {
        /// <summary>
        /// 获取或设置图层名称
        /// </summary>
        [JsonProperty("layer")]
        public string Layer { get; set; }

        /// <summary>
        /// 获取或设置配额
        /// </summary>
        [JsonProperty("quota")]
        public Quota Quota { get; set; }
    }

    /// <summary>
    /// 表示网格集定义
    /// </summary>
    public class Gridset
    {
        /// <summary>
        /// 获取或设置网格集名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置空间参考系统
        /// </summary>
        [JsonProperty("srs")]
        public SRS SRS { get; set; }

        /// <summary>
        /// 获取或设置范围
        /// </summary>
        [JsonProperty("extent")]
        public Extent Extent { get; set; }

        /// <summary>
        /// 获取或设置对齐方式是否为左上角
        /// </summary>
        [JsonProperty("alignTopLeft")]
        public bool? AlignTopLeft { get; set; }

        /// <summary>
        /// 获取或设置分辨率
        /// </summary>
        [JsonProperty("resolutions")]
        public List<double> Resolutions { get; set; }

        /// <summary>
        /// 获取或设置每单位米数
        /// </summary>
        [JsonProperty("metersPerUnit")]
        public double? MetersPerUnit { get; set; }

        /// <summary>
        /// 获取或设置像素大小
        /// </summary>
        [JsonProperty("pixelSize")]
        public double? PixelSize { get; set; }

        /// <summary>
        /// 获取或设置比例尺名称
        /// </summary>
        [JsonProperty("scaleNames")]
        public List<string> ScaleNames { get; set; }

        /// <summary>
        /// 获取或设置瓦片高度
        /// </summary>
        [JsonProperty("tileHeight")]
        public int? TileHeight { get; set; }

        /// <summary>
        /// 获取或设置瓦片宽度
        /// </summary>
        [JsonProperty("tileWidth")]
        public int? TileWidth { get; set; }

        /// <summary>
        /// 获取或设置 Y 坐标是否向下递增
        /// </summary>
        [JsonProperty("yCoordinateFirst")]
        public bool? YCoordinateFirst { get; set; }
    }

    /// <summary>
    /// 表示空间参考系统信息
    /// </summary>
    public class SRS
    {
        /// <summary>
        /// 获取或设置空间参考系统 number
        /// </summary>
        [JsonProperty("number")]
        public int Number { get; set; }
    }

    /// <summary>
    /// 网格集的包装器s list
    /// </summary>
    public class GridsetListWrapper
    {
        /// <summary>
        /// 获取或设置网格集列表
        /// </summary>
        [JsonProperty("gridSets")]
        public List<string> GridSets { get; set; }
    }

    /// <summary>
    /// 表示 Blob 存储配置
    /// </summary>
    public class Blobstore
    {
        /// <summary>
        /// 获取或设置 Blob 存储 ID
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// 获取或设置 Blob 存储 type
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// 获取或设置 Blob 存储是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置文件 Blob 存储的基础目录
        /// </summary>
        [JsonProperty("baseDirectory")]
        public string BaseDirectory { get; set; }

        /// <summary>
        /// 获取或设置文件系统块大小
        /// </summary>
        [JsonProperty("fileSystemBlockSize")]
        public int? FileSystemBlockSize { get; set; }

        /// <summary>
        /// 获取或设置附加配置属性
        /// </summary>
        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; }
    }

    /// <summary>
    /// Blob 存储的包装器 list
    /// </summary>
    public class BlobstoreListWrapper
    {
        /// <summary>
        /// 获取或设置 Blob 存储列表
        /// </summary>
        [JsonProperty("blobstores")]
        public List<string> Blobstores { get; set; }
    }

    /// <summary>
    /// Blob 存储的包装器 response
    /// </summary>
    public class BlobstoreWrapper
    {
        /// <summary>
        /// 获取或设置 Blob 存储
        /// </summary>
        [JsonProperty("blobstore")]
        public Blobstore Blobstore { get; set; }
    }
}
