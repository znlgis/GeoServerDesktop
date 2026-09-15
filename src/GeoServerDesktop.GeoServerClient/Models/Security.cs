using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示安全访问控制列表（GeoServer 3.0.1 形态）。
    /// 实测：/rest/security/acl/catalog 返回 <c>{"mode":"HIDE"}</c>；
    /// /rest/security/acl/{services|layers|rest} 均返回扁平 <c>{"&lt;key&gt;":"&lt;comma-separated roles&gt;"}</c> map。
    /// 旧版扁平 <c>{"resource","rules":[{role,access}]}</c> 形态已不存在（E19 修复：模型改以 Mode + Rules 承接 3.0.1 实际响应）。
    /// <see cref="Rules"/> 由扁平 map 转译：key → role、value → access，兼容既有 App 视图模型。
    /// </summary>
    public class SecurityACL
    {
        /// <summary>
        /// 仅 /acl/catalog 有值（例如 "HIDE"/"MIXED"/"BANNER"/"DISABLE"）；其它子资源为 null。
        /// </summary>
        [JsonProperty("mode")]
        public string Mode { get; set; }

        /// <summary>
        /// 兼容既有调用点：3.0.1 由扁平 map 转译出的规则序列（Role = 键，Access = 值）。
        /// </summary>
        [JsonIgnore]
        public List<AccessRule> Rules { get; set; }

        /// <summary>
        /// 3.0.1 原始 map（key=形如 <c>*.*.r</c> 的资源模式；value=逗号分隔角色）。
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, string> RawRules { get; set; }

        /// <summary>
        /// 3.0.1 兼容：从 mode 与 map 两形态统一解析。调用方传入控制器已 GET 的原始 JSON。
        /// </summary>
        public static SecurityACL Parse(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return null;
            var token = JToken.Parse(rawJson);
            var result = new SecurityACL();
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                if (obj["mode"] != null) result.Mode = (string)obj["mode"];
                else
                {
                    result.RawRules = obj.ToObject<Dictionary<string, string>>() ?? new Dictionary<string, string>();
                    result.Rules = new List<AccessRule>();
                    foreach (var kv in result.RawRules)
                        result.Rules.Add(new AccessRule { Role = kv.Key, Access = kv.Value });
                }
            }
            return result;
        }
    }

    /// <summary>
    /// 表示访问控制规则（旧形态兼容，3.0.1 已不再返回，保留供反序列化兼容）
    /// </summary>
    public class AccessRule
    {
        /// <summary>
        /// 获取或设置角色
        /// </summary>
        [JsonProperty("role")]
        public string Role { get; set; }

        /// <summary>
        /// 获取或设置访问级别（READ、WRITE、ADMIN）
        /// </summary>
        [JsonProperty("access")]
        public string Access { get; set; }
    }

    /// <summary>
    /// 用户组服务列表的包装器。
    /// FIXED-E7：3.0.1 实测用户组服务列表路由为 <c>/rest/security/usergroupservices</c>（UserGroupServiceController），
    /// 响应形如 <c>{"userGroupServices":{"userGroupService":[{"name","href"},...]}}</c>；
    /// 旧 <c>/usergroup/services.json</c> 路由 404。提供 <see cref="Parse"/> 容错解析（空串/数组/双层对象）。
    /// </summary>
    public class UserGroupServiceList
    {
        /// <summary>
        /// 获取或设置用户组服务名称列表（解析自 3.0.1 双层结构）。
        /// </summary>
        [JsonIgnore]
        public List<string> Services { get; set; }

        /// <summary>
        /// 服务引用（name + href）。
        /// </summary>
        [JsonIgnore]
        public List<UserGroupServiceRef> References { get; set; }

        /// <summary>
        /// 容错解析 3.0.1 响应；对旧扁平数组形态同样兼容。
        /// </summary>
        public static UserGroupServiceList Parse(string rawJson)
        {
            var w = new UserGroupServiceList { Services = new List<string>(), References = new List<UserGroupServiceRef>() };
            if (string.IsNullOrWhiteSpace(rawJson)) return w;
            var token = JToken.Parse(rawJson);
            JToken inner = token.Type == JTokenType.Object ? token["userGroupServices"] : token;
            if (inner == null || inner.Type == JTokenType.String) return w;
            // 直接数组形态原样透传给下方 JArray 分支；对象形态尝试内层 userGroupService 键。
            if (inner.Type == JTokenType.Object) inner = inner["userGroupService"] ?? inner["string"] ?? inner;
            if (inner is JArray arr)
            {
                foreach (var item in arr)
                {
                    if (item.Type == JTokenType.String)
                    {
                        w.Services.Add((string)item);
                        w.References.Add(new UserGroupServiceRef { Name = (string)item });
                    }
                    else if (item is JObject io)
                    {
                        var name = (string)io["name"];
                        if (!string.IsNullOrEmpty(name)) w.Services.Add(name);
                        w.References.Add(new UserGroupServiceRef { Name = name, Href = (string)io["href"] });
                    }
                }
            }
            return w;
        }
    }

    /// <summary>
    /// 用户组服务引用（name + href）。
    /// </summary>
    public class UserGroupServiceRef
    {
        /// <summary>服务名称。</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>服务详情端点 href。</summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 表示系统中的用户
    /// </summary>
    public class User
    {
        /// <summary>
        /// 获取或设置用户名
        /// </summary>
        [JsonProperty("userName")]
        public string UserName { get; set; }

        /// <summary>
        /// 获取或设置密码（只写；实测 3.0.1 服务端读取返回 null，写入时必须显式提供）
        /// </summary>
        [JsonProperty("password")]
        public string Password { get; set; }

        /// <summary>
        /// 获取或设置用户是否启用。POST 创建时必须显式设值，否则 GeoServer 3.0.1 用户控制器 NPE 500。
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置用户所属的组列表（仅 GET /user/{user}/groups 提供独立端点；list 端点不返回）
        /// </summary>
        [JsonProperty("groups")]
        public List<string> Groups { get; set; }
    }

    /// <summary>
    /// Wrapper for user response
    /// </summary>
    public class UserWrapper
    {
        /// <summary>
        /// 获取或设置用户
        /// </summary>
        [JsonProperty("user")]
        public User User { get; set; }
    }

    /// <summary>
    /// 用户列表的包装器。
    /// FIXED-E7：GeoServer 3.0.1 <c>/rest/security/usergroup/users.json</c> 实际直接返回
    /// <c>{"users":[{"enabled","password","userName"}, ...]}</c>（对象数组、无内层 user 键）；
    /// 模型由 <c>List&lt;string&gt;</c> 改为 <c>List&lt;User&gt;</c> 并暴露便捷用户名列表。
    /// </summary>
    public class UserListWrapper
    {
        /// <summary>
        /// 用户对象列表（3.0.1 实测形态）。
        /// </summary>
        [JsonProperty("users")]
        public List<User> Users { get; set; }

        /// <summary>
        /// 便捷属性：用户名列表。
        /// </summary>
        [JsonIgnore]
        public List<string> UserNames
        {
            get
            {
                var list = new List<string>();
                if (Users != null)
                    foreach (var u in Users)
                        if (u != null && u.UserName != null) list.Add(u.UserName);
                return list;
            }
        }
    }

    /// <summary>
    /// 表示用户组
    /// </summary>
    public class UserGroup
    {
        /// <summary>
        /// 获取或设置组 name
        /// </summary>
        [JsonProperty("groupName")]
        public string GroupName { get; set; }

        /// <summary>
        /// 获取或设置组是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置用户列表 in the group
        /// </summary>
        [JsonProperty("users")]
        public List<string> Users { get; set; }
    }

    /// <summary>
    /// Wrapper for group response
    /// </summary>
    public class GroupWrapper
    {
        /// <summary>
        /// 获取或设置组
        /// </summary>
        [JsonProperty("group")]
        public UserGroup Group { get; set; }
    }

    /// <summary>
    /// 组列表的包装器。
    /// FIXED-E7：GeoServer 3.0.1 <c>groups.json</c> 实测为 <c>{"groups":["str",...]}</c> 直接字符串数组，
    /// 模型保持 <c>List&lt;string&gt;</c> 即可（旧基线"预期抛异常"为误报，此处已翻正）。
    /// </summary>
    public class GroupListWrapper
    {
        /// <summary>
        /// 获取或设置组列表
        /// </summary>
        [JsonProperty("groups")]
        public List<string> Groups { get; set; }
    }

    /// <summary>
    /// 表示安全角色
    /// </summary>
    public class Role
    {
        /// <summary>
        /// 获取或设置角色 name
        /// </summary>
        [JsonProperty("role")]
        public string RoleName { get; set; }

        /// <summary>
        /// 获取或设置父角色
        /// </summary>
        [JsonProperty("parentRole")]
        public string ParentRole { get; set; }

        /// <summary>
        /// 获取或设置角色 properties
        /// </summary>
        [JsonProperty("properties")]
        public Dictionary<string, string> Properties { get; set; }
    }

    /// <summary>
    /// 角色列表的包装器
    /// </summary>
    public class RoleListWrapper
    {
        /// <summary>
        /// 获取或设置角色列表
        /// </summary>
        [JsonProperty("roles")]
        public List<string> Roles { get; set; }
    }

    /// <summary>
    /// Wrapper for role response
    /// </summary>
    public class RoleWrapper
    {
        /// <summary>
        /// 获取或设置角色
        /// </summary>
        [JsonProperty("role")]
        public Role Role { get; set; }
    }

    /// <summary>
    /// 表示认证过滤器配置。
    /// FIXED-E1：路径改为全小写 authfilters；GET 单体 3.0.1 实测返回
    /// <c>{"&lt;配置类 FQN&gt;":{"id","name","className",...}}</c>（以配置类全名为动态根键），
    /// 因此新增 <see cref="Raw"/> 保存原始 JToken 以便消费动态字段。
    /// </summary>
    public class AuthenticationFilter
    {
        /// <summary>
        /// 获取或设置过滤器 name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置过滤器 class name
        /// </summary>
        [JsonProperty("className")]
        public string ClassName { get; set; }

        /// <summary>
        /// 配置类全限定名（3.0.1 动态键；<c>{"org.geoserver.security.config.BasicAuthenticationFilterConfig":{...}}</c>）
        /// </summary>
        [JsonIgnore]
        public string ConfigClassName { get; set; }

        /// <summary>
        /// 获取或设置过滤器 configuration（3.0.1 单体响应里除 name/className 外的其余字段）
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, object> Config { get; set; }

        /// <summary>
        /// 从 3.0.1 动态键响应解析单个 filter 对象。响应示例：
        /// <c>{"org.geoserver.security.config.BasicAuthenticationFilterConfig":{"id":"...","name":"basic","className":"...","useRememberMe":true}}</c>
        /// </summary>
        public static AuthenticationFilter ParseSingle(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return null;
            var obj = JObject.Parse(rawJson);
            foreach (var prop in obj.Properties())
            {
                var inner = prop.Value as JObject;
                if (inner == null) continue;
                var f = new AuthenticationFilter
                {
                    ConfigClassName = prop.Name,
                    Name = (string)inner["name"],
                    ClassName = (string)inner["className"],
                };
                var cfg = new Dictionary<string, object>();
                foreach (var p in inner.Properties())
                {
                    if (p.Name == "name" || p.Name == "className") continue;
                    cfg[p.Name] = p.Value.Type == JTokenType.Object || p.Value.Type == JTokenType.Array
                        ? (object)p.Value : p.Value.ToObject<object>();
                }
                f.Config = cfg;
                return f;
            }
            return null;
        }
    }

    /// <summary>
    /// Wrapper for authentication filter list。
    /// FIXED-E1 + FIXED-E7：3.0.1 <c>/rest/security/authfilters.json</c> 实际为
    /// <c>{"authfilters":{"authfilter":[{"name","href"},...]}}</c>，模型改为 <see cref="Authfilters"/> 承接内层 authfilter 数组。
    /// </summary>
    public class AuthenticationFilterListWrapper
    {
        /// <summary>
        /// 内层 authfilter 数组（name + href 引用）。
        /// </summary>
        [JsonProperty("authfilter")]
        public List<AuthenticationFilterRef> Filters { get; set; }

        /// <summary>
        /// 容错解析 3.0.1 双层响应 <c>{"authfilters":{"authfilter":[...]}}</c>；
        /// 亦接受裸数组、<c>{"authfilter":[...]}</c> 与空串（无过滤器）。
        /// </summary>
        public static AuthenticationFilterListWrapper Parse(string rawJson)
        {
            var w = new AuthenticationFilterListWrapper { Filters = new List<AuthenticationFilterRef>() };
            if (string.IsNullOrWhiteSpace(rawJson)) return w;
            var token = JToken.Parse(rawJson);
            JToken inner = token.Type == JTokenType.Object ? (token["authfilters"] ?? token["authfilter"] ?? token["filters"]) : token;
            if (inner == null || inner.Type == JTokenType.String) return w;
            if (inner.Type == JTokenType.Object) inner = inner["authfilter"] ?? inner["string"] ?? inner;
            if (inner is JArray arr)
                foreach (var item in arr)
                    w.Filters.Add(item.ToObject<AuthenticationFilterRef>());
            return w;
        }
    }

    /// <summary>
    /// 认证过滤器列表项（name + href）。
    /// </summary>
    public class AuthenticationFilterRef
    {
        /// <summary>
        /// 过滤器名称。
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 过滤器详情端点 href。
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// Wrapper for authentication filter response
    /// </summary>
    public class AuthenticationFilterWrapper
    {
        /// <summary>
        /// 获取或设置过滤器（3.0.1 单体响应根为动态配置类全名，需 <see cref="AuthenticationFilter.ParseSingle"/> 解析）
        /// </summary>
        [JsonProperty("filter")]
        public AuthenticationFilter Filter { get; set; }
    }

    /// <summary>
    /// 表示认证提供者配置。
    /// FIXED-E2 + FIXED-E7：3.0.1 <c>/rest/security/authproviders.json</c> 返回
    /// <c>{"authproviders":{"&lt;配置类 FQN&gt;":{...}}}</c>（以配置类全名为动态键）；
    /// 单体 GET 亦以类名为根键。<see cref="ConfigClassName"/> 保留类名，<see cref="Config"/> 保留其余字段。
    /// </summary>
    public class AuthenticationProvider
    {
        /// <summary>
        /// 获取或设置提供者名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置提供者类名
        /// </summary>
        [JsonProperty("className")]
        public string ClassName { get; set; }

        /// <summary>
        /// 提供者 id（3.0.1 响应含此字段）
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// 配置类全限定名（作为外层动态键）
        /// </summary>
        [JsonIgnore]
        public string ConfigClassName { get; set; }

        /// <summary>
        /// 获取或设置提供者配置（除 name/className/id 外的字段）
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, object> Config { get; set; }
    }

    /// <summary>
    /// Wrapper for authentication provider list。
    /// FIXED-E2：3.0.1 集合根为 <c>{"authproviders":{"&lt;FQN&gt;":{...}}}</c>，动态配置类名作为字典键。
    /// 通过 <see cref="Providers"/> 暴露已解析列表。
    /// </summary>
    public class AuthenticationProviderListWrapper
    {
        /// <summary>
        /// 原始 authproviders 字典：key=配置类全名，value=provider 字段。
        /// </summary>
        [JsonProperty("authproviders")]
        public Dictionary<string, JObject> Raw { get; set; }

        /// <summary>
        /// 便捷属性：已解析的 AuthenticationProvider 列表。
        /// </summary>
        [JsonIgnore]
        public List<AuthenticationProvider> Providers
        {
            get
            {
                var list = new List<AuthenticationProvider>();
                if (Raw == null) return list;
                foreach (var kv in Raw)
                {
                    var p = ParseProvider(kv.Value, kv.Key);
                    if (p != null) list.Add(p);
                }
                return list;
            }
        }

        /// <summary>
        /// 容错解析 3.0.1 响应 <c>{"authproviders":{"&lt;FQN&gt;":{...},...}}</c>；空串 → 空集合。
        /// </summary>
        public static AuthenticationProviderListWrapper Parse(string rawJson)
        {
            var w = new AuthenticationProviderListWrapper();
            if (string.IsNullOrWhiteSpace(rawJson)) { w.Raw = new Dictionary<string, JObject>(); return w; }
            var token = JToken.Parse(rawJson);
            JToken inner = token.Type == JTokenType.Object ? (token["authproviders"] ?? token) : token;
            w.Raw = new Dictionary<string, JObject>();
            if (inner is JObject io)
            {
                foreach (var p in io.Properties())
                    if (p.Value is JObject jo) w.Raw[p.Name] = jo;
            }
            else if (inner is JArray arr)
            {
                // 旧式 {"authproviders":{"authprovider":[{...}]}} 数组形态兜底
                foreach (var item in arr)
                    if (item is JObject jo)
                    {
                        var key = (string)jo["className"] ?? (string)jo["name"] ?? w.Raw.Count.ToString();
                        w.Raw[key] = jo;
                    }
            }
            return w;
        }

        /// <summary>
        /// 解析 3.0.1 单体响应：根为配置类全名（<c>{"&lt;FQN&gt;":{...}}</c>），亦兼容 "provider" 键。
        /// </summary>
        public static AuthenticationProvider ParseSingle(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return null;
            var obj = JObject.Parse(rawJson);
            foreach (var prop in obj.Properties())
            {
                var inner = prop.Value as JObject;
                if (inner == null) continue;
                return ParseProvider(inner, prop.Name == "provider" ? (string)inner["className"] : prop.Name);
            }
            return null;
        }

        public static AuthenticationProvider ParseProvider(JObject inner, string configClassName)
        {
            if (inner == null) return null;
            var prov = new AuthenticationProvider
            {
                ConfigClassName = configClassName,
                Name = (string)inner["name"],
                ClassName = (string)inner["className"],
                Id = (string)inner["id"],
            };
            var cfg = new Dictionary<string, object>();
            foreach (var p in inner.Properties())
            {
                if (p.Name == "name" || p.Name == "className" || p.Name == "id") continue;
                cfg[p.Name] = p.Value.Type == JTokenType.Object || p.Value.Type == JTokenType.Array
                    ? (object)p.Value : p.Value.ToObject<object>();
            }
            prov.Config = cfg;
            return prov;
        }
    }

    /// <summary>
    /// Wrapper for authentication provider response
    /// </summary>
    public class AuthenticationProviderWrapper
    {
        /// <summary>
        /// 获取或设置提供者
        /// </summary>
        [JsonProperty("provider")]
        public AuthenticationProvider Provider { get; set; }
    }

    /// <summary>
    /// 容错：JSON 值可能是字符串或字符串数组，统一读为 List&lt;string&gt;（FIXED-E3：3.0.1 filterchain
    /// 元素 filter 字段实测可为标量，如 "form"）。
    /// </summary>
    internal sealed class StringOrArrayConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(List<string>);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var token = JToken.Load(reader);
            if (token.Type == JTokenType.String) return new List<string> { (string)token };
            if (token.Type == JTokenType.Array) return token.ToObject<List<string>>();
            return new List<string>();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    /// <summary>
    /// 表示安全过滤器链配置（3.0.1 形态）。
    /// FIXED-E3：3.0.1 <c>/rest/security/filterchain</c>（单数）为真实路由，列表响应为
    /// <c>{"filterchain":{"filters":[{...},...]}}</c>；单体响应为 <c>{"filters":{...}}</c>。
    /// 元素字段以 <c>@name/@class/@path</c> 等属性形式给出。
    /// </summary>
    public class FilterChain
    {
        /// <summary>
        /// 链名称（JSON 中为 @name）
        /// </summary>
        [JsonProperty("@name")]
        public string Name { get; set; }

        /// <summary>
        /// 链实现类（JSON 中为 @class）
        /// </summary>
        [JsonProperty("@class")]
        public string ChainClass { get; set; }

        /// <summary>
        /// URL 匹配模式（JSON 中为 @path）
        /// </summary>
        [JsonProperty("@path")]
        public string Pattern { get; set; }

        /// <summary>
        /// 获取或设置过滤器列表 in the chain（3.0.1 实测 JSON 中 filter 可能是数组，也可能是标量字符串，
        /// 如 {"filter":"form"} —— 由 StringOrArrayConverter 容错承接）。
        /// </summary>
        [JsonProperty("filter")]
        [JsonConverter(typeof(StringOrArrayConverter))]
        public List<string> Filters { get; set; }

        /// <summary>
        /// 获取或设置链是否禁用（JSON 中为 @disabled）
        /// </summary>
        [JsonProperty("@disabled")]
        public bool? Disabled { get; set; }

        /// <summary>
        /// 获取或设置是否允许创建会话（JSON 中为 @allowSessionCreation）
        /// </summary>
        [JsonProperty("@allowSessionCreation")]
        public bool? AllowSessionCreation { get; set; }

        /// <summary>
        /// 获取或设置是否需要 SSL（JSON 中为 @ssl）
        /// </summary>
        [JsonProperty("@ssl")]
        public bool? RequireSSL { get; set; }

        /// <summary>
        /// 获取或设置是否匹配 HTTP 方法（JSON 中为 @matchHTTPMethod）
        /// </summary>
        [JsonProperty("@matchHTTPMethod")]
        public bool? MatchHTTPMethod { get; set; }
    }

    /// <summary>
    /// Wrapper for filter chain list。
    /// FIXED-E3：3.0.1 集合根为 <c>filterchain</c>（单数），内层 <c>filters</c> 为链数组。
    /// </summary>
    public class FilterChainListWrapper
    {
        /// <summary>
        /// 内层 filters 数组（GeoServer 语义：整个 filterchain 由 filters 列表构成）。
        /// </summary>
        [JsonProperty("filters")]
        public List<FilterChain> Chains { get; set; }

        /// <summary>
        /// 容错解析 3.0.1 响应 <c>{"filterchain":{"filters":[...]}}</c>；亦接受 <c>{"filters":[...]}</c>/裸数组/空串。
        /// </summary>
        public static FilterChainListWrapper Parse(string rawJson)
        {
            var w = new FilterChainListWrapper { Chains = new List<FilterChain>() };
            if (string.IsNullOrWhiteSpace(rawJson)) return w;
            var token = JToken.Parse(rawJson);
            JToken inner = token.Type == JTokenType.Object ? (token["filterchain"] ?? token["filterChains"] ?? token["filters"]) : token;
            if (inner == null || inner.Type == JTokenType.String) return w;
            if (inner.Type == JTokenType.Object) inner = inner["filters"] ?? inner["filterChain"] ?? inner;
            if (inner is JArray arr)
                foreach (var item in arr)
                {
                    if (item.Type == JTokenType.String) continue;
                    w.Chains.Add(item.ToObject<FilterChain>());
                }
            else if (inner is JObject obj)
            {
                w.Chains.Add(obj.ToObject<FilterChain>());
            }
            return w;
        }
    }

    /// <summary>
    /// Wrapper for filter chain response
    /// </summary>
    public class FilterChainWrapper
    {
        /// <summary>
        /// 获取或设置过滤器 chain（3.0.1 单体根键为 "filters"）
        /// </summary>
        [JsonProperty("filters")]
        public FilterChain Chain { get; set; }
    }

    /// <summary>
    /// 表示密码更改请求。
    /// FIXED-E16（原基线误报）：3.0.1 <c>/rest/security/self/password</c> 的 PUT 请求体就是扁平
    /// <c>{"newPassword":"..."}</c>（UserPasswordController#passwordPut Map&lt;String,String&gt; @RequestBody），
    /// 加外层 <c>{"password":{...}}</c> 反而 400 "Missing 'newPassword'"。当前实现与真实契约一致。
    /// </summary>
    public class PasswordChangeRequest
    {
        /// <summary>
        /// 获取或设置新密码
        /// </summary>
        [JsonProperty("newPassword")]
        public string NewPassword { get; set; }
    }

    /// <summary>
    /// 表示密钥存储条目（3.0.1 无 REST 端点，保留模型）
    /// </summary>
    public class KeystoreEntry
    {
        /// <summary>
        /// 获取或设置别名
        /// </summary>
        [JsonProperty("alias")]
        public string Alias { get; set; }

        /// <summary>
        /// 获取或设置条目类型
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// 获取或设置算法
        /// </summary>
        [JsonProperty("algorithm")]
        public string Algorithm { get; set; }
    }

    /// <summary>
    /// 密钥存储的包装器 information（3.0.1 无对应 REST 端点，保留兼容旧形态）。
    /// </summary>
    public class KeystoreInfo
    {
        /// <summary>
        /// 获取或设置密钥存储类型
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// 获取或设置密钥存储提供者
        /// </summary>
        [JsonProperty("provider")]
        public string Provider { get; set; }

        /// <summary>
        /// 获取或设置别名列表
        /// </summary>
        [JsonProperty("aliases")]
        public List<KeystoreEntry> Aliases { get; set; }
    }
}
