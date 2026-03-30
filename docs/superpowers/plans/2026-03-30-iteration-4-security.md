# 迭代 4：Security 管理页面实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 实现 Security Settings（安全设置）和 Users/Groups/Roles（用户/组/角色完整 CRUD）两个管理页面。

**架构：** 扩展 `IGeoServerConnectionService` 添加 SecurityService、UserGroupService、RoleService 获取方法。UserGroupService 和 RoleService 支持完整 CRUD（列表+新建+删除），SecurityService 支持读取和修改 ACL 规则。

**技术栈：** C# 12, .NET 8, Avalonia UI, CommunityToolkit.Mvvm, GeoServerClient 服务层

---

## 文件清单

### 需先确认的 Model/Service 结构
- `src/GeoServerDesktop.GeoServerClient/Models/Security.cs`
- `src/GeoServerDesktop.GeoServerClient/Services/SecurityService.cs`
- `src/GeoServerDesktop.GeoServerClient/Services/UserGroupService.cs`
- `src/GeoServerDesktop.GeoServerClient/Services/RoleService.cs`

### 新建文件
- `src/GeoServerDesktop.App/ViewModels/SecuritySettingsViewModel.cs`
- `src/GeoServerDesktop.App/ViewModels/UsersGroupsRolesViewModel.cs`
- `src/GeoServerDesktop.App/Views/SecuritySettingsView.axaml`
- `src/GeoServerDesktop.App/Views/SecuritySettingsView.axaml.cs`
- `src/GeoServerDesktop.App/Views/UsersGroupsRolesView.axaml`
- `src/GeoServerDesktop.App/Views/UsersGroupsRolesView.axaml.cs`

### 修改文件
- `src/GeoServerDesktop.App/Services/IGeoServerConnectionService.cs`
- `src/GeoServerDesktop.App/Services/GeoServerConnectionService.cs`
- `src/GeoServerDesktop.App/ViewModels/MainWindowViewModel.cs`

---

## 任务 1：探查 Security 模型和服务

- [ ] **步骤 1：阅读 Security.cs**

关注 ACL 规则的数据结构（通常是 `{rule: roleList}` 键值对形式）。

- [ ] **步骤 2：阅读 UserGroupService.cs 和 RoleService.cs**

确认方法签名：
- 用户列表、创建用户、删除用户、更新密码
- 组列表、创建组、删除组
- 角色列表、创建角色、删除角色

---

## 任务 2：扩展 IGeoServerConnectionService

- [ ] **步骤 1：添加接口方法**

```csharp
/// <summary>获取安全访问控制服务</summary>
SecurityService GetSecurityService();

/// <summary>获取用户组服务</summary>
UserGroupService GetUserGroupService();

/// <summary>获取角色服务</summary>
RoleService GetRoleService();
```

- [ ] **步骤 2：实现服务方法**

```csharp
public SecurityService GetSecurityService() { EnsureConnected(); return _factory!.CreateSecurityService(); }
public UserGroupService GetUserGroupService() { EnsureConnected(); return _factory!.CreateUserGroupService(); }
public RoleService GetRoleService() { EnsureConnected(); return _factory!.CreateRoleService(); }
```

---

## 任务 3：实现 SecuritySettingsViewModel

**文件：**
- 创建：`src/GeoServerDesktop.App/ViewModels/SecuritySettingsViewModel.cs`

- [ ] **步骤 1：创建 ViewModel**

展示 ACL 规则列表（以字符串列表形式）并支持加载：

```csharp
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// GeoServer 安全设置的视图模型
    /// </summary>
    public partial class SecuritySettingsViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>访问控制规则列表（以 "rule => roles" 形式展示）</summary>
        [ObservableProperty]
        private ObservableCollection<string> _aclRules = new();

        /// <summary>是否正在加载</summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>状态消息</summary>
        [ObservableProperty]
        private string _statusMessage = "Ready";

        public SecuritySettingsViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            if (!_connectionService.IsConnected) { StatusMessage = "Not connected to GeoServer"; return; }
            IsLoading = true;
            StatusMessage = "Loading security settings...";
            try
            {
                var service = _connectionService.GetSecurityService();
                var acl = await service.GetAccessControlListAsync();
                AclRules.Clear();
                // 根据实际模型结构展示规则，例如：
                // if (acl?.Rules != null)
                //     foreach (var rule in acl.Rules)
                //         AclRules.Add($"{rule.Key} => {string.Join(", ", rule.Value)}");
                StatusMessage = $"Loaded {AclRules.Count} ACL rules";
            }
            catch (Exception ex) { StatusMessage = $"Failed to load security settings: {ex.Message}"; }
            finally { IsLoading = false; }
        }
    }
}
```

---

## 任务 4：实现 UsersGroupsRolesViewModel

**文件：**
- 创建：`src/GeoServerDesktop.App/ViewModels/UsersGroupsRolesViewModel.cs`

- [ ] **步骤 1：创建支持三标签页（Users/Groups/Roles）的 ViewModel**

使用 TabControl 区分三个视角，每个标签页独立的列表 + 操作：

