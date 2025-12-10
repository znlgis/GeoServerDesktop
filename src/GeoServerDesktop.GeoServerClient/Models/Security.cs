using Newtonsoft.Json;
using System.Collections.Generic;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示安全访问控制列表
    /// </summary>
    public class SecurityACL
    {
        /// <summary>
        /// 获取或设置资源路径
        /// </summary>
        [JsonProperty("resource")]
        public string Resource { get; set; }

        /// <summary>
        /// 获取或设置访问规则列表
        /// </summary>
        [JsonProperty("rules")]
        public List<AccessRule> Rules { get; set; }
    }

    /// <summary>
    /// 表示访问控制规则
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
    /// 用户组服务列表的包装器
    /// </summary>
    public class UserGroupServiceList
    {
        /// <summary>
        /// 获取或设置用户组服务列表
        /// </summary>
        [JsonProperty("userGroupServices")]
        public List<string> Services { get; set; }
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
        /// 获取或设置密码（只写）
        /// </summary>
        [JsonProperty("password")]
        public string Password { get; set; }

        /// <summary>
        /// 获取或设置用户是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// 获取或设置用户所属的组列表
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
    /// 用户列表的包装器
    /// </summary>
    public class UserListWrapper
    {
        /// <summary>
        /// 获取或设置用户列表
        /// </summary>
        [JsonProperty("users")]
        public List<string> Users { get; set; }
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
    /// 组列表的包装器
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
    /// 表示认证过滤器配置
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
        /// 获取或设置过滤器 configuration
        /// </summary>
        [JsonProperty("config")]
        public Dictionary<string, object> Config { get; set; }
    }

    /// <summary>
    /// Wrapper for authentication filter list
    /// </summary>
    public class AuthenticationFilterListWrapper
    {
        /// <summary>
        /// 获取或设置过滤器列表
        /// </summary>
        [JsonProperty("filters")]
        public List<string> Filters { get; set; }
    }

    /// <summary>
    /// Wrapper for authentication filter response
    /// </summary>
    public class AuthenticationFilterWrapper
    {
        /// <summary>
        /// 获取或设置过滤器
        /// </summary>
        [JsonProperty("filter")]
        public AuthenticationFilter Filter { get; set; }
    }

    /// <summary>
    /// 表示认证提供者配置
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
        /// 获取或设置提供者配置
        /// </summary>
        [JsonProperty("config")]
        public Dictionary<string, object> Config { get; set; }
    }

    /// <summary>
    /// Wrapper for authentication provider list
    /// </summary>
    public class AuthenticationProviderListWrapper
    {
        /// <summary>
        /// 获取或设置提供者列表
        /// </summary>
        [JsonProperty("providers")]
        public List<string> Providers { get; set; }
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
    /// 表示安全过滤器链配置
    /// </summary>
    public class FilterChain
    {
        /// <summary>
        /// 获取或设置链名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置链模式
        /// </summary>
        [JsonProperty("pattern")]
        public string Pattern { get; set; }

        /// <summary>
        /// 获取或设置过滤器列表 in the chain
        /// </summary>
        [JsonProperty("filters")]
        public List<string> Filters { get; set; }

        /// <summary>
        /// 获取或设置链是否禁用
        /// </summary>
        [JsonProperty("disabled")]
        public bool? Disabled { get; set; }

        /// <summary>
        /// 获取或设置是否允许创建会话
        /// </summary>
        [JsonProperty("allowSessionCreation")]
        public bool? AllowSessionCreation { get; set; }

        /// <summary>
        /// 获取或设置是否需要 SSL
        /// </summary>
        [JsonProperty("requireSSL")]
        public bool? RequireSSL { get; set; }

        /// <summary>
        /// 获取或设置是否匹配 HTTP 方法
        /// </summary>
        [JsonProperty("matchHTTPMethod")]
        public bool? MatchHTTPMethod { get; set; }
    }

    /// <summary>
    /// Wrapper for filter chain list
    /// </summary>
    public class FilterChainListWrapper
    {
        /// <summary>
        /// 获取或设置链列表
        /// </summary>
        [JsonProperty("filterChains")]
        public List<FilterChain> Chains { get; set; }
    }

    /// <summary>
    /// Wrapper for filter chain response
    /// </summary>
    public class FilterChainWrapper
    {
        /// <summary>
        /// 获取或设置过滤器 chain
        /// </summary>
        [JsonProperty("filterChain")]
        public FilterChain Chain { get; set; }
    }

    /// <summary>
    /// 表示密码更改请求
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
    /// 表示密钥存储条目
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
    /// 密钥存储的包装器 information
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
