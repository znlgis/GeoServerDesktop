using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.App.ViewModels;

/// <summary>
/// 用户、组和角色管理的视图模型（三标签页布局）
/// </summary>
public partial class UsersGroupsRolesViewModel : ViewModelBase
{
    private readonly IGeoServerConnectionService _connectionService;

    // --- Users ---

    /// <summary>
    /// 用户名列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _users = new();

    /// <summary>
    /// 选中的用户名
    /// </summary>
    [ObservableProperty]
    private string? _selectedUser;

    /// <summary>
    /// 新建用户名输入
    /// </summary>
    [ObservableProperty]
    private string _newUsername = string.Empty;

    /// <summary>
    /// 新建用户密码输入
    /// </summary>
    [ObservableProperty]
    private string _newPassword = string.Empty;

    /// <summary>
    /// 编辑用户时的新密码输入（为空则不修改密码）
    /// </summary>
    [ObservableProperty]
    private string _editPassword = string.Empty;

    /// <summary>
    /// 编辑用户时的启用状态
    /// </summary>
    [ObservableProperty]
    private bool _editEnabled = true;

    // --- Groups ---

    /// <summary>
    /// 组名列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _groups = new();

    /// <summary>
    /// 选中的组名
    /// </summary>
    [ObservableProperty]
    private string? _selectedGroup;

    /// <summary>
    /// 新建组名输入
    /// </summary>
    [ObservableProperty]
    private string _newGroupName = string.Empty;

    // --- Roles ---

    /// <summary>
    /// 角色名列表（只读，RoleService 不支持创建/删除角色）
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _roles = new();

    /// <summary>
    /// 选中的角色名
    /// </summary>
    [ObservableProperty]
    private string? _selectedRole;

    // --- Common ---

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// 状态消息
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Ready";

    /// <summary>
    /// 初始化 UsersGroupsRolesViewModel 类的新实例
    /// </summary>
    /// <param name="connectionService">GeoServer 连接服务</param>
    public UsersGroupsRolesViewModel(IGeoServerConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    /// <summary>
    /// 加载所有数据命令（用户、组、角色）
    /// </summary>
    [RelayCommand]
    private async Task LoadAllAsync()
    {
        if (!_connectionService.IsConnected)
        {
            StatusMessage = "Not connected to GeoServer";
            return;
        }

        IsLoading = true;
        StatusMessage = "Loading users, groups, and roles...";

        try
        {
            await LoadUsersInternalAsync();
            await LoadGroupsInternalAsync();
            await LoadRolesInternalAsync();
            StatusMessage = $"已加载 {Users.Count} 用户，{Groups.Count} 组，{Roles.Count} 角色";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 创建用户命令
    /// </summary>
    [RelayCommand]
    private async Task CreateUserAsync()
    {
        if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewPassword))
        {
            StatusMessage = "用户名和密码不能为空";
            return;
        }

        IsLoading = true;
        StatusMessage = $"正在创建用户 '{NewUsername}'...";

        try
        {
            var service = _connectionService.GetUserGroupService();
            var user = new User
            {
                UserName = NewUsername,
                Password = NewPassword,
                Enabled = true
            };
            await service.CreateUserAsync(user);
            NewUsername = string.Empty;
            NewPassword = string.Empty;
            await LoadUsersInternalAsync();
            StatusMessage = "用户创建成功";
        }
        catch (Exception ex)
        {
            StatusMessage = $"创建用户失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 删除用户命令
    /// </summary>
    [RelayCommand]
    private async Task DeleteUserAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedUser))
        {
            StatusMessage = "请先选择一个用户";
            return;
        }

        IsLoading = true;
        StatusMessage = $"正在删除用户 '{SelectedUser}'...";

        try
        {
            var service = _connectionService.GetUserGroupService();
            await service.DeleteUserAsync(SelectedUser);
            SelectedUser = null;
            await LoadUsersInternalAsync();
            StatusMessage = "用户删除成功";
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除用户失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 创建组命令
    /// </summary>
    [RelayCommand]
    private async Task CreateGroupAsync()
    {
        if (string.IsNullOrWhiteSpace(NewGroupName))
        {
            StatusMessage = "组名不能为空";
            return;
        }

        IsLoading = true;
        StatusMessage = $"正在创建组 '{NewGroupName}'...";

        try
        {
            var service = _connectionService.GetUserGroupService();
            var group = new UserGroup { GroupName = NewGroupName };
            await service.CreateGroupAsync(group);
            NewGroupName = string.Empty;
            await LoadGroupsInternalAsync();
            StatusMessage = "组创建成功";
        }
        catch (Exception ex)
        {
            StatusMessage = $"创建组失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 删除组命令
    /// </summary>
    [RelayCommand]
    private async Task DeleteGroupAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedGroup))
        {
            StatusMessage = "请先选择一个组";
            return;
        }

        IsLoading = true;
        StatusMessage = $"正在删除组 '{SelectedGroup}'...";

        try
        {
            var service = _connectionService.GetUserGroupService();
            await service.DeleteGroupAsync(SelectedGroup);
            SelectedGroup = null;
            await LoadGroupsInternalAsync();
            StatusMessage = "组删除成功";
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除组失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 更新用户（修改密码和启用状态）
    /// </summary>
    [RelayCommand]
    private async Task UpdateUserAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedUser))
        {
            StatusMessage = "请先选择一个用户";
            return;
        }

        IsLoading = true;
        StatusMessage = $"正在更新用户 '{SelectedUser}'...";

        try
        {
            var service = _connectionService.GetUserGroupService();
            var user = new User
            {
                UserName = SelectedUser,
                Enabled = EditEnabled
            };
            if (!string.IsNullOrWhiteSpace(EditPassword))
            {
                user.Password = EditPassword;
            }
            await service.UpdateUserAsync(SelectedUser, user);
            EditPassword = string.Empty;
            StatusMessage = "用户更新成功";
        }
        catch (Exception ex)
        {
            StatusMessage = $"更新用户失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 内部方法：加载用户列表
    /// </summary>
    private async Task LoadUsersInternalAsync()
    {
        var service = _connectionService.GetUserGroupService();
        var result = await service.GetUsersAsync();
        Users.Clear();
        if (result?.Users != null)
        {
            foreach (var username in result.Users)
            {
                Users.Add(username);
            }
        }
    }

    /// <summary>
    /// 内部方法：加载组列表
    /// </summary>
    private async Task LoadGroupsInternalAsync()
    {
        var service = _connectionService.GetUserGroupService();
        var result = await service.GetGroupsAsync();
        Groups.Clear();
        if (result?.Groups != null)
        {
            foreach (var groupname in result.Groups)
            {
                Groups.Add(groupname);
            }
        }
    }

    /// <summary>
    /// 内部方法：加载角色列表（只读）
    /// </summary>
    private async Task LoadRolesInternalAsync()
    {
        var service = _connectionService.GetRoleService();
        var result = await service.GetRolesAsync();
        Roles.Clear();
        if (result?.Roles != null)
        {
            foreach (var rolename in result.Roles)
            {
                Roles.Add(rolename);
            }
        }
    }
}
