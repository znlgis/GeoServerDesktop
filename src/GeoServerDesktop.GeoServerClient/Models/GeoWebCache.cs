using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Wrapper for GeoWebCache layers list.
    /// FIXED-E8：GWC 2.0.1 实测 <c>/gwc/rest/layers.json</c> 顶层直接返回 JSON 字符串数组
    /// （<c>["ws:layer",...]</c>，无外层 layers 键）。使用自定义转换器将裸数组读入本包装器的 Layers 属性。
    /// </summary>
    [JsonConverter(typeof(RawStringArrayConverter))]
    public class GWCLayerListWrapper
    {
        /// <summary>
        /// 获取或设置缓存图层列表
        /// </summary>
        [JsonProperty("layers")]
        public List<string> Layers { get; set; }
    }

    /// <summary>
    /// 网格集名称列表包装器（GWC 顶层数组，无外层键）。FIXED-E8。
    /// </summary>
    [JsonConverter(typeof(RawStringArrayConverter))]
    public class GridsetListWrapper
    {
        /// <summary>
        /// 获取或设置网格集列表
        /// </summary>
        [JsonProperty("gridSets")]
        public List<string> GridSets { get; set; }
    }

    /// <summary>
    /// Blob 存储名称列表包装器（GWC 顶层数组，无外层键）。FIXED-E8。
    /// </summary>
    [JsonConverter(typeof(RawStringArrayConverter))]
    public class BlobstoreListWrapper
    {
        /// <summary>
        /// 获取或设置 Blob 存储列表
        /// </summary>
        [JsonProperty("blobstores")]
        public List<string> Blobstores { get; set; }
    }

    /// <summary>
    /// 用于把 GeoServer / GWC 顶层裸数组（如 ["a","b"]）读入 { List&lt;string&gt; Names } 包装。
    /// </summary>
    internal sealed class RawStringArrayConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(GWCLayerListWrapper)
            || objectType == typeof(GridsetListWrapper)
            || objectType == typeof(BlobstoreListWrapper);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var token = JToken.Load(reader);
            var names = token.Type == JTokenType.Array
                ? token.ToObject<List<string>>()
                : new List<string>();
            if (objectType == typeof(GWCLayerListWrapper))
                return new GWCLayerListWrapper { Layers = names };
            if (objectType == typeof(GridsetListWrapper))
                return new GridsetListWrapper { GridSets = names };
            if (objectType == typeof(BlobstoreListWrapper))
                return new BlobstoreListWrapper { Blobstores = names };
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // 序列化时输出为顶层数组（当前服务未使用写侧，直接抛以提醒）
            throw new NotSupportedException("RawStringArrayConverter 只支持反序列化");
        }
    }

    /// <summary>
    /// 表示 GeoWebCache 图层。
    /// FIXED-E9：GWC 单体 GET <c>/gwc/rest/layers/{name}.json</c> 实际返回
    /// <c>{"GeoServerLayer":{...}}</c>（以 Java 类名为动态根键），需 <see cref="ParseSingle"/> 手工解析。
    /// </summary>
    public class GWCLayer
    {
        /// <summary>
        /// 获取或设置图层名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置图层 id（GWC 返回的内部实现 id）
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

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
        /// 获取或设置过期客户端；3.0.1 实测为标量 int（旧基线 int? 兼容）。
        /// </summary>
        [JsonProperty("expireClients")]
        public int? ExpireClients { get; set; }

        /// <summary>
        /// 从 GWC 3.0.1 动态类名根解析单体图层。
        /// </summary>
        public static GWCLayer ParseSingle(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return null;
            var obj = JObject.Parse(rawJson);
            foreach (var prop in obj.Properties())
            {
                var inner = prop.Value as JObject;
                if (inner == null) continue;
                return inner.ToObject<GWCLayer>();
            }
            return null;
        }
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
    /// 表示种子请求（GWC 只支持 XML；此处保留 JSON 模型，
    /// 服务侧改用 XML 字符串生成器 <see cref="ToXmlBody"/>）。
    /// </summary>
    public class SeedRequest
    {
        /// <summary>
        /// 获取或设置种子请求包装器
        /// </summary>
        [JsonProperty("seedRequest")]
        public SeedRequestConfig Config { get; set; }

        /// <summary>
        /// 生成 GWC 接受的 <c>&lt;seedRequest&gt;</c> XML 请求体。
        /// 必填项：name / gridSetId / zoomStart / zoomStop / format / type / threadCount。
        /// </summary>
        public string ToXmlBody()
        {
            var c = Config ?? new SeedRequestConfig();
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
                 + "<seedRequest>\n"
                 + "  <name>" + XmlEscape(c.Name) + "</name>\n"
                 + "  <gridSetId>" + XmlEscape(c.GridSetId) + "</gridSetId>\n"
                 + "  <zoomStart>" + c.ZoomStart + "</zoomStart>\n"
                 + "  <zoomStop>" + c.ZoomStop + "</zoomStop>\n"
                 + "  <format>" + XmlEscape(c.Format ?? "image/png") + "</format>\n"
                 + "  <type>" + XmlEscape(c.Type ?? "seed") + "</type>\n"
                 + "  <threadCount>" + (c.ThreadCount ?? 1) + "</threadCount>\n"
                 + "</seedRequest>\n";
        }

        private static string XmlEscape(string s) =>
            string.IsNullOrEmpty(s) ? "" : s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
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
    /// 表示磁盘配额配置。
    /// FIXED-E13：GWC 2.0.1 diskquota 端点无论 Accept 与否都返回 XML（<c>org.geowebcache.diskquota.DiskQuotaConfig</c> 根），
    /// 服务侧改用 XDocument 解析映射到本模型；PUT 回写同样 XML。
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
        /// 获取或设置最大缓存并发清理数
        /// </summary>
        [JsonProperty("maxConcurrentCleanUps")]
        public int? MaxConcurrentCleanUps { get; set; }

        /// <summary>
        /// 全局过期策略名（GWC 3.x 响应含此字段）
        /// </summary>
        [JsonProperty("globalExpirationPolicyName")]
        public string GlobalExpirationPolicyName { get; set; }

        /// <summary>
        /// 配额存储类型（HSQL/JDBC 等）
        /// </summary>
        [JsonProperty("quotaStore")]
        public string QuotaStore { get; set; }

        /// <summary>
        /// 获取或设置全局配额
        /// </summary>
        [JsonProperty("globalQuota")]
        public Quota GlobalQuota { get; set; }

        /// <summary>
        /// 获取或设置图层配额（GWC 3.x XML 里为 &lt;queue/&gt;/&lt;layerQuotas/&gt; 结构，本客户端只保留全局项）
        /// </summary>
        [JsonProperty("layerQuotas")]
        public List<LayerQuota> LayerQuotas { get; set; }

        /// <summary>
        /// FIXED-E13：解析 GWC diskquota 端点的 XML 响应
        /// （根 <c>org.geowebcache.diskquota.DiskQuotaConfig</c>，实测恒为 XML，与 Accept 无关）。
        /// </summary>
        public static DiskQuotaConfig ParseXml(string rawXml)
        {
            if (string.IsNullOrWhiteSpace(rawXml)) return null;
            var doc = System.Xml.Linq.XDocument.Parse(rawXml);
            var root = doc.Root;
            if (root == null) return null;
            System.Func<string, string> el = name => root.Element(name)?.Value;
            var cfg = new DiskQuotaConfig
            {
                Enabled = bool.TryParse(el("enabled"), out var en) ? (bool?)en : null,
                DiskBlockSize = int.TryParse(el("diskBlockSize"), out var dbs) ? (int?)dbs : null,
                CacheCleanUpFrequency = int.TryParse(el("cacheCleanUpFrequency"), out var cc) ? (int?)cc : null,
                CacheCleanUpUnits = el("cacheCleanUpUnits"),
                MaxConcurrentCleanUps = int.TryParse(el("maxConcurrentCleanUps"), out var mc) ? (int?)mc : null,
                GlobalExpirationPolicyName = el("globalExpirationPolicyName"),
                QuotaStore = el("quotaStore"),
            };
            var gq = root.Element("globalQuota");
            if (gq != null)
            {
                cfg.GlobalQuota = new Quota
                {
                    Id = gq.Element("id")?.Value,
                    Bytes = long.TryParse(gq.Element("bytes")?.Value, out var b) ? (long?)b : null,
                    Value = double.TryParse(gq.Element("value")?.Value, out var v) ? v : 0d,
                    Units = gq.Element("units")?.Value,
                };
            }
            return cfg;
        }

        /// <summary>
        /// 生成 PUT 回写的 GWC diskquota XML（与 <see cref="ParseXml"/> 往返等价的字段集合）。
        /// </summary>
        public string ToXmlBody()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
            sb.Append("<org.geowebcache.diskquota.DiskQuotaConfig>");
            if (Enabled.HasValue) sb.Append("<enabled>").Append(Enabled.Value ? "true" : "false").Append("</enabled>");
            if (DiskBlockSize.HasValue) sb.Append("<diskBlockSize>").Append(DiskBlockSize.Value).Append("</diskBlockSize>");
            if (CacheCleanUpFrequency.HasValue) sb.Append("<cacheCleanUpFrequency>").Append(CacheCleanUpFrequency.Value).Append("</cacheCleanUpFrequency>");
            if (!string.IsNullOrEmpty(CacheCleanUpUnits)) sb.Append("<cacheCleanUpUnits>").Append(CacheCleanUpUnits).Append("</cacheCleanUpUnits>");
            if (MaxConcurrentCleanUps.HasValue) sb.Append("<maxConcurrentCleanUps>").Append(MaxConcurrentCleanUps.Value).Append("</maxConcurrentCleanUps>");
            if (!string.IsNullOrEmpty(GlobalExpirationPolicyName)) sb.Append("<globalExpirationPolicyName>").Append(GlobalExpirationPolicyName).Append("</globalExpirationPolicyName>");
            if (GlobalQuota != null)
            {
                sb.Append("<globalQuota>");
                if (GlobalQuota.Bytes.HasValue) sb.Append("<bytes>").Append(GlobalQuota.Bytes.Value).Append("</bytes>");
                if (GlobalQuota.Value > 0) sb.Append("<value>").Append(GlobalQuota.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append("</value>");
                if (!string.IsNullOrEmpty(GlobalQuota.Units)) sb.Append("<units>").Append(GlobalQuota.Units).Append("</units>");
                sb.Append("</globalQuota>");
            }
            if (!string.IsNullOrEmpty(QuotaStore)) sb.Append("<quotaStore>").Append(QuotaStore).Append("</quotaStore>");
            sb.Append("</org.geowebcache.diskquota.DiskQuotaConfig>");
            return sb.ToString();
        }
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

        /// <summary>
        /// GWC 3.x XML 里为 &lt;bytes&gt; 直接给字节数；服务解析时填入本字段并置 Units="B"。
        /// </summary>
        [JsonProperty("bytes")]
        public long? Bytes { get; set; }

        /// <summary>
        /// 可选：GWC XML 里的 id
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
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
    /// 表示网格集定义。
    /// FIXED-E9：单体 GET 响应根为 <c>{"gridSet":{...}}</c>（大写 S），需 <see cref="ParseSingle"/> 手工解析。
    /// </summary>
    public class Gridset
    {
        /// <summary>
        /// 获取或设置网格集名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 网格集描述
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

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

        /// <summary>
        /// 解析 GWC 3.0.1 单体响应 <c>{"gridSet":{...}}</c>。
        /// </summary>
        public static Gridset ParseSingle(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return null;
            var obj = JObject.Parse(rawJson);
            var inner = obj["gridSet"] as JObject ?? obj["gridset"] as JObject;
            return inner?.ToObject<Gridset>();
        }

        /// <summary>
        /// 生成 GWC 可接受的 <c>&lt;gridSet&gt;</c> XStream XML（FIXED-E10：JSON PUT 在实测报
        /// "Duplicate field coords"，XML 形态实测 201 创建成功）。必填 name/srs/extent/resolutions/tileWidth/tileHeight。
        /// </summary>
        public string ToXmlBody()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<gridSet>");
            sb.Append("<name>").Append(XmlEscape(Name)).Append("</name>");
            if (!string.IsNullOrEmpty(Description)) sb.Append("<description>").Append(XmlEscape(Description)).Append("</description>");
            if (SRS != null) sb.Append("<srs><number>").Append(SRS.Number).Append("</number></srs>");
            if (Extent != null && Extent.Coords != null && Extent.Coords.Length > 0)
            {
                sb.Append("<extent>");
                foreach (var c in Extent.Coords) sb.Append("<double>").Append(c.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append("</double>");
                sb.Append("</extent>");
            }
            if (AlignTopLeft.HasValue) sb.Append("<alignTopLeft>").Append(AlignTopLeft.Value ? "true" : "false").Append("</alignTopLeft>");
            if (Resolutions != null && Resolutions.Count > 0)
            {
                sb.Append("<resolutions>");
                foreach (var r in Resolutions) sb.Append("<double>").Append(r.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append("</double>");
                sb.Append("</resolutions>");
            }
            if (MetersPerUnit.HasValue) sb.Append("<metersPerUnit>").Append(MetersPerUnit.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append("</metersPerUnit>");
            if (PixelSize.HasValue) sb.Append("<pixelSize>").Append(PixelSize.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append("</pixelSize>");
            if (ScaleNames != null && ScaleNames.Count > 0)
            {
                sb.Append("<scaleNames>");
                foreach (var s in ScaleNames) sb.Append("<string>").Append(XmlEscape(s)).Append("</string>");
                sb.Append("</scaleNames>");
            }
            if (TileWidth.HasValue) sb.Append("<tileWidth>").Append(TileWidth.Value).Append("</tileWidth>");
            if (TileHeight.HasValue) sb.Append("<tileHeight>").Append(TileHeight.Value).Append("</tileHeight>");
            if (YCoordinateFirst.HasValue) sb.Append("<yCoordinateFirst>").Append(YCoordinateFirst.Value ? "true" : "false").Append("</yCoordinateFirst>");
            sb.Append("</gridSet>");
            return sb.ToString();
        }

        private static string XmlEscape(string s) =>
            string.IsNullOrEmpty(s) ? "" : s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
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
    /// 表示 Blob 存储配置。
    /// FIXED-E11：GWC 单体 blobstore 响应根为具体实现类名（<c>{"FileBlobStore":{...}}</c>），
    /// 需 <see cref="ParseSingle"/> 手工解析。
    /// </summary>
    public class Blobstore
    {
        /// <summary>
        /// 获取或设置 Blob 存储 id（GWC 响应里为 name 的别名，读取时由 name 填充）
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// 获取或设置 Blob 存储 type（实现类名，如 FileBlobStore）
        /// </summary>
        [JsonIgnore]
        public string Type { get; set; }

        /// <summary>
        /// 获取或设置 Blob 存储是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 是否为默认 blobstore（GWC 3.x 响应里为 @default 属性）
        /// </summary>
        [JsonProperty("@default")]
        public bool? IsDefault { get; set; }

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

        /// <summary>
        /// 解析 GWC 3.0.1 单体响应 <c>{"&lt;实现类&gt;":{...}}</c>。
        /// </summary>
        public static Blobstore ParseSingle(string rawJson, string fallbackTypeName = null)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return null;
            var obj = JObject.Parse(rawJson);
            foreach (var prop in obj.Properties())
            {
                var inner = prop.Value as JObject;
                if (inner == null) continue;
                var b = inner.ToObject<Blobstore>();
                b.Type = prop.Name;
                if (b.Id == null) b.Id = (string)inner["id"] ?? (string)inner["name"] ?? fallbackTypeName;
                return b;
            }
            return null;
        }

        /// <summary>
        /// 生成用于 PUT 的 XML 请求体（GWC 端 JSON PUT 会因 XStream 类型信息缺失而 500，实测仅 XML 可用）。
        /// </summary>
        public string ToXmlBody(string blobStoreName)
        {
            var name = blobStoreName ?? Id;
            var cls = Type ?? "FileBlobStore";
            var sb = new System.Text.StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
            sb.Append('<').Append(cls).Append('>');
            sb.Append("<name>").Append(XmlEscape(name)).Append("</name>");
            if (Enabled.HasValue) sb.Append("<enabled>").Append(Enabled.Value ? "true" : "false").Append("</enabled>");
            if (!string.IsNullOrEmpty(BaseDirectory)) sb.Append("<baseDirectory>").Append(XmlEscape(BaseDirectory)).Append("</baseDirectory>");
            if (FileSystemBlockSize.HasValue) sb.Append("<fileSystemBlockSize>").Append(FileSystemBlockSize.Value).Append("</fileSystemBlockSize>");
            sb.Append("</").Append(cls).Append('>');
            return sb.ToString();
        }

        private static string XmlEscape(string s) =>
            string.IsNullOrEmpty(s) ? "" : s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    /// <summary>
    /// 兼容旧式 <c>{"blobstore":{...}}</c> 包装（保留供既有测试引用；
    /// GWC 3.0.1 实际响应以类名为根，请使用 <see cref="Blobstore.ParseSingle(string, string)"/>）。
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