```csharp
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// 用户、组和角色管理的视图模型
    /// </summary>
    public partial class UsersGroupsRolesViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        // --- Users ---
        [ObservableProperty] private ObservableCollection<string> _users = new();
        [ObservableProperty] private string? _selectedUser;
        [ObservableProperty] private string _newUsername = string.Empty;
        [ObservableProperty] private string _newPassword = string.Empty;

        // --- Groups ---
        [ObservableProperty] private ObservableCollection<string> _groups = new();
        [ObservableProperty] private string? _selectedGroup;
        [ObservableProperty] private string _newGroupName = string.Empty;

        // --- Roles ---
        [ObservableProperty] private ObservableCollection<string> _roles = new();
        [ObservableProperty] private string? _selectedRole;
        [ObservableProperty] private string _newRoleName = string.Empty;

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _statusMessage = "Ready";

        public UsersGroupsRolesViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        [RelayCommand]
        private async Task LoadAllAsync()
        {
            if (!_connectionService.IsConnected) { StatusMessage = "Not connected to GeoServer"; return; }
            IsLoading = true;
            try
            {
                await LoadUsersAsync();
                await LoadGroupsAsync();
                await LoadRolesAsync();
                StatusMessage = "Users, groups, and roles loaded";
            }
            catch (Exception ex) { StatusMessage = $"Load failed: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        private async Task LoadUsersAsync()
        {
            var service = _connectionService.GetUserGroupService();
            var result = await service.GetUsersAsync();
            Users.Clear();
            // 根据实际模型调整
            // foreach (var u in result?.Users?.UserList ?? Array.Empty<object>())
            //     Users.Add(u.UserName ?? u.ToString());
        }

        private async Task LoadGroupsAsync()
        {
            var service = _connectionService.GetUserGroupService();
            var result = await service.GetGroupsAsync();
            Groups.Clear();
            // foreach (var g in result?.Groups?.GroupList ?? Array.Empty<object>())
            //     Groups.Add(g.GroupName ?? g.ToString());
        }

        private async Task LoadRolesAsync()
        {
            var service = _connectionService.GetRoleService();
            var result = await service.GetRolesAsync();
            Roles.Clear();
            // foreach (var r in result?.Roles?.RoleList ?? Array.Empty<object>())
            //     Roles.Add(r.RoleName ?? r.ToString());
        }

        [RelayCommand]
        private async Task CreateUserAsync()
        {
            if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewPassword))
            {
                StatusMessage = "Username and password are required";
                return;
            }
            IsLoading = true;
            try
            {
                var service = _connectionService.GetUserGroupService();
                // await service.CreateUserAsync(NewUsername, NewPassword);
                NewUsername = string.Empty;
                NewPassword = string.Empty;
                await LoadUsersAsync();
                StatusMessage = "User created";
            }
            catch (Exception ex) { StatusMessage = $"Failed to create user: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        private async Task DeleteUserAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedUser)) { StatusMessage = "No user selected"; return; }
            IsLoading = true;
            try
            {
                var service = _connectionService.GetUserGroupService();
                // await service.DeleteUserAsync(SelectedUser);
                SelectedUser = null;
                await LoadUsersAsync();
                StatusMessage = "User deleted";
            }
            catch (Exception ex) { StatusMessage = $"Failed to delete user: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        // CreateGroup、DeleteGroup、CreateRole、DeleteRole 方法类似，略
    }
}
```

**重要：** 执行前必须先阅读 `UserGroupService.cs` 和 `RoleService.cs` 确认实际方法签名，解除注释中的代码并做必要调整。

---

## 任务 5：创建 View 文件

- [ ] **步骤 1：创建 SecuritySettingsView.axaml**

布局：Header + ListBox（展示 ACL 规则，只读）+ StatusBar

- [ ] **步骤 2：创建 UsersGroupsRolesView.axaml**

使用 TabControl 创建三个标签页：
- **Users 标签页**：ListBox 展示用户列表 + 右侧面板（用户名输入+密码输入+Create按钮，Delete按钮）
- **Groups 标签页**：ListBox 展示组列表 + 右侧面板（组名输入+Create按钮，Delete按钮）
- **Roles 标签页**：ListBox 展示角色列表 + 右侧面板（角色名输入+Create按钮，Delete按钮）

Avalonia TabControl 示例：
```xml
<TabControl>
    <TabItem Header="Users">
        <Grid ColumnDefinitions="*,Auto,280">
            <!-- List and actions -->
        </Grid>
    </TabItem>
    <TabItem Header="Groups">
        <!-- similar structure -->
    </TabItem>
    <TabItem Header="Roles">
        <!-- similar structure -->
    </TabItem>
</TabControl>
```

---

## 任务 6：集成到 MainWindowViewModel

- [ ] **步骤 1：添加字段并初始化**

```csharp
[ObservableProperty] private SecuritySettingsViewModel _securitySettingsViewModel;
[ObservableProperty] private UsersGroupsRolesViewModel _usersGroupsRolesViewModel;
```

- [ ] **步骤 2：替换 ShowSecuritySettings 和 ShowUsersGroups 方法**

```csharp
[RelayCommand]
private void ShowSecuritySettings()
{
    if (!IsConnected) { StatusMessage = "Please connect to GeoServer first"; return; }
    CurrentView = SecuritySettingsViewModel;
    _ = SecuritySettingsViewModel.LoadSettingsCommand.ExecuteAsync(null);
    StatusMessage = "Security Settings";
}

[RelayCommand]
private void ShowUsersGroups()
{
    if (!IsConnected) { StatusMessage = "Please connect to GeoServer first"; return; }
    CurrentView = UsersGroupsRolesViewModel;
    _ = UsersGroupsRolesViewModel.LoadAllCommand.ExecuteAsync(null);
    StatusMessage = "Users, Groups, and Roles";
}
```

- [ ] **步骤 3：构建验证并 Commit**

```bash
git commit -m "feat: 实现 Security Settings 和 Users/Groups/Roles 管理页面（迭代4）"
```

---

## 验收标准

- [ ] Security > Settings 显示 ACL 规则列表
- [ ] Security > Users/Groups/Roles 显示三标签页
- [ ] Users 标签可以创建和删除用户
- [ ] Groups/Roles 标签可以创建和删除组/角色
- [ ] `dotnet build` 0 错误 0 警告
