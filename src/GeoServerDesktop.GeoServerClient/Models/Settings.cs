using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示 GeoServer 全局设置（GET /rest/settings.json 实测 3.0.1 根为 {"global":{"settings":{...}}}）。
    /// FIXED（E17 复核 + 整包 PUT 语义）：原模型期望根 "settings"，与真实形态不符导致 Settings 恒 null；
    /// 现模型对齐 "global" 根，并保留 Settings 便捷访问器（读写均透传到 Global.Settings）。
    /// 注意：/rest/settings 的 PUT 为整包替换语义（实测：请求体缺失的 settings 键会被重置为服务器默认值），
    /// 因此 GET→改目标字段→PUT 必须回传完整模型；GlobalSettingsInfo/Settings/ContactInfo 均带
    /// [JsonExtensionData]，把 GET 中出现但模型未显式声明的键（如 metadata、updateSequence 等）
    /// 原样捕获并在序列化时回写，保证往返不丢字段。
    /// </summary>
    public class GlobalSettings
    {
        /// <summary>
        /// 真实 REST 根包装 "global"
        /// </summary>
        [JsonProperty("global")]
        public GlobalSettingsInfo Global { get; set; }

        /// <summary>
        /// 便捷访问器：透传 Global.Settings（保持既有调用方 App 层 Settings 读写形态不变）
        /// </summary>
        [JsonIgnore]
        public Settings Settings
        {
            get { return Global != null ? Global.Settings : null; }
            set
            {
                if (Global == null)
                    Global = new GlobalSettingsInfo();
                Global.Settings = value;
            }
        }
    }

    /// <summary>
    /// "global" 包装体内的全局设置对象（settings 子块之外的键经 ExtensionData 无损往返）
    /// </summary>
    public class GlobalSettingsInfo
    {
        /// <summary>
        /// 获取或设置核心设置块
        /// </summary>
        [JsonProperty("settings")]
        public Settings Settings { get; set; }

        /// <summary>
        /// GET 返回中模型未显式声明的键（jai、coverageAccess、updateSequence、metadata 等），
        /// PUT 时原样回写，防整包替换丢字段
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    /// <summary>
    /// 表示 GeoServer 设置
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// 获取或设置目录对象 ID（GET 返回，PUT 回传保持身份稳定）
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

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

        /// <summary>
        /// 获取或设置是否启用工作空间级 include 前缀（实测 GET 返回键）
        /// </summary>
        [JsonProperty("localWorkspaceIncludesPrefix")]
        public bool? LocalWorkspaceIncludesPrefix { get; set; }

        /// <summary>
        /// 管理列表是否显示创建时间列（实测 GET 返回键）
        /// </summary>
        [JsonProperty("showCreatedTimeColumnsInAdminList")]
        public bool? ShowCreatedTimeColumnsInAdminList { get; set; }

        /// <summary>
        /// 管理列表是否显示修改时间列（实测 GET 返回键）
        /// </summary>
        [JsonProperty("showModifiedTimeColumnsInAdminList")]
        public bool? ShowModifiedTimeColumnsInAdminList { get; set; }

        /// <summary>
        /// 管理列表是否显示修改用户列（实测 GET 返回键）
        /// </summary>
        [JsonProperty("showModifiedUserAdminList")]
        public bool? ShowModifiedUserAdminList { get; set; }

        /// <summary>
        /// 是否使用请求头解析代理 URL（实测 GET 返回键）
        /// </summary>
        [JsonProperty("useHeadersProxyURL")]
        public bool? UseHeadersProxyURL { get; set; }

        /// <summary>
        /// GET 返回中出现但模型未声明的其余键（如 metadata），PUT 时原样回写
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    /// <summary>
    /// 表示联系信息。
    /// FIXED（E17 复核，判为误报）：实测 3.0.1 GET /rest/settings/contact.json 返回即为
    /// {"contact":{...平铺字段...}}——不存在双层 {"contact":{"contact":...,"address":{...}}}，
    /// 本模型的 address* 键本就是普通字符串字段名（无嵌套 address 对象）。
    /// 历史 JsonReaderException 的担忧源于对双层形态的误判；现补齐 GET 实际出现的
    /// onlineResource/welcome 键，保证读写往返不丢字段。
    /// </summary>
    public class ContactInfo
    {
        /// <summary>
        /// 目录对象 ID（部分操作后 GET 会带上，回传保持往返一致）
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

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
        /// 获取或设置街道地址（E17 复核：GET 无嵌套 address 对象，此字段即字符串；
        /// 之前的 JsonReaderException 仅在对"双层形态"的误判构造下出现，非真实服务器行为）
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

        /// <summary>
        /// 获取或设置联系信息级在线资源 URL（实测 GET 返回键，E17 复核补齐）
        /// </summary>
        [JsonProperty("onlineResource")]
        public string OnlineResource { get; set; }

        /// <summary>
        /// 获取或设置欢迎语（实测 GET 返回键，E17 复核补齐）
        /// </summary>
        [JsonProperty("welcome")]
        public string Welcome { get; set; }

        /// <summary>
        /// GET 返回中出现但模型未声明的其余键，PUT 时原样回写
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; }
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
