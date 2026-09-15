using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    /// 表示监控请求信息。
    /// FIXED-E7：3.0.1 <c>/rest/monitor/requests.json</c> 实际返回
    /// <c>{"org.geoserver.monitor.RequestDatas":{"org.geoserver.monitor.RequestData":[{name,href},...]}}</c>
    /// （以 Java 类名为动态根键，且列表项仅含 name/href 摘要，不含完整请求详情）。
    /// </summary>
    public class MonitorRequest
    {
        /// <summary>
        /// 获取或设置请求 ID（GWC 3.0.1 里为 name 字段的整数值）
        /// </summary>
        [JsonProperty("name")]
        public long Id { get; set; }

        /// <summary>
        /// 请求详情端点 href
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }

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
    /// 监控请求列表的包装器（GWC 3.0.1 动态类名键）。
    /// </summary>
    public class MonitorRequestListWrapper
    {
        /// <summary>
        /// 请求列表（由 <see cref="Parse"/> 从动态键抽取）。
        /// </summary>
        [JsonIgnore]
        public List<MonitorRequest> Requests { get; set; }

        /// <summary>
        /// 从 <c>{"&lt;RequestDatasFQN&gt;":{"&lt;RequestDataFQN&gt;":[{name,href},...]}}</c> 手工解析。
        /// </summary>
        public static MonitorRequestListWrapper Parse(string rawJson)
        {
            var wrapper = new MonitorRequestListWrapper { Requests = new List<MonitorRequest>() };
            if (string.IsNullOrWhiteSpace(rawJson)) return wrapper;
            var obj = JObject.Parse(rawJson);
            foreach (var outer in obj.Properties())
            {
                var inner = outer.Value as JObject;
                if (inner == null) continue;
                foreach (var arr in inner.Properties())
                {
                    if (arr.Value is JArray ja)
                    {
                        foreach (var t in ja)
                        {
                            var r = t.ToObject<MonitorRequest>();
                            if (r != null) wrapper.Requests.Add(r);
                        }
                    }
                }
            }
            return wrapper;
        }
    }

    /// <summary>
    /// 表示监控统计信息（GWC 3.0.1 <c>/rest/monitor/statistics</c> 端点在默认安装里返回 404；
    /// 该模型仅在启用 monitor 扩展后可用）。
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
    /// URL 检查列表的包装器。
    /// FIXED-E7：3.0.1 根键为 <c>urlChecks</c>（大小写混合），且无检查时值为空字符串（<c>{"urlChecks":""}</c>）；
    /// 有检查时形态为 <c>{"urlChecks":{"&lt;检查实现类&gt;":{...}}}</c>。
    /// </summary>
    public class URLCheckListWrapper
    {
        /// <summary>
        /// 已解析的 URL 检查名称列表（空字符串响应 → 空列表；对象 → 单/多键名）。
        /// </summary>
        [JsonIgnore]
        public List<string> Checks { get; set; }

        /// <summary>
        /// 原始内层数据（数组/对象/空串）。
        /// </summary>
        [JsonIgnore]
        public JToken Raw { get; set; }

        /// <summary>
        /// 解析 <c>{"urlChecks":...}</c> 形态：空串=无检查；
        /// 有值时按文档为 <c>{"urlChecks":{"entry":[{"string":["name","value"]},...]}}</c>，
        /// 兼容对象直接键与对象数组。
        /// </summary>
        public static URLCheckListWrapper Parse(string rawJson)
        {
            var w = new URLCheckListWrapper { Checks = new List<string>() };
            if (string.IsNullOrWhiteSpace(rawJson)) return w;
            var obj = JObject.Parse(rawJson);
            var v = obj["urlChecks"] ?? obj["URLChecks"];
            w.Raw = v;
            if (v == null) return w;
            if (v.Type == JTokenType.String) return w; // 空串 = 无检查
            if (v is JObject vo && vo["entry"] is JArray entryArr)
            {
                foreach (var e in entryArr) AddEntryName(w, e);
                return w;
            }
            if (v is JArray arr)
            {
                foreach (var item in arr)
                {
                    if (item.Type == JTokenType.String) w.Checks.Add((string)item);
                    else AddEntryName(w, item);
                }
                return w;
            }
            if (v is JObject jo)
            {
                foreach (var p in jo.Properties())
                {
                    var name = (string)((p.Value as JObject)?["name"]) ?? p.Name;
                    w.Checks.Add(name);
                }
            }
            return w;
        }

        private static void AddEntryName(URLCheckListWrapper w, JToken e)
        {
            if (e is JObject eo)
            {
                if (eo["string"] is JArray s && s.Count > 0) { w.Checks.Add((string)s[0]); return; }
                var name = (string)eo["name"];
                if (!string.IsNullOrEmpty(name)) { w.Checks.Add(name); return; }
                foreach (var p in eo.Properties())
                {
                    if (p.Value.Type == JTokenType.String) { w.Checks.Add((string)p.Value); return; }
                }
            }
            else if (e.Type == JTokenType.String) w.Checks.Add((string)e);
        }
    }
}
