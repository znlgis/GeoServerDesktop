using Newtonsoft.Json;
using System.Collections.Generic;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Represents security access control list
    /// </summary>
    public class SecurityACL
    {
        /// <summary>
        /// Gets or sets the resource path
        /// </summary>
        [JsonProperty("resource")]
        public string Resource { get; set; }

        /// <summary>
        /// Gets or sets the list of access rules
        /// </summary>
        [JsonProperty("rules")]
        public List<AccessRule> Rules { get; set; }
    }

    /// <summary>
    /// Represents an access control rule
    /// </summary>
    public class AccessRule
    {
        /// <summary>
        /// Gets or sets the role
        /// </summary>
        [JsonProperty("role")]
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets the access level (READ, WRITE, ADMIN)
        /// </summary>
        [JsonProperty("access")]
        public string Access { get; set; }
    }

    /// <summary>
    /// Wrapper for user group services list
    /// </summary>
    public class UserGroupServiceList
    {
        /// <summary>
        /// Gets or sets the list of user group services
        /// </summary>
        [JsonProperty("userGroupServices")]
        public List<string> Services { get; set; }
    }

    /// <summary>
    /// Represents a user in the system
    /// </summary>
    public class User
    {
        /// <summary>
        /// Gets or sets the username
        /// </summary>
        [JsonProperty("userName")]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the password (write-only)
        /// </summary>
        [JsonProperty("password")]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets whether the user is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the list of groups the user belongs to
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
        /// Gets or sets the user
        /// </summary>
        [JsonProperty("user")]
        public User User { get; set; }
    }

    /// <summary>
    /// Wrapper for users list
    /// </summary>
    public class UserListWrapper
    {
        /// <summary>
        /// Gets or sets the list of users
        /// </summary>
        [JsonProperty("users")]
        public List<string> Users { get; set; }
    }

    /// <summary>
    /// Represents a user group
    /// </summary>
    public class UserGroup
    {
        /// <summary>
        /// Gets or sets the group name
        /// </summary>
        [JsonProperty("groupName")]
        public string GroupName { get; set; }

        /// <summary>
        /// Gets or sets whether the group is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the list of users in the group
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
        /// Gets or sets the group
        /// </summary>
        [JsonProperty("group")]
        public UserGroup Group { get; set; }
    }

    /// <summary>
    /// Wrapper for groups list
    /// </summary>
    public class GroupListWrapper
    {
        /// <summary>
        /// Gets or sets the list of groups
        /// </summary>
        [JsonProperty("groups")]
        public List<string> Groups { get; set; }
    }

    /// <summary>
    /// Represents a security role
    /// </summary>
    public class Role
    {
        /// <summary>
        /// Gets or sets the role name
        /// </summary>
        [JsonProperty("role")]
        public string RoleName { get; set; }

        /// <summary>
        /// Gets or sets the parent role
        /// </summary>
        [JsonProperty("parentRole")]
        public string ParentRole { get; set; }

        /// <summary>
        /// Gets or sets the role properties
        /// </summary>
        [JsonProperty("properties")]
        public Dictionary<string, string> Properties { get; set; }
    }

    /// <summary>
    /// Wrapper for roles list
    /// </summary>
    public class RoleListWrapper
    {
        /// <summary>
        /// Gets or sets the list of roles
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
        /// Gets or sets the role
        /// </summary>
        [JsonProperty("role")]
        public Role Role { get; set; }
    }

    /// <summary>
    /// Represents an authentication filter configuration
    /// </summary>
    public class AuthenticationFilter
    {
        /// <summary>
        /// Gets or sets the filter name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the filter class name
        /// </summary>
        [JsonProperty("className")]
        public string ClassName { get; set; }

        /// <summary>
        /// Gets or sets the filter configuration
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
        /// Gets or sets the list of filters
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
        /// Gets or sets the filter
        /// </summary>
        [JsonProperty("filter")]
        public AuthenticationFilter Filter { get; set; }
    }

    /// <summary>
    /// Represents an authentication provider configuration
    /// </summary>
    public class AuthenticationProvider
    {
        /// <summary>
        /// Gets or sets the provider name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the provider class name
        /// </summary>
        [JsonProperty("className")]
        public string ClassName { get; set; }

        /// <summary>
        /// Gets or sets the provider configuration
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
        /// Gets or sets the list of providers
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
        /// Gets or sets the provider
        /// </summary>
        [JsonProperty("provider")]
        public AuthenticationProvider Provider { get; set; }
    }

    /// <summary>
    /// Represents a security filter chain configuration
    /// </summary>
    public class FilterChain
    {
        /// <summary>
        /// Gets or sets the chain name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the chain pattern
        /// </summary>
        [JsonProperty("pattern")]
        public string Pattern { get; set; }

        /// <summary>
        /// Gets or sets the list of filters in the chain
        /// </summary>
        [JsonProperty("filters")]
        public List<string> Filters { get; set; }

        /// <summary>
        /// Gets or sets whether the chain is disabled
        /// </summary>
        [JsonProperty("disabled")]
        public bool? Disabled { get; set; }

        /// <summary>
        /// Gets or sets whether to allow session creation
        /// </summary>
        [JsonProperty("allowSessionCreation")]
        public bool? AllowSessionCreation { get; set; }

        /// <summary>
        /// Gets or sets whether SSL is required
        /// </summary>
        [JsonProperty("requireSSL")]
        public bool? RequireSSL { get; set; }

        /// <summary>
        /// Gets or sets whether to match HTTP method
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
        /// Gets or sets the list of chains
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
        /// Gets or sets the filter chain
        /// </summary>
        [JsonProperty("filterChain")]
        public FilterChain Chain { get; set; }
    }

    /// <summary>
    /// Represents a password change request
    /// </summary>
    public class PasswordChangeRequest
    {
        /// <summary>
        /// Gets or sets the new password
        /// </summary>
        [JsonProperty("newPassword")]
        public string NewPassword { get; set; }
    }

    /// <summary>
    /// Represents a keystore entry
    /// </summary>
    public class KeystoreEntry
    {
        /// <summary>
        /// Gets or sets the alias
        /// </summary>
        [JsonProperty("alias")]
        public string Alias { get; set; }

        /// <summary>
        /// Gets or sets the entry type
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the algorithm
        /// </summary>
        [JsonProperty("algorithm")]
        public string Algorithm { get; set; }
    }

    /// <summary>
    /// Wrapper for keystore information
    /// </summary>
    public class KeystoreInfo
    {
        /// <summary>
        /// Gets or sets the keystore type
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the keystore provider
        /// </summary>
        [JsonProperty("provider")]
        public string Provider { get; set; }

        /// <summary>
        /// Gets or sets the list of aliases
        /// </summary>
        [JsonProperty("aliases")]
        public List<KeystoreEntry> Aliases { get; set; }
    }
}
