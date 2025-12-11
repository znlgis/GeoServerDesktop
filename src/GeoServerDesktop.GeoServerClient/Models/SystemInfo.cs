using Newtonsoft.Json;
using System.Linq;

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
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置版本
        /// </summary>
        [JsonProperty("Version")]
        public string Version { get; set; }

        /// <summary>
        /// 获取或设置构建时间戳
        /// </summary>
        [JsonProperty("Build-Timestamp")]
        public string BuildTimestamp { get; set; }

        /// <summary>
        /// 获取或设置 Git 修订版本
        /// </summary>
        [JsonProperty("Git-Revision")]
        public string GitRevision { get; set; }
    }

    /// <summary>
    /// 系统状态响应的包装器
    /// </summary>
    public class SystemStatusWrapper
    {
        /// <summary>
        /// 获取或设置指标容器
        /// </summary>
        [JsonProperty("metrics")]
        public MetricsContainer Metrics { get; set; }
    }

    /// <summary>
    /// 表示指标容器
    /// </summary>
    public class MetricsContainer
    {
        /// <summary>
        /// 获取或设置指标数组
        /// </summary>
        [JsonProperty("metric")]
        public Metric[] MetricArray { get; set; }
    }

    /// <summary>
    /// 表示单个指标
    /// </summary>
    public class Metric
    {
        /// <summary>
        /// 获取或设置指标是否可用
        /// </summary>
        [JsonProperty("available")]
        public bool Available { get; set; }

        /// <summary>
        /// 获取或设置指标描述
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// 获取或设置指标名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置单位
        /// </summary>
        [JsonProperty("unit")]
        public string Unit { get; set; }

        /// <summary>
        /// 获取或设置类别
        /// </summary>
        [JsonProperty("category")]
        public string Category { get; set; }

        /// <summary>
        /// 获取或设置标识符
        /// </summary>
        [JsonProperty("identifier")]
        public string Identifier { get; set; }

        /// <summary>
        /// 获取或设置优先级
        /// </summary>
        [JsonProperty("priority")]
        public int Priority { get; set; }

        /// <summary>
        /// 获取或设置值
        /// </summary>
        [JsonProperty("value")]
        public object Value { get; set; }
    }

    /// <summary>
    /// 表示系统指标（已弃用，保留以向后兼容）
    /// </summary>
    [System.Obsolete("Use MetricsContainer and Metric classes instead")]
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

    /// <summary>
    /// SystemStatusWrapper 扩展方法
    /// </summary>
    public static class SystemStatusExtensions
    {
        /// <summary>
        /// 从指标数组中获取指定名称的指标值
        /// </summary>
        public static string GetMetricValue(this SystemStatusWrapper wrapper, string metricName)
        {
            var metric = wrapper?.Metrics?.MetricArray?.FirstOrDefault(m => m.Name == metricName);
            return metric?.Value?.ToString();
        }

        /// <summary>
        /// 从指标数组中获取 long 类型的指标值
        /// </summary>
        public static long GetMetricValueAsLong(this SystemStatusWrapper wrapper, string metricName)
        {
            var metric = wrapper?.Metrics?.MetricArray?.FirstOrDefault(m => m.Name == metricName);
            if (metric?.Value == null) return 0;
            
            if (metric.Value is long longValue) return longValue;
            var valueString = metric.Value?.ToString();
            if (valueString != null && long.TryParse(valueString, out var result)) return result;
            return 0;
        }

        /// <summary>
        /// 获取 JVM 版本
        /// </summary>
        public static string GetJvmVersion(this SystemStatusWrapper wrapper)
        {
            return wrapper.GetMetricValue("JVM_VERSION");
        }

        /// <summary>
        /// 获取 JVM 供应商
        /// </summary>
        public static string GetJvmVendor(this SystemStatusWrapper wrapper)
        {
            return wrapper.GetMetricValue("JVM_VENDOR");
        }

        /// <summary>
        /// 获取最大堆内存
        /// </summary>
        public static long GetMaximumHeapMemory(this SystemStatusWrapper wrapper)
        {
            return wrapper.GetMetricValueAsLong("MAXIMUM_HEAP_MEMORY");
        }

        /// <summary>
        /// 获取总堆内存
        /// </summary>
        public static long GetTotalHeapMemory(this SystemStatusWrapper wrapper)
        {
            return wrapper.GetMetricValueAsLong("TOTAL_HEAP_MEMORY");
        }

        /// <summary>
        /// 获取空闲堆内存
        /// </summary>
        public static long GetFreeHeapMemory(this SystemStatusWrapper wrapper)
        {
            return wrapper.GetMetricValueAsLong("FREE_HEAP_MEMORY");
        }

        /// <summary>
        /// 获取已使用堆内存
        /// </summary>
        public static long GetUsedHeapMemory(this SystemStatusWrapper wrapper)
        {
            return wrapper.GetMetricValueAsLong("USED_HEAP_MEMORY");
        }
    }
}
