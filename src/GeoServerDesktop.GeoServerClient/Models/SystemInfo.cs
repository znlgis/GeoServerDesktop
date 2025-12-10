using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示 GeoServer 版本信息
    /// </summary>
    public class VersionInfo
    {
        /// <summary>
        /// 获取或设置 GeoServer 版本
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// 获取或设置 GeoTools 版本
        /// </summary>
        [JsonProperty("geotools")]
        public string GeoTools { get; set; }

        /// <summary>
        /// 获取或设置 GeoWebCache 版本
        /// </summary>
        [JsonProperty("geowebcache")]
        public string GeoWebCache { get; set; }
    }

    /// <summary>
    /// 版本信息响应的包装器
    /// </summary>
    public class VersionInfoWrapper
    {
        /// <summary>
        /// 获取或设置关于信息
        /// </summary>
        [JsonProperty("about")]
        public AboutInfo About { get; set; }
    }

    /// <summary>
    /// 表示关于信息
    /// </summary>
    public class AboutInfo
    {
        /// <summary>
        /// 获取或设置资源信息
        /// </summary>
        [JsonProperty("resource")]
        public ResourceInfo[] Resources { get; set; }
    }

    /// <summary>
    /// 表示资源信息
    /// </summary>
    public class ResourceInfo
    {
        /// <summary>
        /// 获取或设置资源名称
        /// </summary>
        [JsonProperty("@name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置资源值
        /// </summary>
        [JsonProperty("$")]
        public string Value { get; set; }
    }

    /// <summary>
    /// 表示系统状态信息
    /// </summary>
    public class SystemStatus
    {
        /// <summary>
        /// 获取或设置内存信息
        /// </summary>
        [JsonProperty("metrics")]
        public Metrics Metrics { get; set; }
    }

    /// <summary>
    /// 系统状态响应的包装器
    /// </summary>
    public class SystemStatusWrapper
    {
        /// <summary>
        /// 获取或设置系统状态
        /// </summary>
        [JsonProperty("systemStatus")]
        public SystemStatus SystemStatus { get; set; }
    }

    /// <summary>
    /// 表示系统指标
    /// </summary>
    public class Metrics
    {
        /// <summary>
        /// 获取或设置内存使用情况
        /// </summary>
        [JsonProperty("memory")]
        public MemoryInfo Memory { get; set; }

        /// <summary>
        /// 获取或设置 JVM 信息
        /// </summary>
        [JsonProperty("jvm")]
        public JvmInfo Jvm { get; set; }
    }

    /// <summary>
    /// 表示内存信息
    /// </summary>
    public class MemoryInfo
    {
        /// <summary>
        /// 获取或设置总内存
        /// </summary>
        [JsonProperty("total")]
        public long Total { get; set; }

        /// <summary>
        /// 获取或设置空闲内存
        /// </summary>
        [JsonProperty("free")]
        public long Free { get; set; }

        /// <summary>
        /// 获取或设置已使用内存
        /// </summary>
        [JsonProperty("used")]
        public long Used { get; set; }

        /// <summary>
        /// 获取或设置最大内存
        /// </summary>
        [JsonProperty("max")]
        public long Max { get; set; }
    }

    /// <summary>
    /// 表示 JVM 信息
    /// </summary>
    public class JvmInfo
    {
        /// <summary>
        /// 获取或设置 JVM 版本
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// 获取或设置 JVM 供应商
        /// </summary>
        [JsonProperty("vendor")]
        public string Vendor { get; set; }
    }

    /// <summary>
    /// 表示 GeoServer 清单信息
    /// </summary>
    public class ManifestInfo
    {
        /// <summary>
        /// 获取或设置清单名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置清单版本
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }
    }

    /// <summary>
    /// 清单响应的包装器
    /// </summary>
    public class ManifestsWrapper
    {
        /// <summary>
        /// 获取或设置清单信息
        /// </summary>
        [JsonProperty("about")]
        public ManifestsAbout About { get; set; }
    }

    /// <summary>
    /// 表示清单关于信息
    /// </summary>
    public class ManifestsAbout
    {
        /// <summary>
        /// 获取或设置资源清单
        /// </summary>
        [JsonProperty("resource")]
        public ManifestInfo[] Resources { get; set; }
    }
}
