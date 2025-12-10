using Newtonsoft.Json;
using System.Collections.Generic;

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
    /// 模板列表的包装器
    /// </summary>
    public class TemplateListWrapper
    {
        /// <summary>
        /// 获取或设置模板列表
        /// </summary>
        [JsonProperty("templates")]
        public List<string> Templates { get; set; }
    }
}
