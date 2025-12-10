using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示 GeoServer 样式
    /// </summary>
    public class Style
    {
        /// <summary>
        /// 样式的名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 样式的格式（例如 sld）
        /// </summary>
        [JsonProperty("format")]
        public string Format { get; set; }

        /// <summary>
        /// 样式格式的版本
        /// </summary>
        [JsonProperty("languageVersion")]
        public LanguageVersion LanguageVersion { get; set; }

        /// <summary>
        /// 样式的文件名
        /// </summary>
        [JsonProperty("filename")]
        public string Filename { get; set; }

        /// <summary>
        /// 样式资源的链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 样式语言版本
    /// </summary>
    public class LanguageVersion
    {
        /// <summary>
        /// 版本字符串
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }
    }

    /// <summary>
    /// 单个样式响应的包装器
    /// </summary>
    public class StyleWrapper
    {
        /// <summary>
        /// 样式数据
        /// </summary>
        [JsonProperty("style")]
        public Style Style { get; set; }
    }

    /// <summary>
    /// 样式列表
    /// </summary>
    public class StyleList
    {
        /// <summary>
        /// 样式数组
        /// </summary>
        [JsonProperty("style")]
        public Style[] Styles { get; set; }
    }

    /// <summary>
    /// 样式列表响应的包装器
    /// </summary>
    public class StyleListWrapper
    {
        /// <summary>
        /// 样式列表数据
        /// </summary>
        [JsonProperty("styles")]
        public StyleList StyleList { get; set; }
    }
}
