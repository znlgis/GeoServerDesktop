using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 字体列表的包装器
    /// </summary>
    public class FontListWrapper
    {
        /// <summary>
        /// 获取或设置字体列表
        /// </summary>
        [JsonProperty("fonts")]
        public List<string> Fonts { get; set; }
    }

    /// <summary>
    /// 表示要素模板
    /// </summary>
    public class Template
    {
        /// <summary>
        /// 获取或设置模板名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置模板内容
        /// </summary>
        [JsonProperty("content")]
        public string Content { get; set; }

        /// <summary>
        /// 获取或设置模板类型（例如：header、footer、content）
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }
    }

    /// <summary>
    /// 模板列表的包装器。
    /// FIXED-E7（新形态）：GeoServer 3.0.1 <c>/rest/templates.json</c> 实际根为 Java 类全名——
    /// 空态：<c>{"org.geoserver.rest.catalog.TemplateInfos":""}</c>；
    /// 有数据：<c>{"org.geoserver.rest.catalog.TemplateInfos":{"org.geoserver.rest.catalog.TemplateInfo":[{"name","href"},...]}}</c>。
    /// 模型改为承载解析后的模板名称列表。
    /// </summary>
    public class TemplateListWrapper
    {
        /// <summary>
        /// 已解析的模板列表（空态返回空列表）。
        /// </summary>
        [JsonIgnore]
        public List<string> Templates { get; set; }

        /// <summary>
        /// 解析 3.0.1 动态类名根。
        /// </summary>
        public static TemplateListWrapper Parse(string rawJson)
        {
            var w = new TemplateListWrapper { Templates = new List<string>() };
            if (string.IsNullOrWhiteSpace(rawJson)) return w;
            var obj = JObject.Parse(rawJson);
            foreach (var outer in obj.Properties())
            {
                // 顶层为 "org.geoserver.rest.catalog.TemplateInfos"：值为 "" 或 内层对象
                var v = outer.Value;
                if (v.Type == JTokenType.String) continue; // 空串
                if (v is JObject inner)
                {
                    foreach (var arr in inner.Properties())
                    {
                        if (arr.Value is JArray ja)
                            foreach (var item in ja)
                            {
                                var name = (string)item["name"];
                                if (!string.IsNullOrEmpty(name)) w.Templates.Add(name);
                            }
                    }
                }
                else if (v is JArray arr2)
                {
                    foreach (var item in arr2)
                    {
                        var name = (string)item["name"];
                        if (!string.IsNullOrEmpty(name)) w.Templates.Add(name);
                    }
                }
            }
            return w;
        }
    }

}
