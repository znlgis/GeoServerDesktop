using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示 GeoServer 全局设置
    /// </summary>
    public class GlobalSettings
    {
        /// <summary>
        /// 获取或设置工作空间设置
        /// </summary>
        [JsonProperty("settings")]
        public Settings Settings { get; set; }
    }

    /// <summary>
    /// 表示 GeoServer 设置
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// 获取或设置字符集
        /// </summary>
        [JsonProperty("charset")]
        public string Charset { get; set; }

        /// <summary>
        /// 获取或设置编码浮点数时使用的小数位数
        /// </summary>
        [JsonProperty("numDecimals")]
        public int? NumDecimals { get; set; }

        /// <summary>
        /// 获取或设置在线资源 URL
        /// </summary>
        [JsonProperty("onlineResource")]
        public string OnlineResource { get; set; }

        /// <summary>
        /// 获取或设置是否启用详细消息
        /// </summary>
        [JsonProperty("verbose")]
        public bool? Verbose { get; set; }

        /// <summary>
        /// 获取或设置是否启用详细异常消息
        /// </summary>
        [JsonProperty("verboseExceptions")]
        public bool? VerboseExceptions { get; set; }

        /// <summary>
        /// 获取或设置联系信息
        /// </summary>
        [JsonProperty("contact")]
        public ContactInfo Contact { get; set; }

        /// <summary>
        /// 获取或设置代理基础 URL
        /// </summary>
        [JsonProperty("proxyBaseUrl")]
        public string ProxyBaseUrl { get; set; }

        /// <summary>
        /// 获取或设置日志位置
        /// </summary>
        [JsonProperty("loggingLocation")]
        public string LoggingLocation { get; set; }

        /// <summary>
        /// 获取或设置是否启用全局服务
        /// </summary>
        [JsonProperty("globalServices")]
        public bool? GlobalServices { get; set; }

        /// <summary>
        /// 获取或设置返回的最大要素数量
        /// </summary>
        [JsonProperty("maxFeatures")]
        public int? MaxFeatures { get; set; }
    }

    /// <summary>
    /// 表示联系信息
    /// </summary>
    public class ContactInfo
    {
        /// <summary>
        /// 获取或设置联系人
        /// </summary>
        [JsonProperty("contactPerson")]
        public string ContactPerson { get; set; }

        /// <summary>
        /// 获取或设置组织名称
        /// </summary>
        [JsonProperty("contactOrganization")]
        public string ContactOrganization { get; set; }

        /// <summary>
        /// 获取或设置职位名称
        /// </summary>
        [JsonProperty("contactPosition")]
        public string ContactPosition { get; set; }

        /// <summary>
        /// 获取或设置地址类型
        /// </summary>
        [JsonProperty("addressType")]
        public string AddressType { get; set; }

        /// <summary>
        /// 获取或设置街道地址
        /// </summary>
        [JsonProperty("address")]
        public string Address { get; set; }

        /// <summary>
        /// 获取或设置城市
        /// </summary>
        [JsonProperty("addressCity")]
        public string AddressCity { get; set; }

        /// <summary>
        /// 获取或设置州或省
        /// </summary>
        [JsonProperty("addressState")]
        public string AddressState { get; set; }

        /// <summary>
        /// 获取或设置邮政编码
        /// </summary>
        [JsonProperty("addressPostalCode")]
        public string AddressPostalCode { get; set; }

        /// <summary>
        /// 获取或设置国家
        /// </summary>
        [JsonProperty("addressCountry")]
        public string AddressCountry { get; set; }

        /// <summary>
        /// 获取或设置语音电话号码
        /// </summary>
        [JsonProperty("contactVoice")]
        public string ContactVoice { get; set; }

        /// <summary>
        /// 获取或设置传真电话号码
        /// </summary>
        [JsonProperty("contactFacsimile")]
        public string ContactFacsimile { get; set; }

        /// <summary>
        /// 获取或设置电子邮件地址
        /// </summary>
        [JsonProperty("contactEmail")]
        public string ContactEmail { get; set; }
    }

    /// <summary>
    /// 联系信息响应的包装器
    /// </summary>
    public class ContactInfoWrapper
    {
        /// <summary>
        /// 获取或设置联系信息数据
        /// </summary>
        [JsonProperty("contact")]
        public ContactInfo Contact { get; set; }
    }
}
