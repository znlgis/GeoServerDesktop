using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示 WMS 服务设置
    /// </summary>
    public class WMSSettings
    {
        /// <summary>
        /// 获取或设置 WMS 服务包装器
        /// </summary>
        [JsonProperty("wms")]
        public WMSServiceConfig WMS { get; set; }
    }

    /// <summary>
    /// WMS 服务配置
    /// </summary>
    public class WMSServiceConfig
    {
        /// <summary>
        /// 获取或设置服务是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置服务名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置服务标题
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 获取或设置服务摘要
        /// </summary>
        [JsonProperty("abstrct")]
        public string Abstract { get; set; }

        /// <summary>
        /// 获取或设置维护者
        /// </summary>
        [JsonProperty("maintainer")]
        public string Maintainer { get; set; }

        /// <summary>
        /// 获取或设置在线资源
        /// </summary>
        [JsonProperty("onlineResource")]
        public string OnlineResource { get; set; }

        /// <summary>
        /// 获取或设置是否符合引用规范
        /// </summary>
        [JsonProperty("citeCompliant")]
        public bool? CiteCompliant { get; set; }

        /// <summary>
        /// 获取或设置架构基础 URL
        /// </summary>
        [JsonProperty("schemaBaseURL")]
        public string SchemaBaseURL { get; set; }

        /// <summary>
        /// 获取或设置是否启用详细模式
        /// </summary>
        [JsonProperty("verbose")]
        public bool? Verbose { get; set; }

        /// <summary>
        /// 获取或设置元数据链接
        /// </summary>
        [JsonProperty("metadataLink")]
        public MetadataLink MetadataLink { get; set; }

        /// <summary>
        /// 获取或设置水印配置
        /// </summary>
        [JsonProperty("watermark")]
        public Watermark Watermark { get; set; }

        /// <summary>
        /// 获取或设置是否禁用动态样式
        /// </summary>
        [JsonProperty("dynamicStylingDisabled")]
        public bool? DynamicStylingDisabled { get; set; }

        /// <summary>
        /// 获取或设置最大请求内存
        /// </summary>
        [JsonProperty("maxRequestMemory")]
        public int? MaxRequestMemory { get; set; }

        /// <summary>
        /// 获取或设置最大渲染错误数
        /// </summary>
        [JsonProperty("maxRenderingErrors")]
        public int? MaxRenderingErrors { get; set; }

        /// <summary>
        /// 获取或设置空间参考系统列表
        /// </summary>
        [JsonProperty("srs")]
        public SRSList SRS { get; set; }
    }

    /// <summary>
    /// 表示 WFS 服务设置
    /// </summary>
    public class WFSSettings
    {
        /// <summary>
        /// 获取或设置 WFS 服务包装器
        /// </summary>
        [JsonProperty("wfs")]
        public WFSServiceConfig WFS { get; set; }
    }

    /// <summary>
    /// WFS 服务配置
    /// </summary>
    public class WFSServiceConfig
    {
        /// <summary>
        /// 获取或设置服务是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置服务名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置服务标题
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 获取或设置服务摘要
        /// </summary>
        [JsonProperty("abstrct")]
        public string Abstract { get; set; }

        /// <summary>
        /// 获取或设置维护者
        /// </summary>
        [JsonProperty("maintainer")]
        public string Maintainer { get; set; }

        /// <summary>
        /// 获取或设置在线资源
        /// </summary>
        [JsonProperty("onlineResource")]
        public string OnlineResource { get; set; }

        /// <summary>
        /// 获取或设置是否符合引用规范
        /// </summary>
        [JsonProperty("citeCompliant")]
        public bool? CiteCompliant { get; set; }

        /// <summary>
        /// 获取或设置架构基础 URL
        /// </summary>
        [JsonProperty("schemaBaseURL")]
        public string SchemaBaseURL { get; set; }

        /// <summary>
        /// 获取或设置是否启用详细模式
        /// </summary>
        [JsonProperty("verbose")]
        public bool? Verbose { get; set; }

        /// <summary>
        /// 获取或设置 GML2 输出格式
        /// </summary>
        [JsonProperty("gml2")]
        public GMLFormat GML2 { get; set; }

        /// <summary>
        /// 获取或设置 GML3 输出格式
        /// </summary>
        [JsonProperty("gml3")]
        public GMLFormat GML3 { get; set; }

        /// <summary>
        /// 获取或设置最大要素数
        /// </summary>
        [JsonProperty("maxFeatures")]
        public int? MaxFeatures { get; set; }

        /// <summary>
        /// 获取或设置服务级别
        /// </summary>
        [JsonProperty("serviceLevel")]
        public string ServiceLevel { get; set; }

        /// <summary>
        /// 获取或设置是否启用要素边界
        /// </summary>
        [JsonProperty("featureBounding")]
        public bool? FeatureBounding { get; set; }

        /// <summary>
        /// 获取或设置是否启用规范架构位置
        /// </summary>
        [JsonProperty("canonicalSchemaLocation")]
        public bool? CanonicalSchemaLocation { get; set; }

        /// <summary>
        /// 获取或设置是否编码要素成员
        /// </summary>
        [JsonProperty("encodeFeatureMember")]
        public bool? EncodeFeatureMember { get; set; }

        /// <summary>
        /// 获取或设置命中是否忽略最大要素数
        /// </summary>
        [JsonProperty("hitsIgnoreMaxFeatures")]
        public bool? HitsIgnoreMaxFeatures { get; set; }
    }

    /// <summary>
    /// 表示 WCS 服务设置
    /// </summary>
    public class WCSSettings
    {
        /// <summary>
        /// 获取或设置 WCS 服务包装器
        /// </summary>
        [JsonProperty("wcs")]
        public WCSServiceConfig WCS { get; set; }
    }

    /// <summary>
    /// WCS 服务配置
    /// </summary>
    public class WCSServiceConfig
    {
        /// <summary>
        /// 获取或设置服务是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置服务名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置服务标题
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 获取或设置服务摘要
        /// </summary>
        [JsonProperty("abstrct")]
        public string Abstract { get; set; }

        /// <summary>
        /// 获取或设置维护者
        /// </summary>
        [JsonProperty("maintainer")]
        public string Maintainer { get; set; }

        /// <summary>
        /// 获取或设置在线资源
        /// </summary>
        [JsonProperty("onlineResource")]
        public string OnlineResource { get; set; }

        /// <summary>
        /// 获取或设置是否符合引用规范
        /// </summary>
        [JsonProperty("citeCompliant")]
        public bool? CiteCompliant { get; set; }

        /// <summary>
        /// 获取或设置架构基础 URL
        /// </summary>
        [JsonProperty("schemaBaseURL")]
        public string SchemaBaseURL { get; set; }

        /// <summary>
        /// 获取或设置是否启用详细模式
        /// </summary>
        [JsonProperty("verbose")]
        public bool? Verbose { get; set; }

        /// <summary>
        /// 获取或设置最大输入内存（KB）
        /// </summary>
        [JsonProperty("maxInputMemory")]
        public int? MaxInputMemory { get; set; }

        /// <summary>
        /// 获取或设置最大输出内存（KB）
        /// </summary>
        [JsonProperty("maxOutputMemory")]
        public int? MaxOutputMemory { get; set; }

        /// <summary>
        /// 获取或设置是否启用子采样
        /// </summary>
        [JsonProperty("subsamplingEnabled")]
        public bool? SubsamplingEnabled { get; set; }

        /// <summary>
        /// 获取或设置概览策略
        /// </summary>
        [JsonProperty("overviewPolicy")]
        public string OverviewPolicy { get; set; }

        /// <summary>
        /// 获取或设置资源消耗限制
        /// </summary>
        [JsonProperty("resourceConsumptionLimits")]
        public ResourceConsumptionLimits ResourceConsumptionLimits { get; set; }
    }

    /// <summary>
    /// 表示 WMTS 服务设置
    /// </summary>
    public class WMTSSettings
    {
        /// <summary>
        /// 获取或设置 WMTS 服务包装器
        /// </summary>
        [JsonProperty("wmts")]
        public WMTSServiceConfig WMTS { get; set; }
    }

    /// <summary>
    /// WMTS 服务配置
    /// </summary>
    public class WMTSServiceConfig
    {
        /// <summary>
        /// 获取或设置服务是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置服务名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置服务标题
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 获取或设置服务摘要
        /// </summary>
        [JsonProperty("abstrct")]
        public string Abstract { get; set; }

        /// <summary>
        /// 获取或设置维护者
        /// </summary>
        [JsonProperty("maintainer")]
        public string Maintainer { get; set; }

        /// <summary>
        /// 获取或设置在线资源
        /// </summary>
        [JsonProperty("onlineResource")]
        public string OnlineResource { get; set; }

        /// <summary>
        /// 获取或设置是否符合引用规范
        /// </summary>
        [JsonProperty("citeCompliant")]
        public bool? CiteCompliant { get; set; }

        /// <summary>
        /// 获取或设置是否启用详细模式
        /// </summary>
        [JsonProperty("verbose")]
        public bool? Verbose { get; set; }
    }

    /// <summary>
    /// 表示元数据链接信息
    /// </summary>
    public class MetadataLink
    {
        /// <summary>
        /// 获取或设置元数据类型
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// 获取或设置元数据 URL
        /// </summary>
        [JsonProperty("metadataType")]
        public string MetadataType { get; set; }

        /// <summary>
        /// 获取或设置内容
        /// </summary>
        [JsonProperty("content")]
        public string Content { get; set; }
    }

    /// <summary>
    /// 表示水印配置
    /// </summary>
    public class Watermark
    {
        /// <summary>
        /// 获取或设置是否启用水印
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置水印位置
        /// </summary>
        [JsonProperty("position")]
        public string Position { get; set; }

        /// <summary>
        /// 获取或设置透明度（0-100）
        /// </summary>
        [JsonProperty("transparency")]
        public int? Transparency { get; set; }

        /// <summary>
        /// 获取或设置水印 URL
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    /// <summary>
    /// 表示空间参考系统列表
    /// </summary>
    public class SRSList
    {
        /// <summary>
        /// 获取或设置空间参考系统代码列表
        /// </summary>
        [JsonProperty("string")]
        public string[] Strings { get; set; }
    }

    /// <summary>
    /// 表示 GML 格式配置
    /// </summary>
    public class GMLFormat
    {
        /// <summary>
        /// 获取或设置是否覆盖 GML 属性
        /// </summary>
        [JsonProperty("overrideGMLAttributes")]
        public bool? OverrideGMLAttributes { get; set; }

        /// <summary>
        /// 获取或设置空间参考系统名称样式
        /// </summary>
        [JsonProperty("srsNameStyle")]
        public string SrsNameStyle { get; set; }
    }

    /// <summary>
    /// 表示资源消耗限制
    /// </summary>
    public class ResourceConsumptionLimits
    {
        /// <summary>
        /// 获取或设置最大维度值
        /// </summary>
        [JsonProperty("maxDimensions")]
        public int? MaxDimensions { get; set; }

        /// <summary>
        /// 获取或设置最大请求大小（像素）
        /// </summary>
        [JsonProperty("maxRequestSizePixels")]
        public long? MaxRequestSizePixels { get; set; }
    }

    /// <summary>
    /// 表示 WPS 服务设置
    /// </summary>
    public class WPSSettings
    {
        /// <summary>
        /// 获取或设置 WPS 服务包装器
        /// </summary>
        [JsonProperty("wps")]
        public WPSServiceConfig WPS { get; set; }
    }

    /// <summary>
    /// WPS 服务配置
    /// </summary>
    public class WPSServiceConfig
    {
        /// <summary>
        /// 获取或设置服务是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置服务名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置服务标题
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 获取或设置服务摘要
        /// </summary>
        [JsonProperty("abstrct")]
        public string Abstract { get; set; }

        /// <summary>
        /// 获取或设置维护者
        /// </summary>
        [JsonProperty("maintainer")]
        public string Maintainer { get; set; }

        /// <summary>
        /// 获取或设置在线资源
        /// </summary>
        [JsonProperty("onlineResource")]
        public string OnlineResource { get; set; }

        /// <summary>
        /// 获取或设置是否符合引用规范
        /// </summary>
        [JsonProperty("citeCompliant")]
        public bool? CiteCompliant { get; set; }

        /// <summary>
        /// 获取或设置架构基础 URL
        /// </summary>
        [JsonProperty("schemaBaseURL")]
        public string SchemaBaseURL { get; set; }

        /// <summary>
        /// 获取或设置连接超时（秒）
        /// </summary>
        [JsonProperty("connectionTimeout")]
        public int? ConnectionTimeout { get; set; }

        /// <summary>
        /// 获取或设置资源过期超时
        /// </summary>
        [JsonProperty("resourceExpirationTimeout")]
        public int? ResourceExpirationTimeout { get; set; }

        /// <summary>
        /// 获取或设置最大同步进程数
        /// </summary>
        [JsonProperty("maxSynchronousProcesses")]
        public int? MaxSynchronousProcesses { get; set; }

        /// <summary>
        /// 获取或设置最大异步进程数
        /// </summary>
        [JsonProperty("maxAsynchronousProcesses")]
        public int? MaxAsynchronousProcesses { get; set; }
    }

    /// <summary>
    /// 表示 CSW 服务设置
    /// </summary>
    public class CSWSettings
    {
        /// <summary>
        /// 获取或设置 CSW 服务包装器
        /// </summary>
        [JsonProperty("csw")]
        public CSWServiceConfig CSW { get; set; }
    }

    /// <summary>
    /// CSW 服务配置
    /// </summary>
    public class CSWServiceConfig
    {
        /// <summary>
        /// 获取或设置服务是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置服务名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置服务标题
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 获取或设置服务摘要
        /// </summary>
        [JsonProperty("abstrct")]
        public string Abstract { get; set; }

        /// <summary>
        /// 获取或设置维护者
        /// </summary>
        [JsonProperty("maintainer")]
        public string Maintainer { get; set; }

        /// <summary>
        /// 获取或设置在线资源
        /// </summary>
        [JsonProperty("onlineResource")]
        public string OnlineResource { get; set; }

        /// <summary>
        /// 获取或设置是否符合引用规范
        /// </summary>
        [JsonProperty("citeCompliant")]
        public bool? CiteCompliant { get; set; }

        /// <summary>
        /// 获取或设置架构基础 URL
        /// </summary>
        [JsonProperty("schemaBaseURL")]
        public string SchemaBaseURL { get; set; }
    }
}
