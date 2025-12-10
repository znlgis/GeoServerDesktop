using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示 GeoServer 日志配置
    /// </summary>
    public class LoggingSettings
    {
        /// <summary>
        /// 获取或设置日志配置数据
        /// </summary>
        [JsonProperty("logging")]
        public LoggingConfig Logging { get; set; }
    }

    /// <summary>
    /// 表示日志配置详细信息
    /// </summary>
    public class LoggingConfig
    {
        /// <summary>
        /// 获取或设置日志级别
        /// </summary>
        [JsonProperty("level")]
        public string Level { get; set; }

        /// <summary>
        /// 获取或设置日志位置（文件路径）
        /// </summary>
        [JsonProperty("location")]
        public string Location { get; set; }

        /// <summary>
        /// 获取或设置是否记录到标准输出
        /// </summary>
        [JsonProperty("stdOutLogging")]
        public bool? StdOutLogging { get; set; }

        /// <summary>
        /// 获取或设置是否启用文件日志
        /// </summary>
        [JsonProperty("fileLogging")]
        public bool? FileLogging { get; set; }
    }
}
