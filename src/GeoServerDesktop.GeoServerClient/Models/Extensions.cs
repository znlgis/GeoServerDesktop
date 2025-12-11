using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示导入上下文
    /// </summary>
    public class ImportContext
    {
        /// <summary>
        /// 获取或设置导入 ID
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// 获取或设置导入状态（PENDING、READY、RUNNING、COMPLETE 等）
        /// </summary>
        [JsonProperty("state")]
        public string State { get; set; }

        /// <summary>
        /// 获取或设置目标工作空间
        /// </summary>
        [JsonProperty("targetWorkspace")]
        public WorkspaceReference TargetWorkspace { get; set; }

        /// <summary>
        /// 获取或设置目标存储
        /// </summary>
        [JsonProperty("targetStore")]
        public StoreReference TargetStore { get; set; }

        /// <summary>
        /// 获取或设置任务列表
        /// </summary>
        [JsonProperty("tasks")]
        public List<ImportTask> Tasks { get; set; }
    }

    /// <summary>
    /// 表示导入任务
    /// </summary>
    public class ImportTask
    {
        /// <summary>
        /// 获取或设置任务 ID
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// 获取或设置任务状态
        /// </summary>
        [JsonProperty("state")]
        public string State { get; set; }

        /// <summary>
        /// 获取或设置源数据
        /// </summary>
        [JsonProperty("data")]
        public ImportData Data { get; set; }

        /// <summary>
        /// 获取或设置目标图层
        /// </summary>
        [JsonProperty("target")]
        public LayerReference Target { get; set; }

        /// <summary>
        /// 获取或设置进度信息
        /// </summary>
        [JsonProperty("progress")]
        public string Progress { get; set; }
    }

    /// <summary>
    /// 表示导入数据
    /// </summary>
    public class ImportData
    {
        /// <summary>
        /// 获取或设置数据类型（文件、目录、数据库等）
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// 获取或设置格式
        /// </summary>
        [JsonProperty("format")]
        public string Format { get; set; }

        /// <summary>
        /// 获取或设置位置
        /// </summary>
        [JsonProperty("location")]
        public string Location { get; set; }
    }

    /// <summary>
    /// 表示图层引用
    /// </summary>
    public class LayerReference
    {
        /// <summary>
        /// 获取或设置图层名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    /// <summary>
    /// 导入上下文的包装器
    /// </summary>
    public class ImportContextWrapper
    {
        /// <summary>
        /// 获取或设置导入上下文数据
        /// </summary>
        [JsonProperty("import")]
        public ImportContext Import { get; set; }
    }

    /// <summary>
    /// 表示监控请求信息
    /// </summary>
    public class MonitorRequest
    {
        /// <summary>
        /// 获取或设置请求 ID
        /// </summary>
        [JsonProperty("id")]
        public long Id { get; set; }

        /// <summary>
        /// 获取或设置路径
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary>
        /// 获取或设置查询字符串
        /// </summary>
        [JsonProperty("queryString")]
        public string QueryString { get; set; }

        /// <summary>
        /// 获取或设置 HTTP 方法
        /// </summary>
        [JsonProperty("httpMethod")]
        public string HttpMethod { get; set; }

        /// <summary>
        /// 获取或设置开始时间
        /// </summary>
        [JsonProperty("startTime")]
        public string StartTime { get; set; }

        /// <summary>
        /// 获取或设置结束时间
        /// </summary>
        [JsonProperty("endTime")]
        public string EndTime { get; set; }

        /// <summary>
        /// 获取或设置总时间（毫秒）
        /// </summary>
        [JsonProperty("totalTime")]
        public long TotalTime { get; set; }

        /// <summary>
        /// 获取或设置响应状态
        /// </summary>
        [JsonProperty("responseStatus")]
        public int ResponseStatus { get; set; }

        /// <summary>
        /// 获取或设置响应长度
        /// </summary>
        [JsonProperty("responseLength")]
        public long ResponseLength { get; set; }

        /// <summary>
        /// 获取或设置远程地址
        /// </summary>
        [JsonProperty("remoteAddr")]
        public string RemoteAddr { get; set; }

        /// <summary>
        /// 获取或设置远程主机
        /// </summary>
        [JsonProperty("remoteHost")]
        public string RemoteHost { get; set; }
    }

    /// <summary>
    /// 监控请求列表的包装器
    /// </summary>
    public class MonitorRequestListWrapper
    {
        /// <summary>
        /// 获取或设置请求列表
        /// </summary>
        [JsonProperty("requests")]
        public List<MonitorRequest> Requests { get; set; }
    }

    /// <summary>
    /// 表示监控统计信息
    /// </summary>
    public class MonitorStatistics
    {
        /// <summary>
        /// 获取或设置总请求数
        /// </summary>
        [JsonProperty("totalRequests")]
        public long TotalRequests { get; set; }

        /// <summary>
        /// 获取或设置平均响应时间
        /// </summary>
        [JsonProperty("avgResponseTime")]
        public double AvgResponseTime { get; set; }

        /// <summary>
        /// 获取或设置按路径分组的请求统计信息
        /// </summary>
        [JsonProperty("byPath")]
        public Dictionary<string, PathStatistics> ByPath { get; set; }
    }

    /// <summary>
    /// 表示特定路径的统计信息
    /// </summary>
    public class PathStatistics
    {
        /// <summary>
        /// 获取或设置请求数量
        /// </summary>
        [JsonProperty("count")]
        public long Count { get; set; }

        /// <summary>
        /// 获取或设置平均响应时间
        /// </summary>
        [JsonProperty("avgTime")]
        public double AvgTime { get; set; }

        /// <summary>
        /// 获取或设置传输的总字节数
        /// </summary>
        [JsonProperty("totalBytes")]
        public long TotalBytes { get; set; }
    }

    /// <summary>
    /// 表示转换（XSLT）
    /// </summary>
    public class Transform
    {
        /// <summary>
        /// 获取或设置转换名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置 XSLT 内容
        /// </summary>
        [JsonProperty("xslt")]
        public string XSLT { get; set; }

        /// <summary>
        /// 获取或设置源格式
        /// </summary>
        [JsonProperty("sourceFormat")]
        public string SourceFormat { get; set; }

        /// <summary>
        /// 获取或设置输出格式
        /// </summary>
        [JsonProperty("outputFormat")]
        public string OutputFormat { get; set; }
    }

    /// <summary>
    /// 转换列表的包装器
    /// </summary>
    public class TransformListWrapper
    {
        /// <summary>
        /// 获取或设置转换列表
        /// </summary>
        [JsonProperty("transforms")]
        public List<string> Transforms { get; set; }
    }

    /// <summary>
    /// 表示 URL 检查规则
    /// </summary>
    public class URLCheck
    {
        /// <summary>
        /// 获取或设置规则名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置描述
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// 获取或设置检查是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置 URL 模式（正则表达式）
        /// </summary>
        [JsonProperty("urlPattern")]
        public string UrlPattern { get; set; }

        /// <summary>
        /// 获取或设置检查类型（DENY、ALLOW）
        /// </summary>
        [JsonProperty("checkType")]
        public string CheckType { get; set; }
    }

    /// <summary>
    /// URL 检查列表的包装器
    /// </summary>
    public class URLCheckListWrapper
    {
        /// <summary>
        /// 获取或设置 URL 检查列表
        /// </summary>
        [JsonProperty("checks")]
        public List<string> Checks { get; set; }
    }
}
