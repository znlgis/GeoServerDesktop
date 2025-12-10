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
}
